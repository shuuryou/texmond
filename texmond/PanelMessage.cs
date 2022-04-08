using System;
using System.IO;

namespace texmond
{
    public class PanelMessage
    {
        public const byte PANEL_MESSAGE_START_INDICATOR = (byte)'t';

        public PanelMessage(PanelMessageType type, byte sequenceno, byte[] payload)
        {
            if (type == PanelMessageType.None) throw new ArgumentOutOfRangeException("type");
            if (payload == null) throw new ArgumentNullException("payload");
            if (payload.Length > 250) throw new ArgumentOutOfRangeException("The payload is too big.");

            RawMessage = new byte[payload.Length + 5];

            Buffer.BlockCopy(payload, 0, RawMessage, 4, payload.Length);

            RawMessage[0] = (byte)'t';
            RawMessage[1] = (byte)type;
            RawMessage[2] = (byte)RawMessage.Length;
            RawMessage[3] = sequenceno;
            RawMessage[RawMessage.Length - 1] = CRC8(RawMessage, 0, RawMessage.Length - 1);
        }

        public PanelMessage(byte[] rawmessage)
        {
            if (rawmessage == null) throw new ArgumentNullException("rawmessage");
            if (rawmessage.Length < 6) throw new InvalidDataException("Raw message data invalid.");
            if (rawmessage[2] != rawmessage.Length) throw new InvalidDataException("Raw message length byte does not match raw message data size.");

            RawMessage = rawmessage;
        }

        public byte SequenceNumber
        {
            get { return RawMessage[3]; }
        }

        public PanelMessageType Type
        {
            get { return (PanelMessageType)RawMessage[1]; }
        }

        public byte[] RawMessage
        {
            get;
            private set;
        }

        public byte[] Payload
        {
            get
            {
                byte[] ret = new byte[RawMessage.Length - 5];
                Buffer.BlockCopy(RawMessage, 4, ret, 0, RawMessage.Length - 5);
                return ret;
            }
        }

        public static byte CRC8(byte[] arr, int offset, int length)
        {
            byte crc = 0xFF;
            byte poly = 0x85;

            for (int i = offset; i < length; i++)
            {
                crc ^= arr[i];
                for (int j = 0; j < 8; j++)
                    if ((crc & 0x80) == 0x80)
                        crc = (byte)((crc << 1) ^ poly);
                    else
                        crc = (byte)(crc << 1);
            }

            return crc;
        }
    }
}