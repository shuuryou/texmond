<?php define('IN_SMARTHOME', TRUE); require_once('init.php'); ?>
<!doctype html>
<html>
    <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title><?php echo __('T_PAGE_TITLE'); ?></title>
        <link rel="stylesheet" href="support/pure.css">
        <link rel="stylesheet" href="support/style.css?<?php echo filemtime('support/style.css'); ?>">
        <script src="support/jquery.js" defer></script>
        <script src="support/script.js?<?php echo filemtime('support/script.js'); ?>" defer></script>
        <link rel="shortcut icon" href="support/favicon.ico">
        <link rel="apple-touch-icon" href="support/apple-touch-icon.png">
        <meta name="apple-mobile-web-app-capable" content="yes">
        <meta name="apple-mobile-web-app-title" content="<?php echo __('T_PAGE_TITLE'); ?>">
    </head>
    <body>
        <noscript><h1>This page requires JavaScript.</h1></noscript>
        <div id="content">
            <div class="pure-g">
<?php
                foreach ($ZONES as $ID => $Name)
                {
                    if ($ID == -1)
                    {
?>
                        <div class="zones pure-u-1-1">
                            <div id="zone_<?php echo Fix($ID); ?>" class="pure-u-1-1 zone" data-zoneid="<?php echo Fix($ID); ?>" data-state="0" data-fault="0" data-tamper="0" data-battery="0">
                                <div class="zone_inner">
                                    <div class="title" id="title_<?php echo Fix($ID); ?>"><?php echo Fix($Name); ?></div>
                                    <div class="status" id="status_<?php echo Fix($ID); ?>"><?php echo __('T_STATUS_LOADING'); ?></div>
                                    <div class="actions">
<?php
                                        foreach ($ARMMODES as $ArmID => $ButtonText)
                                        {
                                            if ($ArmID == 4)
                                                $class = 'disarm';
                                            elseif ($ArmID == 0)
                                                $class = 'arm';
                                            else
                                                $class = 'partarm';
?>
                                            <button disabled onclick="arm(<?php echo Fix($ArmID); ?>);" class="armbutton <?php echo Fix($class); ?> pure-button"><?php echo Fix($ButtonText); ?></button>
<?php
                                        }
?>
                                    </div>
                                </div>
                            </div>
                        </div>
<?php
                    }
                    else
                    {
?>
                        <div id="zone_<?php echo Fix($ID); ?>" class="pure-u-1-1 pure-u-sm-1-2 pure-u-md-1-3 pure-u-lg-1-4 pure-u-xl-1-5 zone" data-zoneid="<?php echo Fix($ID); ?>" data-state="0" data-fault="0" data-tamper="0" data-battery="0">
                            <div class="zone_inner">
                                <div class="title" id="title_<?php echo Fix($ID); ?>"><?php echo Fix($Name); ?></div>
                                <div class="status" id="status_<?php echo Fix($ID); ?>"><?php echo __('T_STATUS_LOADING'); ?></div>
                            </div>
                        </div>
<?php
                    }
                }
?>
            </div>
        </div>
        <script>
            var T_STATUS_ARMING = <?php echo json_encode(T_STATUS_ARMING); ?>;
            var T_STATUS_DISARMING = <?php echo json_encode(T_STATUS_DISARMING); ?>;
            var T_STATUS_LOADING = <?php echo json_encode(T_STATUS_LOADING); ?>;
            var T_STATUS_OFFLINE = <?php echo json_encode(T_STATUS_OFFLINE); ?>;
            var T_ZONE_BATTERY_LOW = <?php echo json_encode(T_ZONE_BATTERY_LOW); ?>;
            var T_ZONE_FAULT = <?php echo json_encode(T_ZONE_FAULT); ?>;
            var T_ZONE_TAMPER = <?php echo json_encode(T_ZONE_TAMPER); ?>;
            var T_ZONE_ACTIVE = <?php echo json_encode(T_ZONE_ACTIVE); ?>;
            var T_ZONE_SECURE = <?php echo json_encode(T_ZONE_SECURE); ?>;
            var T_ZONE_ALARM = <?php echo json_encode(T_ZONE_ALARM); ?>;
            var T_ZONE_UNKNOWN_STATE = <?php echo json_encode(T_ZONE_UNKNOWN_STATE); ?>;
            var T_AREA_ALARM = <?php echo json_encode(T_AREA_ALARM); ?>;
            var T_ALERT_CANNOT_ARM = <?php echo json_encode(T_ALERT_CANNOT_ARM); ?>;
            var T_ALERT_CANNOT_PARTARM = <?php echo json_encode(T_ALERT_CANNOT_PARTARM); ?>;
            var T_ALERT_ALERADY_ARMED = <?php echo json_encode(T_ALERT_ALERADY_ARMED); ?>;
            var T_ALERT_CANNOT_ARM_PANEL_ISSUES = <?php echo json_encode(T_ALERT_CANNOT_ARM_PANEL_ISSUES); ?>;

            var AUTHLINE = <?php echo json_encode(AUTHLINE); ?>;
            var ARMMODES = <?php echo json_encode($ARMMODES); ?>;
            var PART1OMIT = <?php echo json_encode($PART1OMIT); ?>;
            var PART2OMIT = <?php echo json_encode($PART2OMIT); ?>;
            var PART3OMIT = <?php echo json_encode($PART3OMIT); ?>;
        </script>
        <div id="modal">
            <div>
                <div>
                    <p id="modalcontent"></p>
                    <button class="pure-button pure-button-primary" onclick="CloseModal()">Close</button>
                </div>
            </div>
        </div>
    </body>
</html>
