<?php

// TODO: Needs to match the path in "push_state" of "examplescript.sh"
// If you're using that.
define('STATE_DIR', '/opt/alarm/zones/');

// TODO: You need to compile "nudge.c" and make it SUID root (sorry)
// Then edit the path to the script and the arm file as appropriate.
// This program gets called to place a file in a protected directory
// which will trigger texmond to arm the panel.
define('ARMFILE_SCRIPT', '/opt/texmond/nudge /opt/texmond/armfile %s /var/run/texmond.pid');

// TODO: Put some sort of secure passphrase here. If you need enhanced
// security for the web interface, set up your webserver so it provides
// the level of security that makes you feel good (basic auth, etc.)
define('AUTHLINE', 'changeme');

/* TODO:
 * You will need to adjust $ZONES, $PART1OMIT, $PART2OMIT, $PART3OMIT
 * and $ARMMODES to match your zone and area configuration. You can
 * find all of the information you need quite easily using Wintex.
 *
 * In $ZONES, the key is the zone number and the value is any text you
 * like. It usually makes sense to use the same text as set in Wintex.
 *
 * The $PARTxOMIT arrays contain zone IDs that are omitted during part
 * arming. You need to configure these exactly as in the zone config
 * set in Wintex. The web interface will then prevent part arming if
 * the conditions for a part arm aren't being met.
 *
 * In $ARMMODES leave keys 4 and 0 alone (they are special). You can
 * change their text if you want. Keys 1, 2, and 3 are used to show
 * the respective text. Use the same text as you've set with Wintex.
 *
 * Note that this web interface currently only supports one area.
 */

$ZONES = array
(
    // -1 is always the panel, feel free to change its text, but don't
    // delete it.
    -1    => 'My Panel',
    // Customise starting here.
     9    => 'Keypad Main Door',
     10    => 'Keypad Back Door',
     11    => 'PIR Dining Room',
     12    => 'PIR Mstr Bedrm',
     13    => 'PIR Living Room',
     14    => 'PIR Guest Room',
     15    => 'PIR Staircase',
     16    => 'PIR Laundry Room',
     17    => 'PIR Study',
     18    => 'PIR Gallery',
     19    => 'Front Door',
     20    => 'Back Door',
     21    => 'Wnd Guest Room',
     22    => 'Wnd Storage Room',
     23    => 'Wnd Boiler Room',
     24    => 'Wnd Guest Bath',
     25    => 'Wnd LR Balcony',
     26    => 'Wnd LR Side Door',
     27    => 'Wnd Dining Room',
     28    => 'Wnd Kitchen L',
     29    => 'Wnd Kitchen R',
     30    => 'Wnd Mstr Bedrm L',
     31    => 'Wnd Mstr Bedrm R',
     32    => 'Wnd Master Bath',
     33    => 'Wnd Child Room',
     34    => 'Wnd Gallery',
     35    => 'Wnd Toilet',
     36    => 'Wnd Lounge',
     37    => 'Wnd Attic L',
     38    => 'Wnd Attic M',
     39    => 'Wnd Attic R',
     40    => 'Wireless Sounder',
);

$PART1OMIT = array(11, 12, 13, 14, 15, 16, 17, 18, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39);
$PART2OMIT = array(21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39);
$PART3OMIT = array(11, 12, 13, 14, 15, 16, 17, 18);

$ARMMODES = array
(
    4 => 'Home',
    0 => 'Away',
    1 => 'Sleep',
    2 => 'Ign. open window',
    3 => 'Arm windows only'
);

// If you need to translate the web interface, you can do so below:

define('T_PAGE_TITLE', 'Alarm System');
define('T_STATUS_ARMING', 'Arming...');
define('T_STATUS_DISARMING', 'Disarming...');
define('T_STATUS_LOADING', 'Loading...');
define('T_STATUS_OFFLINE', 'Offline');
define('T_AREA_ALARM', 'Alarm');
define('T_ZONE_BATTERY_LOW','Low Battery');
define('T_ZONE_FAULT', 'Faulty');
define('T_ZONE_TAMPER', 'Tamper');
define('T_ZONE_ACTIVE', 'Active');
define('T_ZONE_SECURE', 'Secure');
define('T_ZONE_ALARM', 'Alarm');
define('T_ZONE_UNKNOWN_STATE', '(Unknown)');
define('T_ALERT_CANNOT_ARM', 'You can\'t arm the alarm system because there are zones that prevent this.');
define('T_ALERT_CANNOT_PARTARM', 'You can\'t part arm the alarm system because there are zones that prevent this.');
define('T_ALERT_ALERADY_ARMED', 'The alarm system is already armed. Disarm it first.');
define('T_ALERT_CANNOT_ARM_PANEL_ISSUES', 'The alarm system has technical issues that must be resolved before it can be armed.');
define('T_ERROR_MISSING_MODE', 'The selected arm mode was not transmitted to the backend.');
define('T_ERROR_INVALID_MODE', 'The arm mode that was transmitted to the backend is invalid.');
