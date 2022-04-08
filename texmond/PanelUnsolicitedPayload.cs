using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;

namespace texmond
{
    public static class PanelUnsolicitedPayload
    {
        public static UnsolicitedMessage Parse(byte[] payload)
        {

            switch (payload[0]) // Message ID
            {
                case 0:
                    return new DebugMessage(payload[0], payload);
                case 1:
                    return new ZoneEventMessage(payload[0], payload);
                case 2:
                    return new AreaEventMessage(payload[0], payload);
                case 3:
                    return new OutputEventMessage(payload[0], payload);
                case 4:
                    return new UserEventMessage(payload[0], payload);
                case 5:
                    return new LogEventMessage(payload[0], payload);
                default:
                    throw new NotSupportedException("Unsupported message ID: " + payload[0].ToString(CultureInfo.InvariantCulture));
            }
        }

    }

    public class UnsolicitedMessage
    {
        public UnsolicitedMessage(byte messageid)
        {
            MessageID = messageid;
        }

        public int MessageID { get; private set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Unsolicited message with ID {0}", MessageID);
        }
    }

    public class DebugMessage : UnsolicitedMessage
    {
        public DebugMessage(byte messageid, byte[] payload) : base(messageid)
        {
            if (messageid != 0) throw new ArgumentException("Invalid message ID.", "messageid");
            if (payload == null) throw new ArgumentNullException("payload");

            Message = Encoding.ASCII.GetString(payload, 1, payload.Length - 1);
        }

        public string Message { get; private set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Unsolicited debug message: {0}", Message);
        }
    }

    public class ZoneEventMessage : UnsolicitedMessage
    {
        public ZoneEventMessage(byte messageid, byte[] payload) : base(messageid)
        {
            if (messageid != 1) throw new ArgumentException("Invalid message ID.", "messageid");
            if (payload == null) throw new ArgumentNullException("payload");

            if (payload.Length == 4)
            {
                // 16 bit zone number

                ZoneNumber = BitConverter.ToUInt16(payload, 1);
                ZoneState = PanelZoneState.FromBitmap(payload[3]);

                return;
            }

            if (payload.Length == 3)
            {
                // 8 bit zone number

                ZoneNumber = payload[1];
                ZoneState = PanelZoneState.FromBitmap(payload[2]);

                return;
            }

            throw new InvalidDataException("Unsupported payload length.");
        }

        public int ZoneNumber { get; private set; }
        public PanelZoneState ZoneState { get; private set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Unsolicited zone event message for zone {0}: {1}", ZoneNumber, ZoneState);
        }
    }

    public class AreaEventMessage : UnsolicitedMessage
    {
        public AreaEventMessage(byte messageid, byte[] payload) : base(messageid)
        {
            if (messageid != 2) throw new ArgumentException("Invalid message ID.", "messageid");
            if (payload == null) throw new ArgumentNullException("payload");

            AreaNumber = payload[1];
            AreaState = new PanelAreaState(payload[2]);
        }

        public int AreaNumber { get; private set; }
        public PanelAreaState AreaState { get; private set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Unsolicited area event message for area {0}: {1}", AreaNumber, AreaState);
        }
    }

    public class OutputEventMessage : UnsolicitedMessage
    {
        public OutputEventMessage(byte messageid, byte[] payload) : base(messageid)
        {
            // Not really implemented properly since I don't need output states.

            if (messageid != 3) throw new ArgumentException("Invalid message ID.", "messageid");
            if (payload == null) throw new ArgumentNullException("payload");

            OutputLocation = payload[1];
            OutputState = payload[2];
        }

        public byte OutputLocation { get; private set; }
        public byte OutputState { get; private set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Unsolicited output event message for location 0x{0:x2}: 0x{1:x2}", OutputLocation, OutputState);
        }
    }

    public class UserEventMessage : UnsolicitedMessage
    {
        public UserEventMessage(byte messageid, byte[] payload) : base(messageid)
        {
            if (messageid != 4) throw new ArgumentException("Invalid message ID.", "messageid");
            if (payload == null) throw new ArgumentNullException("payload");


            if (payload.Length == 4)
            {
                // 16 bit user number

                UserNumber = BitConverter.ToUInt16(payload, 1);
                UserState = (PanelUserStates)payload[3];

                return;
            }

            if (payload.Length == 3)
            {
                // 8 bit user number

                UserNumber = payload[1];
                UserState = (PanelUserStates)payload[2];

                return;
            }

            throw new InvalidDataException("Unsupported payload length.");
        }

        public int UserNumber { get; private set; }
        public PanelUserStates UserState { get; private set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Unsolicited user event message for user {0}: {1}", UserNumber, UserState);
        }
    }

    public class LogEventMessage : UnsolicitedMessage
    {
        public LogEventMessage(byte messageid, byte[] payload) : base(messageid)
        {
            if (messageid != 5) throw new ArgumentException("Invalid message ID.", "messageid");
            if (payload == null) throw new ArgumentNullException("payload");

            byte[] logevent = new byte[payload.Length - 1];
            Buffer.BlockCopy(payload, 1, logevent, 0, payload.Length - 1);

            Entry = new PanelRawLogEvent(logevent);
        }

        public PanelRawLogEvent Entry { get; private set; }

        public override string ToString()
        {
            return Entry.ToString();
        }
    }
}