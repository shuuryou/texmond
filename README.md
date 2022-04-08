# texmond - Texecom Premier Elite Panel Event Monitoring

This is a daemon designed to be run with Linux and Mono that will take control of your Texecom Premier Elite panel and monitor its status. Status messages are logged to Syslog where you can act on them using a Syslog monitor. Additionally, it supports executing an event program or event script so that you can write custom actions that occur in response to events logged by your alarm panel. This could be things like zones triggering, alarm conditions, tamper conditions, etc.

A reasonably complete example event script is provided where you only have to fill in the blanks to a program that will send a text message (or email, or anything else you want) when alarm, tamper, or error conditions occur.

Also included is a simple web interface that allows you to see the status of zones and arm/disarm your panel remotely. The web interface is fully responsive and supports installation to the home screen on Apple and Android devices. Thanks to its responsive design, it also looks and works great on desktop computers.

This code has a long history, filled with a bit of happiness and a lot of frustration; I started reverse engineering Texecom's Simple API all the way back in 2019 and managed to get it to a working state after a few months of tinkering in my free time. Unfortunately, the local Subversion repository I used to document changes at the time got lost when Windows Update trashed my development computer and all I had was a recent checkout of the code. That's why there is no revision history here on GitHub that goes back that far. The "initial commit" here reflects the last major update from December 2020.

## About the Name

Originally, this project was called **texecomd**, but I assume Texecom, Ltd. wouldn't be very happy if I used their company name as the project's public name, I wanted to use **montexd**, but they already have a product called *MonTex*, so I chose the next best thing.

## Should I Use This?
This project is not affiliated, endorsed, recommended, or supported by Texecom, Ltd. in any way. Do not contact them if it screws up your panel or if things break otherwise. You are responsible for what you do. If you don't like tinkering and experimenting with your home alarm system, leave the job to a professional installer and ask them to set up [Texecom Cloud](https://www.texe.com/texecom-account/texecom-cloud/) for you. That's a great and affordable solution for casual users who want peace of mind.

But if you're a hacker and your crime is one of being overly curious, you've come to the right place. Have a seat and make yourself comfortable. 

# Screenshot

