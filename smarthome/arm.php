<?php
define('IN_SMARTHOME', TRUE);
require_once('init.php');

if (!isset($_POST['MODE']))
{
    http_response_code(500);
    echo __('T_ERROR_MISSING_MODE', array(), FALSE);
    exit();
}

/*
# 0: full arm
# 1: part arm 1
# 2: part arm 2
# 3: part arm 3
# 4: disarmed
# 5: alarm
*/

$MODE = $_POST['MODE'];

if (!is_numeric($MODE))
{
    http_response_code(500);
    echo __('T_ERROR_INVALID_MODE', array(), FALSE);
    exit();
}

$MODE = intval($MODE);

if (!array_key_exists($MODE, $ARMMODES))
{
    http_response_code(500);
    echo __('T_ERROR_INVALID_MODE', array(), FALSE);
    exit();
}

if ($MODE == 4)
{
    PutArmFile('disarm 1');
    exit();
}

$State = ReadInt(file_build_path(STATE_DIR, -1, 'state'));

if ($State != 4)
{
    http_response_code(500);
    echo __('T_ALERT_ALERADY_ARMED');
    exit();
}

$Tamper = ReadInt(file_build_path(STATE_DIR, -1, 'tamper'));
$Fault = ReadInt(file_build_path(STATE_DIR, -1, 'fault'));
$Battery = ReadInt(file_build_path(STATE_DIR, -1, 'battery'));

if ($Tamper != 0 || $Fault != 0 || $Battery != 0)
{
    http_response_code(500);
    echo __('T_ALERT_CANNOT_ARM_PANEL_ISSUES', array(), FALSE);
    exit();
}

$ActiveZones = array();

foreach ($ZONES as $ID => $Name)
{
    if ($ID == -1)
        continue;

    $State = ReadInt(file_build_path(STATE_DIR, $ID, 'state'));
    $Tamper = ReadInt(file_build_path(STATE_DIR, $ID, 'tamper'));
    $Fault = ReadInt(file_build_path(STATE_DIR, $ID, 'fault'));

    if ($State == 0 && $Tamper == 0 && $Fault == 0)
        continue;

    array_push($ActiveZones, $ID);
}

if ($MODE == 0 && count($ActiveZones) != 0)
{
    http_response_code(500);
    echo __('T_ALERT_CANNOT_ARM', array(), FALSE);
    exit();
}

$Omits = array();

switch ($MODE)
{
    case 1:
        $Omits = $PART1OMIT;
        break;
    case 2:
        $Omits = $PART2OMIT;
        break;
    case 3:
        $Omits = $PART3OMIT;
        break;
}

foreach ($ActiveZones as $v)
{
    if (in_array($v, $Omits))
        continue;

    http_response_code(500);
    echo __('T_ALERT_CANNOT_PARTARM', array(), FALSE);
    exit();
}

switch ($MODE)
{
    case 0:
        $MODE = "fullarm 1";
        break;
    case 1:
        $MODE = "part1arm 1";
        break;
    case 2:
        $MODE = "part2arm 1";
        break;
    case 3:
        $MODE = "part3arm 1";
        break;
}

PutArmFile($MODE);

function PutArmFile($what)
{
    passthru(sprintf(ARMFILE_SCRIPT, escapeshellarg($what)));
    exit();
}
