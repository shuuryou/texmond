using Mono.Unix.Native;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace texmond
{
    public static class PanelResponsePayload
    {
        public static PayloadResponse Parse(byte[] payload)
        {
            bool success;

            if (payload.Length == 2 && (payload[1] == 0x06 || payload[1] == 0x15))
            {
                // Hack since any command can return NAK
                success = payload[1] == 0x06;
            }
            else success = true;

            switch (payload[0]) // Command ID
            {
                case 1:
                    return new LogonResponse(payload[0], success);
                case 2:
                    return new GetZoneStateResponse(payload[0], success, payload);
                case 3:
                    return new GetZoneDetailsResponse(payload[0], success, payload);
                case 6:
                    return new ArmAreasResponse(payload[0], success);
                case 7:
                    return new GetProtocolVersionResponse(payload[0], success, payload);
                case 8:
                    return new DisarmAreasResponse(payload[0], success);
                case 12:
                    return new SendKeyPressResponse(payload[0], success);
                case 13:
                    return new GetLCDDisplayResponse(payload[0], success, payload);
                case 14:
                    return new SetLCDDisplayResponse(payload[0], success);
                case 15:
                    return new GetLogPointerResponse(payload[0], success, payload);
                case 16:
                    return new GetRawLogEventResponse(payload[0], success, payload);
                case 17:
                    return new GetTextLogEventResponse(payload[0], success, payload);
                case 22:
                    return new GetPanelIdentificationResponse(payload[0], success, payload);
                case 23:
                    return new GetDateTimeResponse(payload[0], success, payload);
                case 24:
                    return new SetDateTimeResponse(payload[0], success);
                case 25:
                    return new GetSystemPowerResponse(payload[0], success, payload);
                case 29:
                    return new ArmAsUserResponse(payload[0], success);
                case 30:
                    return new DisarmAsUserResponse(payload[0], success);
                case 27:
                    return new GetUserResponse(payload[0], success, payload);
                case 32:
                    return new GetZoneTextResponse(payload[0], success, payload);
                case 34:
                    return new GetAreaTextResponse(payload[0], success, payload);
                case 35:
                    return new GetAreaDetailsResponse(payload[0], success, payload);
                case 37:
                    return new SetEventMessagesResponse(payload[0], success);
                default:
                    throw new NotSupportedException("Unsupported command ID: " + payload[0].ToString(CultureInfo.InvariantCulture));
            }
        }
    }

    public class PayloadResponse
    {
        public PayloadResponse(byte commandid, bool success)
        {
            CommandID = commandid;
            Success = success;
        }

        public int CommandID { get; private set; }
        public bool Success { get; private set; }
    }

    public class LogonResponse : PayloadResponse
    {
        public LogonResponse(byte commandid, bool success) : base(commandid, success)
        {
            if (commandid != 1) throw new ArgumentException("Invalid command ID.", "commandid");
        }
    }

    public class GetZoneStateResponse : PayloadResponse
    {
        public GetZoneStateResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 2) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");

            List<PanelZoneState> zonestates = new List<PanelZoneState>(payload.Length - 1);
            for (int i = 1; i < payload.Length; i++)
                zonestates.Add(PanelZoneState.FromBitmap(payload[i]));

            ZoneStates = zonestates.ToArray();
        }

        public PanelZoneState[] ZoneStates { get; private set; }
    }

    public class GetZoneDetailsResponse : PayloadResponse
    {
        public GetZoneDetailsResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 3) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");

            ZoneType = (PanelZoneType)payload[1];
            ZoneText = Encoding.ASCII.GetString(payload, payload.Length - 32, 32).Replace('\0', ' ').TrimEnd(new char[] { '\0', ' ' });

            // Figure out the size of the area bitmap by what's left after parsing everything else
            byte[] areabitmap = new byte[payload.Length - 32 - 1];
            Buffer.BlockCopy(payload, 2, areabitmap, 0, payload.Length - 32 - 2);
            AreaBitmap = new BitArray(areabitmap);
        }

        public bool IsZoneInArea(int area)
        {
            if (area < 1 || area > AreaBitmap.Length)
                throw new ArgumentOutOfRangeException("area");

            return AreaBitmap.Get(area - 1);
        }

        private BitArray AreaBitmap { get; set; }

        public PanelZoneType ZoneType { get; private set; }
        public string ZoneText { get; private set; }
    }

    public class ArmAreasResponse : PayloadResponse
    {
        public ArmAreasResponse(byte commandid, bool success) : base(commandid, success)
        {
            if (commandid != 6) throw new ArgumentException("Invalid command ID.", "commandid");
        }
    }

    public class GetProtocolVersionResponse : PayloadResponse
    {
        public GetProtocolVersionResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 7) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");

            ProtocolVersion = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", payload[1], payload[2]);
        }

        public string ProtocolVersion { get; private set; }
    }

    public class DisarmAreasResponse : PayloadResponse
    {
        public DisarmAreasResponse(byte commandid, bool success) : base(commandid, success)
        {
            if (commandid != 8) throw new ArgumentException("Invalid command ID.", "commandid");
        }
    }

    public class SendKeyPressResponse : PayloadResponse
    {
        public SendKeyPressResponse(byte commandid, bool success) : base(commandid, success)
        {
            if (commandid != 12) throw new ArgumentException("Invalid command ID.", "commandid");
        }
    }

    public class GetLCDDisplayResponse : PayloadResponse
    {
        public GetLCDDisplayResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 13) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");

            LCDDisplay = Encoding.ASCII.GetString(payload, 1, payload.Length - 1);
        }

        public string LCDDisplay { get; private set; }
    }

    public class SetLCDDisplayResponse : PayloadResponse
    {
        public SetLCDDisplayResponse(byte commandid, bool success) : base(commandid, success)
        {
            if (commandid != 14) throw new ArgumentException("Invalid command ID.", "commandid");
        }
    }

    public class GetLogPointerResponse : PayloadResponse
    {
        public GetLogPointerResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 15) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");

            LogPointer = BitConverter.ToUInt16(payload, 1);
        }

        public int LogPointer { get; private set; }
    }

    public class GetRawLogEventResponse : PayloadResponse
    {
        private byte[] m_RawLogEvents;

        public GetRawLogEventResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 16) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");

            // 8/9/16 bytes for 1 log message

            m_RawLogEvents = new byte[payload.Length - 1];
            Buffer.BlockCopy(payload, 1, m_RawLogEvents, 0, payload.Length - 1);
        }

        public ReadOnlyCollection<PanelRawLogEvent> GetRawLogEvents(TexecomElitePanelType panelType)
        {
            int field_size;

            switch (panelType)
            {
                case TexecomElitePanelType.Elite12_64:
                    field_size = 8;
                    break;
                case TexecomElitePanelType.Elite168:
                    field_size = 9;
                    break;
                case TexecomElitePanelType.Elite640:
                    field_size = 16;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("panelType");
            }

            if (m_RawLogEvents.Length % field_size != 0)
                throw new ArgumentException("Selected panel type is not correct.", "panelType");

            List<PanelRawLogEvent> events = new List<PanelRawLogEvent>();

            for (int i = 0; i < m_RawLogEvents.Length / field_size; i++)
            {
                byte[] logevent = new byte[field_size];
                Buffer.BlockCopy(m_RawLogEvents, (i * field_size), logevent, 0, field_size);
                events.Add(new PanelRawLogEvent(logevent));
            }

            return events.AsReadOnly();
        }
    }

    public class GetTextLogEventResponse : PayloadResponse
    {
        public GetTextLogEventResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 17) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");

            LogEventMessage = Encoding.ASCII.GetString(payload, 1, payload.Length - 1);
        }

        public string LogEventMessage { get; private set; }
    }

    public class GetPanelIdentificationResponse : PayloadResponse
    {
        public GetPanelIdentificationResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 22) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");

            PanelIdentification = Encoding.ASCII.GetString(payload, 1, payload.Length - 1);
        }

        public string PanelIdentification { get; private set; }
    }

    public class GetDateTimeResponse : PayloadResponse
    {
        public GetDateTimeResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 23) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");
            if (payload.Length != 7) throw new InvalidDataException("Payload is invalid.");

            int day, month, year, hour, minute, second;
            day = payload[1];
            month = payload[2];
            year = 2000 + payload[3];
            hour = payload[4];
            minute = payload[5];
            second = payload[6];
            
            // I have managed to get a Premier Elite 64W to believe it's
            // 00.01.2015, i.e. day is 0. This happened after flashing
            // the firmware.
            // The DateTime constructor DOES NOT like that... ;-)
            
            if (day < 1 || day > 31 || month < 1 || month > 12 || hour > 23 || minute > 59 || second > 59)
            {
                Logging.Log(SyslogLevel.LOG_WARNING, "Panel date/time is illegal. D={0}; M={1}; Y={2}; H={3}; M={4}; S={5}",
                    day, month, year, hour, minute, second);
                
                PanelDateTime = DateTime.MinValue;
                
                return;
            }

            PanelDateTime = new DateTime(year, month, day, hour, minute, second);
        }

        public DateTime PanelDateTime { get; private set; }
    }

    public class SetDateTimeResponse : PayloadResponse
    {
        public SetDateTimeResponse(byte commandid, bool success) : base(commandid, success)
        {
            if (commandid != 24) throw new ArgumentException("Invalid command ID.", "commandid");
        }
    }

    public class GetSystemPowerResponse : PayloadResponse
    {
        public GetSystemPowerResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 25) throw new ArgumentException("Invalid command ID.", "commandid");

            // ((SysV-RefV)*0.07)+13.7 = real voltage
            // ((BatV-RefV)*0.07)+13.7 = real current
            // 9*SysI = real voltage
            // 9*BatI = real current

            double ref_v = payload[1],
                sys_v = payload[2],
                bat_v = payload[3],
                sys_i = payload[4],
                bat_i = payload[5];

            SystemVoltage = Math.Round(13.7D + ((sys_v - ref_v) * 0.070D), 2);
            BatteryVoltage = Math.Round(13.7D + ((bat_v - ref_v) * 0.070D), 2);
            SystemCurrent = Math.Round(sys_i * 9D, 2);
            BatteryChargingCurrent = Math.Round(bat_i * 9D, 2);
            ReferenceVoltage = Math.Round(ref_v * 0.070D, 2);
        }

        public double ReferenceVoltage { get; private set; }
        public double SystemVoltage { get; private set; }
        public double BatteryVoltage { get; private set; }
        public double SystemCurrent { get; private set; }
        public double BatteryChargingCurrent { get; private set; }
    }

    public class GetUserResponse : PayloadResponse
    {
        public GetUserResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            // This one's a pain because of how much different data there is. *sigh* here we go...

            if (commandid != 27) throw new ArgumentException("Invalid command ID.", "commandid");

            Username = Encoding.ASCII.GetString(payload, 1, 8).TrimEnd(new char[] { '\0', ' ' });

            // Passcode is 0000 - 999999 in BCD (packed; 3 bytes)
            {
                int passcode = 0;

                for (int i = 9; i < 12; i++)
                {
                    passcode *= 100;
                    passcode += (10 * (payload[i] >> 4));
                    passcode += payload[i] & 0xf;
                }

                // Makes 0001 passcode look right
                Passcode = string.Format(CultureInfo.InvariantCulture, "{0:0000}", passcode);
            }

            // There simply is no sane way to parse data from here onwards in a panel-agnostic manner!

            // Next is 7 - Areas, which is a tArea. tArea is 2 bytes (not Elite 640) or 4 bytes (Elite 640)
            // Payload length on Elite 640 panels is at least 35 bytes. Use that as a heuristic to move
            // forward.

            byte[] areabitmap;
            int pos;

            if (payload.Length >= 35)
            {
                areabitmap = new byte[4];
                Buffer.BlockCopy(payload, 12, areabitmap, 0, 4);
                pos = 12 + 4;
            }
            else
            {
                areabitmap = new byte[2];
                Buffer.BlockCopy(payload, 12, areabitmap, 0, 2);
                pos = 12 + 2;
            }

            AreaBitmap = new BitArray(areabitmap);

            Modifiers = (PanellUserModifiers)payload[pos];
            pos++;

            Locks = (PanelUserLocks)payload[pos];
            pos++;

            // Now we're at the Doors parameter, which is random length. After it follow 4 bytes for Tag
            // and 2 bytes for Config. We get the length for Doors by calculating it from what we know.

            byte[] doorsbitmap = new byte[payload.Length - pos - 4 - 2];
            Buffer.BlockCopy(payload, pos, doorsbitmap, 0, doorsbitmap.Length);
            pos += doorsbitmap.Length;

            DoorsBitmap = new BitArray(doorsbitmap);

            Tag = 0;
            for (int i = pos; i < pos + 4; i++)
            {
                Tag *= 100;
                Tag += (10 * (payload[i] >> 4));
                Tag += payload[i] & 0xf;
            }
            pos += 4;

            ushort config = BitConverter.ToUInt16(payload, pos);
            pos += 2;
            Config = (PanelUserConfig)config;

            if (pos != payload.Length)
                throw new InvalidDataException("Parsing response failed. Sorry.");
        }

        public bool HasAccessToArea(int area)
        {
            if (area < 1 || area > AreaBitmap.Length)
                throw new ArgumentOutOfRangeException("area");

            return AreaBitmap.Get(area - 1);
        }

        public bool HasAccessToDoor(int door)
        {
            if (door < 1 || door > DoorsBitmap.Length)
                throw new ArgumentOutOfRangeException("area");

            return DoorsBitmap.Get(door - 1);
        }

        public string Username { get; private set; }
        public string Passcode { get; private set; }

        private BitArray AreaBitmap { get; set; }
        public PanellUserModifiers Modifiers { get; private set; }
        public PanelUserLocks Locks { get; private set; }
        private BitArray DoorsBitmap { get; set; }
        public int Tag { get; private set; }
        public PanelUserConfig Config { get; private set; }
    }

    public class ArmAsUserResponse : PayloadResponse
    {
        public ArmAsUserResponse(byte commandid, bool success) : base(commandid, success)
        {
            if (commandid != 29) throw new ArgumentException("Invalid command ID.", "commandid");
        }
    }

    public class DisarmAsUserResponse : PayloadResponse
    {
        public DisarmAsUserResponse(byte commandid, bool success) : base(commandid, success)
        {
            if (commandid != 30) throw new ArgumentException("Invalid command ID.", "commandid");
        }
    }

    public class GetZoneTextResponse : PayloadResponse
    {
        public GetZoneTextResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 32) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");
            if ((payload.Length - 1) % 32 != 0) throw new InvalidDataException("Zone text length has incorrect size.");

            int numzones = (payload.Length - 1) / 32;
            List<string> texts = new List<string>(numzones);

            for (int i = 0; i < numzones; i++)
                texts.Add(Encoding.ASCII.GetString(payload, (i * 32) + 1, 32).Replace('\0', ' ').TrimEnd(new char[] { '\0', ' ' }));

            ZoneText = texts.AsReadOnly();
        }

        public ReadOnlyCollection<string> ZoneText { get; private set; }
    }

    public class GetAreaTextResponse : PayloadResponse
    {
        public GetAreaTextResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 34) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");
            if ((payload.Length - 1) % 16 != 0) throw new InvalidDataException("Zone text length has incorrect size.");

            int numzones = (payload.Length - 1) / 16;
            List<string> texts = new List<string>(numzones);

            for (int i = 0; i < numzones; i++)
                texts.Add(Encoding.ASCII.GetString(payload, (i * 16) + 1, 16).Replace('\0', ' ').TrimEnd(new char[] { '\0', ' ' }));

            AreaText = texts.AsReadOnly();
        }

        public ReadOnlyCollection<string> AreaText { get; private set; }
    }
    public class GetAreaDetailsResponse : PayloadResponse
    {
        public GetAreaDetailsResponse(byte commandid, bool success, byte[] payload) : base(commandid, success)
        {
            if (commandid != 35) throw new ArgumentException("Invalid command ID.", "commandid");
            if (payload == null) throw new ArgumentNullException("payload");

            AreaNumber = payload[1];
            AreaText = Encoding.ASCII.GetString(payload, 2, 16).Replace('\0', ' ').TrimEnd(new char[] { '\0', ' ' });
            ExitDelay = new TimeSpan(0, 0, (int)BitConverter.ToUInt16(payload, 18));
            Entry1Delay = new TimeSpan(0, 0, (int)BitConverter.ToUInt16(payload, 20));
            Entry2Delay = new TimeSpan(0, 0, (int)BitConverter.ToUInt16(payload, 22));
            SecondEntry = new TimeSpan(0, 0, (int)BitConverter.ToUInt16(payload, 24));
        }

        public int AreaNumber { get; private set; }
        public string AreaText { get; private set; }
        public TimeSpan ExitDelay { get; private set; }
        public TimeSpan Entry1Delay { get; private set; }
        public TimeSpan Entry2Delay { get; private set; }
        public TimeSpan SecondEntry { get; private set; }
    }

    public class SetEventMessagesResponse : PayloadResponse
    {
        public SetEventMessagesResponse(byte commandid, bool success) : base(commandid, success)
        {
            if (commandid != 37) throw new ArgumentException("Invalid command ID.", "commandid");
        }
    }
}