#!/bin/bash
export PATH='/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin'

# This script is an EXAMPLE. It is by no means complete or guaranteed to
# work. The author is not responsible if it fails to notify you and your
# stuff gets stolen by an intruder.

# TODO: If you do not want to use the smarthome web interface, remove
# the line    smarthome "$@"    at the bottom of this script.

# TODO: If you ONLY want to use the smarthome web interface, remove
# the line    main "$@"    at the bottom of this script.

# TODO: If you want problems to be logged to syslog instead of stdout,
# uncomment this.
#exec 1> >(logger -i -t NOTIFYSCRIPT) 2>&1

function main {
    # TODO: Insert the command used to send a text message. It is called
    # with three arguments: $1=sender, $2=number (see NRS below), $3=text
    SMSCOMMAND="/bin/true"

    # TODO: Adjust as necessary. $NRS is an array with phone numbers to
    # alert when an event of note occurs. Make sure the format matches
    # that expected by your $SMSCOMMAND. $SENDER is the sender of the
    # text messages shown on your phone (if your SMS service supports
    # this). $PREAMBLE is placed in front of every message and can be
    # used e.g. to identify the property via its address.
    NRS=("440000123123" "440000456456")
    SENDER="Alarm System"
    PREAMBLE="71 Cherry Court SO53 5PD"

    NOTIFY=

    case "$1" in
    "ERROR")
        # The daemon crashed
        REASON=$2
        NOTIFY="$PREAMBLE: SYSTEM FAILURE ($REASON)"
        ;;
    "AREA")
        NUMBER=$2
        TEXT=$3
        STATE=$4

        if [ "$STATE" == "AreaInAlarm" ]
        then
            NOTIFY="$PREAMBLE: AREA IN ALARM"
        fi
        ;;
    "ZONE")
        NUMBER=$2
        TEXT=$3
        STATE=$4
        ALARMED=$5
        AUTOBYPASSED=$6
        FAILEDTEST=$7
        MANUALBYPASSED=$8
        ZONEINFAULT=$9
        ZONEMASKED=${10}

        if [ "$STATE" == "Tampered" ]
        then
            NOTIFY="$PREAMBLE: Tamper in zone $NUMBER ($TEXT)"
        elif [ "$ALARMED" == "TRUE" ]
        then
            NOTIFY="$PREAMBLE: Alarm in zone $NUMBER ($TEXT)"
        elif [ "$ZONEINFAULT" == "TRUE" ]
        then
            NOTIFY="$PREAMBLE: Fault in zone $NUMBER ($TEXT)"
        fi
        ;;
    "LOG")
        EVENTKIND=$2
        DATE=$3
        EVENTTYPE=$4
        GROUPTYPE=$5
        AFFECTEDAREAS=$6
        PARAMETER=$7
        DELAYED=$8
        COMMUNICATED=$9

        if [ "$EVENTKIND" != "AREA" ]
        then
            return 0
        fi

        if [ "$EVENTTYPE" == "ACFail" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: mains power out"
        elif [ "$EVENTTYPE" == "PowerUnitFailure" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: power supply dead"
        elif [ "$EVENTTYPE" == "PSUBatteryFail" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: backup battery dead"
        elif [ "$EVENTTYPE" == "PowerOPFault" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: power output abnormal"
        elif [ "$EVENTTYPE" == "PSUACFail" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: power supply reports mains power out"
        elif [ "$EVENTTYPE" == "MainsOverVoltage" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: voltage too high"
        elif [ "$EVENTTYPE" == "PSULowOutputFail" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: power supply voltage too low"
        elif [ "$EVENTTYPE" == "ExpanderLowVoltage" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: expander voltage too low"
        elif [ "$EVENTTYPE" == "LowBattery" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: backup battery is weak"
        elif [ "$EVENTTYPE" == "TelephoneLineFault" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: telephone line is not working"
        elif [ "$EVENTTYPE" == "CommunicationPort" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: communicaticator is not working"
        elif [ "$EVENTTYPE" == "BatteryChargerFault" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: battery charger is not working"
        elif [ "$EVENTTYPE" == "GSMTamper" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in panel: GSM communicator tamper"
        elif [ "$EVENTTYPE" == "RFDeviceLowBattery" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: FAULT in zone $PARAMETER (low battery)"
        elif [ "$EVENTTYPE" == "ZoneTamper" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in zone $PARAMETER"
        elif [ "$EVENTTYPE" == "ZoneMasked" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in zone $PARAMETER (masked)"
        elif [ "$EVENTTYPE" == "PSUTamper" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in panel: power supply"
        elif [ "$EVENTTYPE" == "PanelBoxTamper" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in panel: panel box"
        elif [ "$EVENTTYPE" == "BellTamper" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in panel: bell box"
        elif [ "$EVENTTYPE" == "AuxiliaryTamper" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in panel: auxiliary"
        elif [ "$EVENTTYPE" == "ExpanderTamper" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in panel: expander unit"
        elif [ "$EVENTTYPE" == "KeypadTamper" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in panel: keypad"
        elif [ "$EVENTTYPE" == "ExpanderTroubleNetworkError" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in panel: expander network error"
        elif [ "$EVENTTYPE" == "RemoteKeypadTroubleNetworkError" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in panel: remote keypad network error"
        elif [ "$EVENTTYPE" == "RadioJamming" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            NOTIFY="$PREAMBLE: TAMPER in panel: RF communication is being jammed"
        fi
        ;;
    esac

    [ -n "$NOTIFY" ] &&
        for nr in "${NRS[@]}"
        do
            "$SMSCOMMAND" "$SENDER" "$nr" "$NOTIFY"
        done

    return 0
}

###############################################################################

# arm states
# 0: full armed
# 1: part arm 1
# 2: part arm 2
# 3: part arm 3
# 4: disarmed
# 5: alarm has been triggered

function smarthome {
    case "$1" in
    "ERROR")
        REASON=$2
        ;;
    "AREA")
        NUMBER=$2
        TEXT=$3
        STATE=$4

        case "$STATE" in
        "AreaDisarmed")
            push_state "STATE" -1 4
            ;;
        "AreaArmed")
            push_state "STATE" -1 0
            ;;
        "AreaInAlarm")
            push_state "STATE" -1 5
            ;;
        esac
        ;;
    "ZONE")
        NUMBER=$2
        TEXT=$3
        STATE=$4
        ALARMED=$5
        AUTOBYPASSED=$6
        FAILEDTEST=$7
        MANUALBYPASSED=$8
        ZONEINFAULT=$9
        ZONEMASKED=${10}

        if [ "$STATE" == "Secure" ]
        then
            push_state "STATE" "$NUMBER" 0
            push_state "TAMPER" "$NUMBER" 0
        elif [ "$STATE" == "Tampered" ]
        then
            push_state "TAMPER" "$NUMBER" 1
        elif [ "$STATE" == "Active" ]
        then
            push_state "STATE" "$NUMBER" 1
        fi

        if [ "$ZONEINFAULT" == "TRUE" ]
        then
            push_state "FAULT" "$NUMBER" 1
        fi

        if [ "$ALARMED" == "TRUE" ]
        then
            push_state "STATE" "$NUMBER" 5
        fi
        ;;
    "LOG")
        EVENTKIND=$2
        DATE=$3
        EVENTTYPE=$4
        GROUPTYPE=$5
        AFFECTEDAREAS=$6
        PARAMETER=$7
        DELAYED=$8
        COMMUNICATED=$9

        if [ "$EVENTKIND" != "AREA" ]
        then
            return 0
        fi

        if [ "$EVENTTYPE" == "PartArm1" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 1
        elif [ "$EVENTTYPE" == "PartArm2" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 2
        elif [ "$EVENTTYPE" == "PartArm3" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 3
        fi

        if [ "$EVENTTYPE" == "PartArm1" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 1
        elif [ "$EVENTTYPE" == "PartArm2" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 2
        elif [ "$EVENTTYPE" == "PartArm3" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 3
        fi

        if [ "$EVENTTYPE" == "QuickPartArm1" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 1
        elif [ "$EVENTTYPE" == "QuickPartArm2" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 2
        elif [ "$EVENTTYPE" == "QuickPartArm3" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 3
        fi

        if [ "$EVENTTYPE" == "TimedPartArm1" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 1
        elif [ "$EVENTTYPE" == "TimedPartArm2" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 2
        elif [ "$EVENTTYPE" == "TimedPartArm3" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 3
        fi

        if [ "$EVENTTYPE" == "RemotePartArm1" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 1
        elif [ "$EVENTTYPE" == "RemotePartArm2" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 2
        elif [ "$EVENTTYPE" == "RemotePartArm3" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 3
        elif [ "$EVENTTYPE" == "RemoteOpenClose" ] && [ "$GROUPTYPE" == "Close" ]
        then
            push_state "STATE" -1 0
        fi

        if [ "$EVENTTYPE" == "SupervisionFault" ] ||
         [ "$EVENTTYPE" == "ZoneFault" ]
        then
            if [ "$GROUPTYPE" == "Alarm" ]
            then
                push_state "FAULT" "$PARAMETER" 1
            elif [ "$GROUPTYPE" == "Restore" ]
            then
                push_state "FAULT" "$PARAMETER" 0
            fi
        fi

        if [ "$EVENTTYPE" == "RFDeviceLowBattery" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            push_state "BATTERY" "$PARAMETER" 1
        elif [ "$EVENTTYPE" == "RFDeviceLowBattery" ] && [ "$GROUPTYPE" == "Restore" ]
        then
            push_state "BATTERY" "$PARAMETER" 0
        fi

        if [ "$EVENTTYPE" == "ZoneTamper" ] && [ "$GROUPTYPE" == "TamperAlarm" ]
        then
            push_state "TAMPER" "$PARAMETER" 1
        elif [ "$EVENTTYPE" == "ZoneTamper" ] && [ "$GROUPTYPE" == "Restore" ]
        then
            push_state "TAMPER" "$PARAMETER" 0
        fi

        if [ "$EVENTTYPE" == "ZoneMasked" ] && [ "$GROUPTYPE" == "Alarm" ]
        then
            push_state "TAMPER" "$PARAMETER" 1
        elif [ "$EVENTTYPE" == "ZoneMasked" ] && [ "$GROUPTYPE" == "Restore" ]
        then
            push_state "TAMPER" "$PARAMETER" 0
        fi

        if [ "$EVENTTYPE" == "PSUTamper" ] ||
         [ "$EVENTTYPE" == "PanelBoxTamper" ] ||
         [ "$EVENTTYPE" == "BellTamper" ] ||
         [ "$EVENTTYPE" == "AuxiliaryTamper" ] ||
         [ "$EVENTTYPE" == "ExpanderTamper" ] ||
         [ "$EVENTTYPE" == "KeypadTamper" ] ||
         [ "$EVENTTYPE" == "ExpanderTroubleNetworkError" ] ||
         [ "$EVENTTYPE" == "RemoteKeypadTroubleNetworkError" ] ||
         [ "$EVENTTYPE" == "RadioJamming" ]
        then
            if [ "$GROUPTYPE" == "TamperAlarm" ]
            then
                push_state "TAMPER" -1 1
            elif [ "$GROUPTYPE" == "TamperRestore" ]
            then
                push_state "TAMPER" -1 0
            fi
        fi

        if [ "$EVENTTYPE" == "PSUBatteryFail" ] ||
         [ "$EVENTTYPE" == "PowerUnitFailure" ] ||
         [ "$EVENTTYPE" == "ACFail" ] ||
         [ "$EVENTTYPE" == "PowerOPFault" ] ||
         [ "$EVENTTYPE" == "PSUACFail" ] ||
         [ "$EVENTTYPE" == "MainsOverVoltage" ] ||
         [ "$EVENTTYPE" == "PSULowOutputFail" ] ||
         [ "$EVENTTYPE" == "ExpanderLowVoltage" ] ||
         [ "$EVENTTYPE" == "LowBattery" ] ||
         [ "$EVENTTYPE" == "TelephoneLineFault" ] ||
         [ "$EVENTTYPE" == "CommunicationPort" ] ||
         [ "$EVENTTYPE" == "BatteryChargerFault" ] ||
         [ "$EVENTTYPE" == "GSMTamper" ]
        then
            if [ "$GROUPTYPE" == "Alarm" ]
            then
                push_state "FAULT" -1 1
            elif [ "$GROUPTYPE" == "Restore" ]
            then
                push_state "FAULT" -1 0
            fi
        fi
        ;;
    esac

    return 0
}

function push_state {
    ! is_number "$2" && return 1
    ! is_number "$3" && return 2

    STATEDIR="/opt/alarm/zones"

    mkdir -p "$STATEDIR/$2"

    case "$1" in
    "STATE")
        echo -n "$3" > "$STATEDIR/$2/state"
        ;;
    "TAMPER")
        echo -n "$3" > "$STATEDIR/$2/tamper"
        ;;
    "BATTERY")
        echo -n "$3" > "$STATEDIR/$2/battery"
        ;;
    "FAULT")
        echo -n "$3" > "$STATEDIR/$2/fault"
        ;;
    esac

    return $?
}

function is_number {
    re='^-?[0-9]+$'

    if [[ $1 =~ $re ]]
    then
         return 0
    fi

    return 1
}

###############################################################################

main "$@"

smarthome "$@"

exit 0
