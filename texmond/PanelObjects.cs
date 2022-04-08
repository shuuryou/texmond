using System;
using System.Collections;
using System.Globalization;

namespace texmond
{
    public enum PanelMessageType
    {
        None = 0,
        Command = 'C',
        Response = 'R',
        Unsolicited = 'M'
    }

    public enum TexecomElitePanelType
    {
        Elite12_64 = 0,
        Elite168 = 1,
        Elite640 = 2
    }

    public enum PanelZoneStates
    {
        Secure = 0,
        Active = 1,
        Tampered = 2,
        Short = 3
    }

    public enum PanelAreaStates
    {
        AreaDisarmed = 0,
        AreaInExit = 1,
        AreaInEntry = 2,
        AreaArmed = 3,
        AreaPartArmed = 4,
        AreaInAlarm = 5
    }

    public enum PanelZoneType
    {
        ZoneNotUsed = 0,
        EntryExit1 = 1,
        EntryExit2 = 2,
        Guard = 3,
        GuardAccess = 4,
        TwentyFourHourAudible = 5,
        TwentyFourHourSilent = 6,
        PAAudible = 7,
        PASilent = 8,
        Fire = 9,
        Medical = 10,
        TwentyFourHourGas = 11,
        Auxiliary = 12,
        Tamper = 13,
        ExitTerminator = 14,
        MomentaryKey = 15,
        LatchedKey = 16,
        Security = 17,
        OmitKey = 18,
        Custom = 19,
        ConfirmedPAAudible = 20,
        ConfirmedPASilent = 21
    }

    [Flags]
    public enum PanelEventMessages : ushort
    {
        Debug = 1,
        ZoneEventMessages = 2,
        AreaEventMessages = 4,
        OutputEventMessages = 8,
        UserEventMessages = 16,
        LogEvents = 32
    }

    [Flags]
    public enum PanelUserStates : byte
    {
        UserCodeEntered = 1,
        TagPresented = 2
    }

    [Flags]
    public enum PanellUserModifiers : byte
    {
        AllowOwnCodeChange = 0,
        AllowChangeChimeZone = 1,
        AllowChangeTimers = 2,
        AllowSystemTests = 3,
        AllowUserSetup = 4,
        AllowUDLEngineerAccess = 5,
        AllowNewEngineerAccess = 6,
        AllowNVMLocking = 7
    }

    [Flags]
    public enum PanelUserLocks : byte
    {
        ControlTimer1 = 0,
        ControlTimer2 = 1,
        ControlTimer3 = 2,
        ControlTimer4 = 3,
        ControlTimer5 = 4,
        ControlTimer6 = 5,
        CstOP2ATAG = 6,
        CstOP2BCODE = 7
    }

    [Flags]
    public enum PanelUserConfig : ushort
    {
        AllowUserMenu = 0,
        EngineerProgramming = 1,
        DualCode = 2,
        VacationCode = 3,
        AtivateDoorStrike = 4,
        CallRemotePC = 5,
        DuressCode = 6,
        ReportOpenClose = 7,
        AllowArming = 8,
        AllowDisarming = 9,
        AllowOmitting = 10,
        AllowEngineerReset = 11,
        LocalArming = 12,
        LocalDisarming = 13,
        AutoYes = 14,
        DisarmFirst = 15
    }

    public enum PanelEventLogKind
    {
        None = 0,
        ZoneEvent = 1,
        NonZoneEvent = 2
    }

