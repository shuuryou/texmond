#if UNIX
using Mono.Unix;
#endif
using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace texmond
{
    public sealed class PanelSerialController : IDisposable
    {
        private Dictionary<byte, PanelMessage> m_PendingMessages;
        private SerialPort m_SerialPort;
        private List<byte> m_ResponseBuffer;

        private bool m_WaitingForSync;
        private byte m_SyncSequenceNumber;
        private ManualResetEvent m_SyncWaitHandle;
        private PanelMessage m_SyncResponse;

        public delegate void MessageReceivedDelegate(object sender, MessageReceivedEventArgs e);
        public event MessageReceivedDelegate MessageReceived;

        public PanelSerialController(string comport)
        {
            if (comport == null) throw new ArgumentNullException("comport");

            m_SerialPort = new SerialPort(comport, 19200, Parity.None, 8, StopBits.Two)
            {
                WriteTimeout = 5000
            };

            m_PendingMessages = new Dictionary<byte, PanelMessage>(255);
            m_ResponseBuffer = new List<byte>(255);

            m_WaitingForSync = false;
            m_SyncSequenceNumber = 0;
            m_SyncWaitHandle = new ManualResetEvent(false);
            m_SyncResponse = null;
            LastMessageSent = DateTime.MinValue;
        }

        private void ____kickoffRead()
        {
            byte[] buffer = new byte[1024];

            m_SerialPort.BaseStream.BeginRead(buffer, 0, buffer.Length, delegate (IAsyncResult ar)
            {
                int actualLength = m_SerialPort.BaseStream.EndRead(ar);

                byte[] received = new byte[actualLength];
                Buffer.BlockCopy(buffer, 0, received, 0, actualLength);

                m_ResponseBuffer.AddRange(received);

            again:
                if (m_ResponseBuffer.Count < 6)
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Buffer has insufficient data from slave. We have {0} byte(s) and need {1} more.",
                        m_ResponseBuffer.Count, 6 - m_ResponseBuffer.Count);
                    goto done;
                }

                // Look for a message start indicator
                int offset = 0;

                foreach (byte b in m_ResponseBuffer)
                {
                    if (b == PanelMessage.PANEL_MESSAGE_START_INDICATOR)
                        break;

                    offset++;
                }

                if (offset != 0)
                {
                    Logging.Log(SyslogLevel.LOG_WARNING, "Removing {0} bytes of garbage from buffer. Protocol out of sync?", offset);
                    m_ResponseBuffer.RemoveRange(0, offset);
                }

                byte len = m_ResponseBuffer[2];

                if (m_ResponseBuffer.Count < len)
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Expecting {0} bytes message from slave but only have {1} bytes so far.", len, m_ResponseBuffer.Count);
                    goto done;
                }

                byte[] this_message = new byte[len];

                for (int i = 0; i < len; i++)
                    this_message[i] = m_ResponseBuffer[i];

                byte crc_expected = m_ResponseBuffer[len - 1];
                byte crc = PanelMessage.CRC8(this_message, 0, this_message.Length - 1);

                if (crc != crc_expected)
                {
                    Logging.Log(SyslogLevel.LOG_ERR, "Checksum mismatch (got 0x{0:x2} but expected 0x{1:x2}) while parsing message from slave. Discarding.",
                        crc, crc_expected);
                    m_ResponseBuffer.RemoveRange(0, len);
                    goto done;
                }

                m_ResponseBuffer.RemoveRange(0, len);

                PanelMessage response = new PanelMessage(this_message);
                PanelMessage message = null;

                Logging.Log(SyslogLevel.LOG_DEBUG, "Successfully parsed message from panel with sequence number {0}: {1}",
                    response.SequenceNumber, BytesToHex(this_message));

                if (response.Type == PanelMessageType.Response && m_PendingMessages.ContainsKey(response.SequenceNumber))
                {
                    message = m_PendingMessages[response.SequenceNumber];
                    m_PendingMessages.Remove(response.SequenceNumber);

                    Logging.Log(SyslogLevel.LOG_DEBUG, "Successfully matched response with sequence number {0} from slave with sent message from master.", response.SequenceNumber);
                }

                if (m_WaitingForSync && response.Type == PanelMessageType.Response && response.SequenceNumber == m_SyncSequenceNumber)
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "SYNC message response received and successfully processed.");
                    m_SyncResponse = response;
                    m_SyncWaitHandle.Set();
                }
                else
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Async message successfully processed.");
                    MessageReceivedEventArgs args = new MessageReceivedEventArgs(message, response);
                    OnMessageReceived(args);
                }

                if (m_ResponseBuffer.Count != 0)
                {
                    Logging.Log(SyslogLevel.LOG_INFO, "There are still {0:n0} byte(s) of data from the slave to process.", m_ResponseBuffer.Count);
                    goto again;
                }

            done:

                ____kickoffRead();
            }, null);
        }

        public void Open()
        {
            m_SerialPort.Open();

            // workaround to get mono to compile this
            Action action = new Action(____kickoffRead);

            action();
        }

        public void SendMessage(PanelMessage message)
        {
            if (m_PendingMessages.ContainsKey(message.SequenceNumber))
                m_PendingMessages.Remove(message.SequenceNumber);

            m_PendingMessages.Add(message.SequenceNumber, message);

            Logging.Log(SyslogLevel.LOG_DEBUG, "Sending async message with sequence number {0} to slave: {1}", message.SequenceNumber,
                BytesToHex(message.RawMessage));
                
            LastMessageSent = DateTime.Now;

            try
            {
                m_SerialPort.BaseStream.Write(message.RawMessage, 0, message.RawMessage.Length);
            }
            catch (IOException e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to send message to slave: {0}", e.Message);
                throw;
            }
            catch (TimeoutException e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Timed out while sending message to slave: {0}", e.Message);
                throw;
            }
        }

        public PanelMessage SendMessageSync(PanelMessage message)
        {
            const int MAXIMUM_ATTEMPTS = 5;
            const int WAIT_TIMEOUT = 1500;

            int attempts = 1;

            if (m_PendingMessages.ContainsKey(message.SequenceNumber))
                m_PendingMessages.Remove(message.SequenceNumber);

            m_PendingMessages.Add(message.SequenceNumber, message);

            Logging.Log(SyslogLevel.LOG_DEBUG, "Sending SYNC message with sequence number {0} to slave: {1}", message.SequenceNumber,
                BytesToHex(message.RawMessage));
                
            LastMessageSent = DateTime.Now;

            m_WaitingForSync = true;
            m_SyncResponse = null;
            m_SyncSequenceNumber = message.SequenceNumber;
            m_SyncWaitHandle.Reset();

        again:

            try
            {
                m_SerialPort.BaseStream.Write(message.RawMessage, 0, message.RawMessage.Length);
            }
            catch (IOException e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to send SYNC message to slave: {0}", e.Message);

                m_WaitingForSync = false;
                return null;
            }
            catch (TimeoutException e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Timed out while sending SYNC message to slave: {0}", e.Message);

                m_WaitingForSync = false;
                return null;
            }

            if (!m_SyncWaitHandle.WaitOne(WAIT_TIMEOUT))
            {
                if (m_PendingMessages.ContainsKey(message.SequenceNumber))
                    m_PendingMessages.Remove(message.SequenceNumber);

                Logging.Log(SyslogLevel.LOG_ERR, "Timed out while waiting for SYNC response from slave.");

                if (++attempts <= MAXIMUM_ATTEMPTS)
                {
                    Logging.Log(SyslogLevel.LOG_INFO, "Performing attempt #{0:n0} for SYNC message.", attempts);
                    goto again;
                }

                m_WaitingForSync = false;
                return null;
            }

            Logging.Log(SyslogLevel.LOG_DEBUG, "Received SYNC response from slave after {0:n0} attempt(s).", attempts);

            m_WaitingForSync = false;
            return m_SyncResponse;
        }

        public void Dispose()
        {
            if (m_SerialPort != null)
            {
                if (m_SerialPort.IsOpen)
                    m_SerialPort.Close();

                m_SerialPort.Dispose();
                m_SerialPort = null;
            }

            if (m_SyncWaitHandle != null)
            {
                m_SyncWaitHandle.Dispose();
                m_SyncWaitHandle = null;
            }
        }

        private void OnMessageReceived(MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        private string BytesToHex(byte[] bytearray)
        {
            StringBuilder hex = new StringBuilder(bytearray.Length * 3);

            foreach (byte b in bytearray)
                hex.AppendFormat("{0:x2} ", b);

            return hex.ToString();
        }
        
        public DateTime LastMessageSent
        {
            get;
            private set;
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(PanelMessage message, PanelMessage response)
        {
            Message = message;
            Response = response;
        }

        public PanelMessage Message { get; private set; }
        public PanelMessage Response { get; private set; }
    }
}