using System;
using System.Collections;
using System.Globalization;
using System.IO;

namespace texmond
{
    public sealed class PanelRawLogEvent
    {
        public PanelRawLogEvent(byte[] logevent)
        {
            if (logevent == null) throw new ArgumentNullException("payload");

            EventKind = logevent[0] > 21 ? PanelEventLogKind.NonZoneEvent : PanelEventLogKind.ZoneEvent;

            EventType = (PanelEventLogEventType)logevent[0];

            {
                // Bits 6 and 7 are flags, so turn those off temporarily
                byte gt = logevent[1];
                gt &= byte.MaxValue ^ (1 << 6);
                gt &= byte.MaxValue ^ (1 << 7);
                GroupType = (PanelEventLogGroupType)gt;
            }

            CommunicationIsDelayed = (logevent[1] & (1 << 6)) != 0;
            EventIsCommunicated = (logevent[1] & (1 << 7)) != 0;

            switch (logevent.Length)
            {
                case 8:
                    // Premier 12/24/48/88 Event Log Data (8 bytes)
                    Parameter = logevent[2];
                    AreaBitmap = new BitArray(new byte[] { logevent[3] });
                    Date = ConvertDateTime(ref logevent, 4);
                    break;
                case 9:
                    // Premier 168 Event Log Data (9 bytes)
                    Parameter = logevent[2];
                    AreaBitmap = new BitArray(new byte[] { logevent[3], logevent[8] });
                    Date = ConvertDateTime(ref logevent, 4);
                    break;
                case 16:
                    // Premier 640 Event Log Data (16 bytes)
                    Parameter = BitConverter.ToUInt16(logevent, 2);
                    AreaBitmap = new BitArray(new byte[] { logevent[4], logevent[5],
                        logevent[6], logevent[7], logevent[8], logevent[9],
                        logevent[10], logevent[11] });
                    Date = ConvertDateTime(ref logevent, 12);
                    break;
                default:
                    throw new InvalidDataException("Unexpected log event length.");
            }
        }

        private static DateTime ConvertDateTime(ref byte[] value, int startIndex)
        {
            uint ts = BitConverter.ToUInt32(value, startIndex);

            byte year, day, hour, month, minute, second;

            year = (byte)(ts >> 26);
            day = (byte)(ts << 6 >> (6 + 21));
            hour = (byte)(ts << 11 >> (11 + 16));
            month = (byte)(ts << 16 >> (16 + 12));
            minute = (byte)(ts << 20 >> (20 + 6));
            second = (byte)(ts << 26 >> 26);

            return new DateTime(2000 + year, month, day, hour, minute, second);
        }

        public PanelEventLogKind EventKind { get; private set; }
        public PanelEventLogEventType EventType { get; private set; }
        public PanelEventLogGroupType GroupType { get; private set; }
        public bool CommunicationIsDelayed { get; private set; }
        public bool EventIsCommunicated { get; private set; }
        public int Parameter { get; private set; }
        private BitArray AreaBitmap { get; set; }
        public DateTime Date { get; private set; }

        public bool IsAreaAffected(int area)
        {
            if (area < 1 || area > AreaBitmap.Length)
                throw new ArgumentOutOfRangeException("area");

            return !AreaBitmap.Get(area - 1);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "Date: {0:F}: Kind: {1} Type: {2} Group: {3} Parameter: {4}",
                Date, EventKind, EventType, GroupType, Parameter);
        }
    }
}