    public enum PanelEventLogEventType
    {
        // Event types (zones)
        None = 0,
        EntryExit1 = 1,
        EntryExit2 = 2,
        Interior = 3,
        Perimeter = 4,
        TwentyFourHourAudible = 5,
        TwentyFourHourSilent = 6,
        AudiblePA = 7,
        SilentPA = 8,
        FireAlarm = 9,
        Medical = 10,
        TwentyFourHourGasAlarm = 11,
        AuxiliaryAlarm = 12,
        TwentyFourHourTamperAlarm = 13,
        ExitTerminator = 14,
        KeyswitchMomentary = 15,
        KeyswitchLatching = 16,
        SecurityKey = 17,
        OmitKey = 18,
        CustomAlarm = 19,
        ConfirmedPAAudible = 20,
        ConfirmedPASilent = 21,
        // Event types (non-zone)
        KeypadMedical = 2,
        KeypadFire = 23,
        KeypadAudiblePA = 24,
        KeypadSilentPA = 25,
        DuressCode = 26,
        AlarmActive = 27,
        BellActive = 28,
        ReArm = 29,
        VerifiedCrossZone = 30,
        UserCode = 31,
        ExitStarted = 32,
        ExitErrorArmingFailed = 33,
        EntryStarted = 34,
        PartArmSuite = 35,
        ArmedwithLineFault = 36,
        OpenCloseAwayArmed = 37,
        PartArmed = 38,
        AutoOpenClose = 39,
        AutoArmDeferred = 40,
        OpenAfterAlarmAlarmAbort = 41,
        RemoteOpenClose = 42,
        QuickArm = 43,
        RecentClosing = 44,
        ResetAfterAlarm = 45,
        PowerOPFault = 46,
        ACFail = 47,
        LowBattery = 48,
        SystemPowerUp = 49,
        MainsOverVoltage = 50,
        TelephoneLineFault = 51,
        FailtoCommunicate = 52,
        DownloadStart = 53,
        DownloadEnd = 54,
        LogCapacityAlert80Percent = 55,
        DateChanged = 56,
        TimeChanged = 57,
        InstallerProgrammingStart = 58,
        InstallerProgrammingEnd = 59,
        PanelBoxTamper = 60,
        BellTamper = 61,
        AuxiliaryTamper = 62,
        ExpanderTamper = 63,
        KeypadTamper = 64,
        ExpanderTroubleNetworkError = 65,
        RemoteKeypadTroubleNetworkError = 66,
        FireZoneTamper = 67,
        ZoneTamper = 68,
        KeypadLockout = 69,
        CodeTamperAlarm = 70,
        SoakTestAlarm = 71,
        ManualTestTransmission = 72,
        AutomaticTestTransmission = 73,
        UserWalkTestStartEnd = 74,
        NVMDefaultsLoaded = 75,
        FirstKnock = 76,
        DoorAccess = 77,
        PartArm1 = 78,
        PartArm2 = 79,
        PartArm3 = 80,
        AutoArmingStarted = 81,
        ConfirmedAlarm = 82,
        ProxTag = 83,
        AccessCodeChangedDeleted = 84,
        ArmFailed = 85,
        LogCleared = 86,
        iDLoopShorted = 87,
        CommunicationPort = 88,
        TAGSystemExitBattOK = 89,
        TAGSystemExitBattLOW = 90,
        TAGSystemEntryBattOK = 91,
        TAGSystemEntryBattLOW = 92,
        MicrophoneActivated = 93,
        AVClearedDown = 94,
        MonitoredAlarm = 95,
        ExpanderLowVoltage = 96,
        SupervisionFault = 97,
        PAfromRemoteFOB = 98,
        RFDeviceLowBattery = 99,
        SiteDataChanged = 100,
        RadioJamming = 101,
        TestCallPassed = 102,
        TestCallFailed = 103,
        ZoneFault = 104,
        ZoneMasked = 105,
        FaultsOverridden = 106,
        PSUACFail = 107,
        PSUBatteryFail = 108,
        PSULowOutputFail = 109,
        PSUTamper = 110,
        DoorAccessReported = 111,
        CIEReset = 112,
        RemoteCommand = 113,
        UserAdded = 114,
        UserDeleted = 115,
        ConfirmedPA = 116,
        UserAcknowledged = 117,
        PowerUnitFailure = 118,
        BatteryChargerFault = 119,
        ConfirmedIntruder = 120,
        GSMTamper = 121,
        RadioConfigFailure = 122,
        QuickPartArm1 = 204,
        QuickPartArm2 = 205,
        QuickPartArm3 = 206,
        RemotePartArm1 = 207,
        RemotePartArm2 = 208,
        RemotePartArm3 = 209,
        TimedPartArm1 = 210,
        TimedPartArm2 = 211,
        TimedPartArm3 = 212
    }