![Screenshot](https://github.com/shuuryou/texmond/blob/main/screenshot.jpg?raw=true)

# Example Log Output
```
Jan  8 12:29:36 pi texecomd[2388]: [ZONE EVENT] Zone 4 ("Test PIR") changed to state: Active. Alarmed? False. Auto bypassed? False. Manual bypassed? False. Failed test? False. Faulty? False. Masked? False.
Jan  8 12:29:36 pi texecomd[2388]: Event script: /opt/texmond/event.sh "ZONE" "4" "Test PIR" "Active" "FALSE" "FALSE" "FALSE" "FALSE" "FALSE" "FALSE"
Jan  8 12:29:36 pi texecomd[2388]: [ZONE EVENT] Zone 8 ("Test PIR 2") changed to state: Active. Alarmed? False. Auto bypassed? False. Manual bypassed? False. Failed test? False. Faulty? False. Masked? False.
Jan  8 12:29:36 pi texecomd[2388]: Event script: /opt/texmond/event.sh "ZONE" "8" "Test PIR 2" "Active" "FALSE" "FALSE" "FALSE" "FALSE" "FALSE" "FALSE"
Jan  8 12:29:43 pi texecomd[2388]: [ZONE EVENT] Zone 4 ("Test PIR") changed to state: Secure. Alarmed? False. Auto bypassed? False. Manual bypassed? False. Failed test? False. Faulty? False. Masked? False.
Jan  8 12:29:43 pi texecomd[2388]: Event script: /opt/texmond/event.sh "ZONE" "4" "Test PIR" "Secure" "FALSE" "FALSE" "FALSE" "FALSE" "FALSE" "FALSE"
Jan  8 12:29:52 pi texecomd[2388]: [ZONE EVENT] Zone 8 ("Test PIR 2") changed to state: Secure. Alarmed? False. Auto bypassed? False. Manual bypassed? False. Failed test? False. Faulty? False. Masked? False.
Jan  8 12:29:52 pi texecomd[2388]: Event script: /opt/texmond/event.sh "ZONE" "8" "Test PIR 2" "Secure" "FALSE" "FALSE" "FALSE" "FALSE" "FALSE" "FALSE"
Jan  8 12:37:25 pi texecomd[2388]: System voltage: 13.63V / Battery voltage: 13.63V / System current: 288.00mA / Battery charging current: 36.00mA / Reference voltage: 12.32V
Jan  8 12:37:25 pi texecomd[2388]: Event script: /opt/texmond/event.sh "POWER" "13.63" "13.63" "288" "36" "12.32"
Jan  8 12:52:31 pi texecomd[2388]: System voltage: 13.63V / Battery voltage: 13.56V / System current: 288.00mA / Battery charging current: 36.00mA / Reference voltage: 12.32V
Jan  8 12:52:31 pi texecomd[2388]: Event script: /opt/texmond/event.sh "POWER" "13.63" "13.56" "288" "36" "12.32"
```

# Prerequisites

1. A Texecom Premier Elite panel, such as a Premier Elite 48, Premier Elite 64-W, etc.
1. A serial port connection to your panel, e.g. using the official *Premier Elite USB-COM* cable if you can source one
1. A working Linux system — a Raspberry Pi works great for this!
1. A few hours of time to get things set up and working properly
1. Your panel's UDL code, access to the installer menu, and the installer's manual
1. A working copy of Wintex (will make your life a lot easier)

# Hardware Setup Guide

## Serial Port Connection

1. If you have a *Premier Elite USB-COM* cable, this is easy: just plug it in as described in the included manual (INS246-2)
1. If you **don't** have that cable, you will need to [buy an FTDI USB-Serial adapter](https://www.amazon.co.uk/DSD-TECH-SH-U09C2-Debugging-Programming/dp/B07TXVRQ7V) and then [follow this guide](https://archive.ph/jjMXk)

Once the serial port connection is working, you can proceed.

# Software Setup Guide

The software consists of these parts:
* The core daemon, written in C# and can technically also run under Windows, but this is not recommended
* The web interface, written in PHP, HTML, CSS, and JavaScript (jQuery); uses `EventSource` to receive live updates from the backend
* A tiny C program, `nudge.c`, to make arming via the web interface work
* An example event shell script written in Bash

All of the instructions that follow are valid for Devuan Linux Chimaera 4.0.

## Building the Daemon

You need Mono installed: `apt-get install mono-complete`. This will install more than you need, but the alternative is picking out the required Mono packages manually, which is rather painful.

1. If necessary, make the build script executable: `chmod +x build.sh`
1. Then run it: `./build.sh`

You should get one warning about xbuild not being able to understand some proprietary Microsoft stuff in the solution file; that's fine.

Now you're ready to copy the compiled files:

```bash
mkdir -p /opt/texmond/
cp texmond/bin/Release/texmond.* /opt/texmond/
cp texmond/bin/Release/settings.ini /etc/texmond.ini
```

If you want, you can install the init script:

```bash
cp initscript.sh /etc/init.d/texmond
chmod +x /etc/init.d/texmond
update-rc.d texmond defaults
update-rc.d texmond enable
```

## Building the Nudge C Program

You'll need to have GCC installed: `apt-get install gcc`

```bash
gcc nudge.c -o nudge
cp nudge /opt/texmond/
chmod u+s /opt/texmond/nudge #sorry
```

The `nudge` program needs to be SUID root so it can write into the protected /opt/texmond/ folder and send SIGUSR1 to the daemon. It's tiny and designed to do as little as possible as carefully as possible. The web interface uses this program to write the arm file. Without it, you'd have to make either PHP or the entire web server run as root, which would be *a lot worse*.

### Building a Debug Build on Windows

1. You need to [download Mono for Windows](https://www.mono-project.com/docs/getting-started/install/windows/) to get the referenced Mono.Posix library (so the Syslog enums resolve; Mono.Posix isn't used otherwise)
1. Afterwards open the solution file with Visual Studio, set the configuration to "Debug" and build as usual

Note that the debug build will look for the INI file in the directory the EXE file lives in. The release build looks for the INI file in `/etc/texmon.ini`.

Building a release build with Visual Studio is prohibited by a guard clause in `Program.cs`

## The Web Interface

### Installing

First you'll need a web server with PHP set up and running. How you do it is up to you, as there are [hundreds of tutorials on how to set up e.g. nginx + PHP](https://duckduckgo.com/?q=nginx+php+debian).

Then you essentially just have to copy the `smarthome` folder to your web server's public-facing directory, for example:

```bash
cp -r smarthome /var/www/html/htdocs/
```

Remember to add `?auth=changeme` to the URL when trying to access it for testing. The next section deals with changing `changeme` to something else.

### Setting up the Event Script

The example event script includes the necessary glue logic to make the web interface display state information. You'll need to copy it to the right place and make it executable:

```bash
cp examplescript.sh /opt/texmond/eventscript.sh
chmod +x /opt/texmond/eventscript.sh
```

Then open `/opt/texmond/eventscript.sh` and search for `TODO` to find the lines you need to modify.

Now set up the state folders and state files:

```bash
# You may need to adjust 40 to reflect the actual number of zones your panel supports.
for ((i=-1; i<=40; i++)); do
    mkdir -p "/opt/alarm/zones/$i"
    echo -n 0 > "/opt/alarm/zones/$i/state"
    echo -n 0 > "/opt/alarm/zones/$i/fault"
    echo -n 0 > "/opt/alarm/zones/$i/tamper"
    echo -n 0 > "/opt/alarm/zones/$i/battery"
done
```

Test that the event source works:

```bash
curl http://localhost/smarthome/eventsource.php?auth=changeme
```

You should get a JSON array as a response, followed by a colon every few seconds. Press CTRL+C to terminate curl.


# Configuration

## Settings INI File

Open `/etc/texmond.ini` in your favourite editor and follow the instructions in the comments. It's that easy.

If you've been following the instructions above, here's a suitable example where you just need to change the `Port` and `UDLCode` settings.

```ini
[texmond]
; Set to the COM port used by your Texecom Premier Elite panel
Port=/dev/ttyS0

; Do you want to sync the panel's time with your computer's time?
; Set to 1. Otherwise set to 0.
SyncTime=1

; The UDL code of your panel as configured in Wintex.
UDLCode=123456

; Set to 1 to output debug logs to Syslog. Useful while setting up
; the daemon. For normal operation, 0 is recommended.
DebugLog=1

; Specify the path to an executable (or shell script with the
; executable bit set) that will be called when an event happens.
EventScript=/opt/texmond/eventscript.sh

; Do you want to log when the event script is executed? Set to 1.
; Otherwise set to 0. Very useful when setting up the daemon.
LogEventScript=1

; If you want to arm the panel by writing to a file, set to the
; path that the daemon should watch. If you don't want this, leave
; it blank.
ArmFile=/opt/texmond/armfile

; Do you want to poll panel power information? Then set this to
; the number of minutes to wait between polls. If you don't want
; this, leave it blank.
; Panel power information tells you about mains current draw and
; battery health. It will be logged and the event script can act
; on the results.
PollPanelPower=1
```

## Web Interface

Open `config.php` in your favourite editor and follow the instructions in the comments. If you have a working installation of Texecom's Wintex tool, it will make setting up the config file a lot easier.

If you've set things up as described in this document, the `STATE_DIR` and `ARMFILE_SCRIPT` settings are fine as-is.

Set `AUTHLINE` to a long and secure password. To access the web interface, you need to add `?auth=`, followed by your `AUTHLINE` to the URL. This obviously isn't secure over HTTP, so you should also consider configuring your web server to enforce HTTPS. If you want to add even more security, you could configure your web server to enforce basic or digest authentication, but this can break things when pinning the web interface to the home screen on iPhone, because it doesn't want to remember the password for reasons only Apple knows.

Now you need to take care of the three arrays that follow and this is where Wintex will help you.

1. `$ZONES` needs to map the zone numbers of your panel to sensible labels for display purposes. Zone `-1` must not be removed, because it refers to the panel itself, but you may change its text as you please.
1. `$PART1OMIT`, `$PART2OMIT`, and `$PART3OMIT` need to contain the zone numbers that are omitted during the respective part arms. Go through all zones in Wintex and check if the respective checkboxes are set. If they are, add that zone number to the array(s).
1. `$ARMMODES` defines the labels shown on the web interface for the various arm modes. `4` is always disarm, `0` is always full arm, so don't remove those. `1`, `2`, and `3` specify the three different part arm suites and you should set the labels to the text shown in Wintex.

Below the arrays are all of the strings that are shown on the web interface. If you want to translate the web interface to your local language, you can edit them.

# Interacting with the Daemon

## Event Scripts

If you set `EventScript` to an executable or a shell script, the daemon will attempt to execute it whenever an event is logged by the panel.

The event script receives several command line arguments, the first of which is the type of event that was logged. This is followed by a number of event-specific arguments as documented below:

|Event|Arg1|Arg2|Arg3|Arg4|Arg5|Arg6|Arg7|Arg8|Arg9|Arg10|
|---|---|---|---|---|---|---|---|---|---|---|
|Critical error|`ERROR`|`PANEL` if there is a communications error (e.g. no response or timeout), `CRASH` when the daemon has crashed.|
|Power status report|`POWER`|System voltage|System current draw|Battery charging current|Reference voltage|
|Area log event|`LOG`|`AREA`|Event date/time as UNIX timestamp|Event type as a string (see `PanelEventLogEventType` enum)|Event group as a string (see `EventLogGroupType` enum)|String with names of affected areas, separated by commas|Parameter sent by the panel, can be ignored|Whether communication of the event was delayed; either `TRUE` or `FALSE`|Whether the event was sent to a connected communicator; either `TRUE` or `FALSE`|
|Zone log event|`LOG`|`ZONE`|Event date/time as UNIX timestamp|Event type as a string (see `PanelEventLogEventType` enum)|Event group as a string (see `EventLogGroupType` enum)|Zone number|Zone text programmed into panel|Whether communication of the event was delayed; either `TRUE` or `FALSE`|Whether the event was sent to a connected communicator; either `TRUE` or `FALSE`|
|Keypad logon event|`USER`|User number|User's name as programmed into panel or `<ENGINEER>` if user number is 0|Either `USER CODE` if logged in using PIN, or `TAG` if logged on using proximity tag|
|Programmable output changed event|`OUTPUT`|Output number|State the output changed to (0 or 1)|
|Area state change event|`AREA`|Area number|Area text as programmed into panel|Area state as a string (see `PanelAreaStates` enum)|
|Zone state change event|`ZONE`|Zone number|Zone text are programmed into panel|Zone state as a string (see `PanelZoneType` enum)|`TRUE` if the zone is in alarm state, otherwise `FALSE`|`TRUE` if the zone is auto-bypassed, otherwise `FALSE`|`TRUE` if the zone is in a failed test state, otherwise `FALSE`|`TRUE` if the zone was manually bypassed, otherwise `FALSE`|`TRUE` if the zone is in fault, otherwise `FALSE`|`TRUE` if the zone is marked, otherwise `FALSE`
|Debug event|`DEBUG`|Raw debug message as a string (you will never see these events unless you work at Texecom, Ltd.)|

## Arm Files

If you set `ArmFile` to a file path in `/etc/texmond.ini` then the daemon will regularly check if that file exists and examine its contents.

The content of the file has to be the command to perform, followed by a space, followed by one or more area numbers. Separate each area number with a space.

Send `SIGUSR1` to the daemon to make it wake up and process the arm file immediately. Otherwise it will process the arm file the next time the event loop is scheduled to wake up.

Place the following contents into the file to trigger arming or disarming:

### Examples

```
PART1ARM 1 2
```
This will part-arm areas 1 and 2 with part-arm set 1.

```
FULLARM 1
```
This will fully arm area 1.

```
DISARM 1 2 3 4
```
This will disarm areas 1, 2, 3 and 4.

The following commands are supported:
|Command|Description|
|---|---|
|`PART1ARM`|Part-arm the specified area(s) with part-arm zone set 1.|
|`PART2ARM`|Part-arm the specified area(s) with part-arm zone set 2.|
|`PART3ARM`|Part-arm the specified area(s) with part-arm zone set 3.|
|`FULLARM`|Fully arm the specified area(s)|
|`DISARM`|Disarm the specified area(s)|


# Help Wanted

Do you own a Premier Elite panel other than a Premier Elite 48 or Premier Elite 64-W? Please try this software and report any bugs, because I had to make some assumptions on how these panels work and they may not be correct. I'm especially interested in feedback from property owners who have a Premier Elite 168 or a Premier Elite 640 that is set up so it uses the maximum number of supported areas and zones.

Of course, I'm also interested in feedback and bug reports from Premier Elite 48 and  Premier Elite 64-W users. :smiley:
