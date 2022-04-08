using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace texmond
{
    public static class PanelSimpleProtocol // don't be fooled by the name; it's not.
    {
        private static byte SEQUENCE_NUMBER = 0;

        private static byte NextSequence()
        {
            SEQUENCE_NUMBER = (byte)((SEQUENCE_NUMBER + 1) % 255);
            return SEQUENCE_NUMBER;
        }

        public static PanelMessage LogOn(string udlcode)
        {
            if (udlcode == null) throw new ArgumentNullException("udlcode");

            if (udlcode.Length < 4 || udlcode.Length > 16)
                throw new ArgumentOutOfRangeException("udlcode", "Invalid UDL code.");

            foreach (char c in udlcode)
                if (c < '0' || c > '9') throw new ArgumentOutOfRangeException("udlcode", "UDL code must be numeric.");

            List<byte> payload = new List<byte>
            {
                1
            };

            payload.AddRange(Encoding.ASCII.GetBytes(udlcode));

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetZoneState(TexecomElitePanelType panel_type, int start_zone, int number_of_zones)
        {
            if (start_zone < 1) throw new ArgumentOutOfRangeException("start_zone");
            if (number_of_zones < 1 || number_of_zones > 168) throw new ArgumentOutOfRangeException("number_of_zones");

            if ((panel_type == TexecomElitePanelType.Elite12_64 || panel_type == TexecomElitePanelType.Elite168) && start_zone > 255)
                throw new ArgumentException("Selected panel type does not support that many zones.", "start_zone");

            List<byte> payload = new List<byte>
            {
                2
            };

            switch (panel_type)
            {
                default:
                    throw new ArgumentOutOfRangeException("panel_type");
                case TexecomElitePanelType.Elite12_64:
                case TexecomElitePanelType.Elite168:
                    payload.Add((byte)start_zone);
                    break;
                case TexecomElitePanelType.Elite640:
                    payload.AddRange(BitConverter.GetBytes((ushort)start_zone));
                    break;
            }

            payload.Add((byte)number_of_zones);

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetZoneDetails(TexecomElitePanelType panel_type, int zone_number)
        {
            if (zone_number < 1) throw new ArgumentOutOfRangeException("zone_number");

            if ((panel_type == TexecomElitePanelType.Elite12_64 || panel_type == TexecomElitePanelType.Elite168) && zone_number > 255)
                throw new ArgumentException("Selected panel type does not support that many zones.", "start_zone");

            List<byte> payload = new List<byte>
            {
                3
            };

            switch (panel_type)
            {
                default:
                    throw new ArgumentOutOfRangeException("panel_type");
                case TexecomElitePanelType.Elite12_64:
                case TexecomElitePanelType.Elite168:
                    payload.Add((byte)zone_number);
                    break;
                case TexecomElitePanelType.Elite640:
                    payload.AddRange(BitConverter.GetBytes((ushort)zone_number));
                    break;
            }

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage ArmAreas(TexecomElitePanelType panel_type, PanelAreaArmType arm_type, params int[] areas)
        {

            List<byte> payload = new List<byte>
            {
                6,
                (byte)arm_type
            };

            if (areas.Length == 0)
                throw new InvalidOperationException("Must specify at least one area to arm.");

            byte[] areadata;
            switch (panel_type)
            {
                // Probably right:
                // * Size in Elite 12 - 64:  8 bits (1 byte)
                // * Size in Elite 168:     16 bits (2 bytes)
                // * Size in Elite 640:     64 bits (8 bytes)
                case TexecomElitePanelType.Elite12_64:
                    areadata = new byte[1];
                    break;
                case TexecomElitePanelType.Elite168:
                    areadata = new byte[2];
                    break;
                case TexecomElitePanelType.Elite640:
                    areadata = new byte[8];
                    break;
                default:
                    throw new ArgumentOutOfRangeException("panel_type");
            }

            foreach (int areano in areas)
            {
                int byteidx = areano / 8, bitidx = areano % 8;

                if (byteidx > areadata.Length)
                    throw new ArgumentOutOfRangeException("areas");

                areadata[byteidx] |= (byte)(1 << bitidx - 1);
            }

            // Area 1 must be the rightmost bit
            for (int i = areadata.Length - 1; i >= 0; i--)
                payload.Add(areadata[i]);

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetProtocolVersion()
        {
            List<byte> payload = new List<byte>
            {
                7
            };

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage DisarmAreas(TexecomElitePanelType panel_type, params int[] areas)
        {

            List<byte> payload = new List<byte>
            {
                8
            };

            if (areas.Length == 0)
                throw new InvalidOperationException("Must specify at least one area to disarm.");

            byte[] areadata;
            switch (panel_type)
            {
                // Probably right:
                // * Size in Elite 12 - 64:  8 bits (1 byte)
                // * Size in Elite 168:     16 bits (2 bytes)
                // * Size in Elite 640:     64 bits (8 bytes)
                case TexecomElitePanelType.Elite12_64:
                    areadata = new byte[1];
                    break;
                case TexecomElitePanelType.Elite168:
                    areadata = new byte[2];
                    break;
                case TexecomElitePanelType.Elite640:
                    areadata = new byte[8];
                    break;
                default:
                    throw new ArgumentOutOfRangeException("panel_type");
            }

            foreach (int areano in areas)
            {
                int byteidx = areano / 8, bitidx = areano % 8;

                if (byteidx > areadata.Length)
                    throw new ArgumentOutOfRangeException("areas");

                areadata[byteidx] |= (byte)(1 << bitidx - 1);
            }

            // Area 1 must be the rightmost bit
            for (int i = areadata.Length - 1; i >= 0; i--)
                payload.Add(areadata[i]);

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage SendKeyPress(PanelKeypadKey keypress)
        {
            List<byte> payload = new List<byte>
            {
                13,
                (byte)keypress
            };

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetLCDDisplay()
        {
            List<byte> payload = new List<byte>
            {
                13
            };

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage SetLCDDisplay(string text_to_display)
        {
            if (text_to_display.Length > 32)
                throw new ArgumentOutOfRangeException("text_to_display", "Maximum text length must not exceed 32 characters.");

            text_to_display = text_to_display.PadRight(32, ' ');

            List<byte> payload = new List<byte>
            {
                14
            };
            payload.AddRange(Encoding.ASCII.GetBytes(text_to_display));

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetLogPointer()
        {
            List<byte> payload = new List<byte>
            {
                15
            };

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetRawLogEvent(int log_number, int number_of_events)
        {
            if (log_number < 0 || log_number > 10000) throw new ArgumentOutOfRangeException("log_number");
            if (number_of_events < 0 || number_of_events > 10) throw new ArgumentOutOfRangeException("number_of_events");

            List<byte> payload = new List<byte>
            {
                16
            };
            payload.AddRange(BitConverter.GetBytes((ushort)log_number));
            payload.Add((byte)number_of_events);

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetTextLogEvent(int log_number)
        {
            if (log_number < 0 || log_number > 10000) throw new ArgumentOutOfRangeException("log_number");

            List<byte> payload = new List<byte>
            {
                17
            };
            payload.AddRange(BitConverter.GetBytes((ushort)log_number));

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetPanelIdentification()
        {
            List<byte> payload = new List<byte>
            {
                22
            };

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetDateTime()
        {
            List<byte> payload = new List<byte>
            {
                23
            };

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage SetDateTime(DateTime newdatetime)
        {
            List<byte> payload = new List<byte>
            {
                24,
                (byte)newdatetime.Day,
                (byte)newdatetime.Month,
                (byte)(newdatetime.Year - 2000),
                (byte)newdatetime.Hour,
                (byte)newdatetime.Minute,
                (byte)newdatetime.Second
            };

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetSystemPower()
        {
            List<byte> payload = new List<byte>
            {
                25
            };

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetUser(TexecomElitePanelType panel_type, int user_number)
        {
            if (user_number < 1) throw new ArgumentOutOfRangeException("user_number");
            if (panel_type != TexecomElitePanelType.Elite640 && user_number > 255) throw new ArgumentOutOfRangeException("user_number");

            List<byte> payload = new List<byte>
            {
                27
            };

            switch (panel_type)
            {
                default:
                    throw new ArgumentOutOfRangeException("panel_type");
                case TexecomElitePanelType.Elite12_64:
                case TexecomElitePanelType.Elite168:
                    payload.Add((byte)user_number);
                    break;
                case TexecomElitePanelType.Elite640:
                    payload.AddRange(BitConverter.GetBytes((ushort)user_number));
                    break;
            }

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage ArmAsUser(TexecomElitePanelType panel_type, int user_number)
        {
            if (user_number < 1) throw new ArgumentOutOfRangeException("user_number");
            if (panel_type != TexecomElitePanelType.Elite640 && user_number > 255) throw new ArgumentOutOfRangeException("user_number");

            List<byte> payload = new List<byte>
            {
                29
            };

            switch (panel_type)
            {
                default:
                    throw new ArgumentOutOfRangeException("panel_type");
                case TexecomElitePanelType.Elite12_64:
                case TexecomElitePanelType.Elite168:
                    payload.Add((byte)user_number);
                    break;
                case TexecomElitePanelType.Elite640:
                    payload.AddRange(BitConverter.GetBytes((ushort)user_number));
                    break;
            }

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }
        public static PanelMessage DisarmAsUser(TexecomElitePanelType panel_type, int user_number)
        {
            if (user_number < 1) throw new ArgumentOutOfRangeException("user_number");
            if (panel_type != TexecomElitePanelType.Elite640 && user_number > 255) throw new ArgumentOutOfRangeException("user_number");

            List<byte> payload = new List<byte>
            {
                30
            };

            switch (panel_type)
            {
                default:
                    throw new ArgumentOutOfRangeException("panel_type");
                case TexecomElitePanelType.Elite12_64:
                case TexecomElitePanelType.Elite168:
                    payload.Add((byte)user_number);
                    break;
                case TexecomElitePanelType.Elite640:
                    payload.AddRange(BitConverter.GetBytes((ushort)user_number));
                    break;
            }

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetZoneText(TexecomElitePanelType panel_type, int zone_number, int number_of_zones)
        {
            if (zone_number < 1) throw new ArgumentOutOfRangeException("zone_number");

            if ((panel_type == TexecomElitePanelType.Elite12_64 || panel_type == TexecomElitePanelType.Elite168) && zone_number > 255)
                throw new ArgumentException("Selected panel type does not support that many zones.", "start_zone");

            if (number_of_zones < 1 || number_of_zones > 4) throw new ArgumentOutOfRangeException("number_of_zones");

            List<byte> payload = new List<byte>
            {
                32
            };

            switch (panel_type)
            {
                default:
                    throw new ArgumentOutOfRangeException("panel_type");
                case TexecomElitePanelType.Elite12_64:
                case TexecomElitePanelType.Elite168:
                    payload.Add((byte)zone_number);
                    break;
                case TexecomElitePanelType.Elite640:
                    payload.AddRange(BitConverter.GetBytes((ushort)zone_number));
                    break;
            }

            payload.Add((byte)number_of_zones);

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetAreaText(int area_number, int number_of_areas)
        {
            if (area_number < 1) throw new ArgumentOutOfRangeException("area_number");

            if (number_of_areas < 1 || number_of_areas > 8) throw new ArgumentOutOfRangeException("number_of_areas");

            List<byte> payload = new List<byte>
            {
                34,
                (byte)area_number,
                (byte)number_of_areas
            };

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage GetAreaDetails(int area_number)
        {
            if (area_number < 1) throw new ArgumentOutOfRangeException("area_number");

            List<byte> payload = new List<byte>
            {
                35,
                (byte)area_number
            };

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }

        public static PanelMessage SetEventMessages(PanelEventMessages event_messages)
        {
            List<byte> payload = new List<byte>
            {
                37
            };

            payload.AddRange(BitConverter.GetBytes((ushort)event_messages));

            return new PanelMessage(PanelMessageType.Command, NextSequence(), payload.ToArray());
        }
    }
}