    public enum PanelEventLogGroupType
    {
        NotReported = 0,
        PriorityAlarm = 1,
        PriorityAlarmRestore = 2,
        Alarm = 3,
        Restore = 4,
        Open = 5,
        Close = 6,
        Bypassed = 7,
        Unbypassed = 8,
        MaintenanceAlarm = 9,
        MaintenanceRestore = 10,
        TamperAlarm = 11,
        TamperRestore = 12,
        TestStart = 13,
        TestEnd = 14,
        Disarmed = 15,
        Armed = 16,
        Tested = 17,
        Started = 18,
        Ended = 19,
        Fault = 20,
        Omitted = 21,
        Reinstated = 22,
        Stopped = 23,
        Start = 24,
        Deleted = 25,
        Active = 26,
        NotUsed = 27,
        Changed = 28,
        LowBattery = 29,
        Radio = 30,
        Deactivated = 31,
        Added = 32,
        BadAction = 33,
        PATimerReset = 34,
        PAZoneLockout = 35,
    }

    public enum PanelKeypadKey
    {
        Key1 = 0x01,
        Key2 = 0x02,
        Key3 = 0x03,
        Key4 = 0x04,
        Key5 = 0x05,
        Key6 = 0x06,
        Key7 = 0x07,
        Key8 = 0x08,
        Key9 = 0x09,
        Key0 = 0x0A,
        Omit = 0x0B,
        Menu = 0x0C,
        Yes = 0x0D,
        Part =0x0E,
        No = 0x0F,
        Area = 0x10,
        Fire = 0x11,
        PA = 0x12,
        Medical = 0x13,
        Chime = 0x14,
        Reset = 0x15,
        Up = 0x16,
        Down = 0x17
    }

    public enum PanelAreaArmType
    {
        FullArm = 0,
        PartArm1 = 1,
        PartArm2 = 2,
        PartArm3 = 3
    }

    public sealed class PanelZoneState
    {
        public PanelZoneState(PanelZoneStates zone_state, bool zone_in_fault, bool failed_test, bool alarmed,
            bool manual_bypassed, bool auto_bypassed, bool zone_masked)
        {
            ZoneState = zone_state;
            ZoneInFault = zone_in_fault;
            FailedTest = failed_test;
            Alarmed = alarmed;
            ManualBypassed = manual_bypassed;
            AutoBypassed = auto_bypassed;
            ZoneMasked = zone_masked;
        }

        public PanelZoneStates ZoneState { get; private set; }
        public bool ZoneInFault { get; private set; }
        public bool FailedTest { get; private set; }
        public bool Alarmed { get; private set; }
        public bool ManualBypassed { get; private set; }
        public bool AutoBypassed { get; private set; }
        public bool ZoneMasked { get; private set; }

        public static PanelZoneState FromBitmap(byte bitmap)
        {
            PanelZoneStates state = (PanelZoneStates)(bitmap & 0x03);
            BitArray ba = new BitArray(new byte[] { bitmap });

            return new PanelZoneState(state, ba.Get(2), ba.Get(3), ba.Get(4), ba.Get(5), ba.Get(6), ba.Get(7));
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Zone {0}", this.ZoneState);
        }
    }


    public sealed class PanelAreaState
    {
        public PanelAreaState(PanelAreaStates area_state)
        {
            AreaState = area_state;
        }

        public PanelAreaState(byte bitmap)
        {
            AreaState = (PanelAreaStates)bitmap;
        }

        public PanelAreaStates AreaState { get; private set; }

        public override string ToString()
        {
            return this.AreaState.ToString();
        }
    }
}