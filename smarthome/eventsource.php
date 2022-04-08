<?php
define('IN_SMARTHOME', TRUE);
require_once ('init.php');

define('MAX_RUNTIME', 300);

header('Content-Type: text/event-stream');
header('Cache-Control: private, no-cache, no-store, max-age=0, no-transform');
header('Access-Control-Allow-Origin: *');
header('X-Accel-Buffering: no');

set_time_limit(MAX_RUNTIME);

$start = microtime(true);

while (ob_get_level() > 0)
    ob_end_clean();

$LastState = array();

foreach ($ZONES as $ID => $Name)
{
    $LastState[$ID] = array
    (
        'State'     => ReadInt(file_build_path(STATE_DIR, $ID, 'state')),
        'Fault'     => ReadInt(file_build_path(STATE_DIR, $ID, 'fault')),
        'Tamper'    => ReadInt(file_build_path(STATE_DIR, $ID, 'tamper')),
        'Battery'   => ReadInt(file_build_path(STATE_DIR, $ID, 'battery'))
    );
}

echo sprintf("event: message\n");
echo sprintf("data: %s\n", json_encode($LastState));
echo "\n";

flush();

while (TRUE)
{
    $time_elapsed_secs = microtime(true) - $start;

    if ((MAX_RUNTIME - $time_elapsed_secs) < 10)
        exit();

    $ret = array();

    foreach ($ZONES as $ID => $Name)
    {
        $tmp = ReadInt(file_build_path(STATE_DIR, $ID, 'state'));

        if ($tmp != $LastState[$ID]['State'])
        {
            $ret[$ID]['State'] = $tmp;
            $LastState[$ID]['State'] = $tmp;
        }

        $tmp = ReadInt(file_build_path(STATE_DIR, $ID, 'fault'));

        if ($tmp != $LastState[$ID]['Fault'])
        {
            $ret[$ID]['Fault'] = $tmp;
            $LastState[$ID]['Fault'] = $tmp;
        }

        $tmp = ReadInt(file_build_path(STATE_DIR, $ID, 'tamper'));

        if ($tmp != $LastState[$ID]['Tamper'])
        {
            $ret[$ID]['Tamper'] = $tmp;
            $LastState[$ID]['Tamper'] = $tmp;
        }

        $tmp = ReadInt(file_build_path(STATE_DIR, $ID, 'battery'));

        if ($tmp != $LastState[$ID]['Battery'])
        {
            $ret[$ID]['Battery'] = $tmp;
            $LastState[$ID]['Battery'] = $tmp;
        }
    }

    if (!empty($ret))
    {
        echo sprintf("event: message\n");
        echo sprintf("data: %s\n", json_encode($ret));
        echo "\n";
        flush();
    }
    else
    {
        // This is to detect aborted fcgi connections
        echo ":\n\n";
        flush();
        sleep(3);
    }
}
