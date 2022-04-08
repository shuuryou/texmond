var m_EventSource = null;

$(document).ready(function()
{
    CreateEventSource();
});

$(document).on('visibilitychange', function()
{
    if (document.visibilityState != 'visible')
    {
        m_EventSource.close();
        m_EventSource = null;
        $('.status').text(T_STATUS_OFFLINE);
        return;
    }

    $('.status').text(T_STATUS_LOADING);
    CreateEventSource();
});

function CreateEventSource()
{
    m_EventSource = new EventSource('eventsource.php?auth=' + AUTHLINE);
    m_EventSource.onmessage = _EventSourceOnMessageImpl;
    m_EventSource.onerror = _EventSourceOnErrorImpl;
}

function _EventSourceOnErrorImpl(e)
{
    $('.zone').attr('data-state', 0);
    $('.zone').attr('data-fault', 0);
    $('.zone').attr('data-tamper', 0);
    $('.zone').attr('data-battery', 0);

    $('button.armbutton').prop('disabled', true);

    if (e.target.readyState == EventSource.CONNECTING)
    {
        $('.status').text(T_STATUS_LOADING);
        return;
    }

    if (e.target.readyState == EventSource.CLOSED)
    {
        $('.status').text(T_STATUS_OFFLINE);
        return;
    }
}

function _EventSourceOnMessageImpl(e)
{
    var r = null;

    try
    {
        r = JSON.parse(e.data.trim());
    }
    catch (e)
    {
        alert('Event source bad data: ' + e);
    }

    for (var key in r)
    {
        if (!r.hasOwnProperty(key)) continue;

        $('#zone_' + key).attr('data-state', r[key].State);
        $('#zone_' + key).attr('data-fault', r[key].Fault);
        $('#zone_' + key).attr('data-tamper', r[key].Tamper);
        $('#zone_' + key).attr('data-battery', r[key].Battery);

        if (key == -1)
            EnableDisableArmButtons();

        UpdateZoneStatus(key);
    }

    SortZones();
}

function UpdateZoneStatus(key)
{
    if (key == -1)
    {
        /*
        # 0: full armed
        # 1: part arm 1
        # 2: part arm 2
        # 3: part arm 3
        # 4: disarmed
        # 5: alarm has been triggered
        */

        if ($('#zone_' + key).attr('data-state') == 5)
        {
            $('#status_' + key).text(T_AREA_ALARM);
        }
        else
        {
            $('#status_' + key).text(ARMMODES[$('#zone_' + key).attr('data-state')]);
        }
    }
    else
    {
        /*
        Order of importance:
        T_ZONE_ALARM
        T_ZONE_TAMPER
        T_ZONE_FAULT
        T_ZONE_BATTERY_LOW
        T_ZONE_ACTIVE
        T_ZONE_SECURE
        */

        var status = [ ];

        if ($('#zone_' + key).attr('data-state') == 5)
            status.push(T_ZONE_ALARM);

        if ($('#zone_' + key).attr('data-tamper') == 1)
            status.push(T_ZONE_TAMPER);

        if ($('#zone_' + key).attr('data-fault') == 1)
            status.push(T_ZONE_FAULT);

        if ($('#zone_' + key).attr('data-state') == 1)
            status.push(T_ZONE_ACTIVE);

        if ($('#zone_' + key).attr('data-battery') == 1)
            status.push(T_ZONE_BATTERY_LOW);

        if (status.length == 0 && $('#zone_' + key).attr('data-state') == 0)
            status.push(T_ZONE_SECURE);

        if (status.length == 0)
            status.push(T_ZONE_UNKNOWN_STATE);

        $('#status_' + key).text(status.join(', '));
    }
}

function EnableDisableArmButtons()
{
    if ($('#zone_-1').attr('data-state') == 4)
    {
        $('button.armbutton.disarm').prop('disabled', true);
        $('button.armbutton.arm').prop('disabled', false);
        $('button.armbutton.partarm').prop('disabled', false);
    }
    else
    {
        $('button.armbutton.disarm').prop('disabled', false);
        $('button.armbutton.arm').prop('disabled', true);
        $('button.armbutton.partarm').prop('disabled', true);
    }
}

function SortZones()
{
    $('.zone').sort(function(a, b)
    {
        if (a.id == 'zone_-1')
            return -1;

        if ($(a).attr('data-state') == $(b).attr('data-state'))
        {
            if ($(a).attr('data-tamper') == $(b).attr('data-tamper'))
            {
                if ($(a).attr('data-fault') == $(b).attr('data-fault'))
                {
                    if ($(a).attr('data-battery') == $(b).attr('data-battery'))
                    {
                        return parseInt($(a).attr('data-zoneid'), 10) > parseInt($(b).attr('data-zoneid'), 10);
                    }
                    else
                    {
                        return $(a).attr('data-battery') < $(b).attr('data-battery');
                    }
                }
                else
                {
                    return $(a).attr('data-fault') < $(b).attr('data-fault');
                }
            }
            else
            {
                return $(a).attr('data-tamper') < $(b).attr('data-tamper');
            }
        }
        else
        {
            return $(a).attr('data-state') < $(b).attr('data-state');
        }
    }).appendTo('.zones');
}

function arm(mode)
{
    $('button.armbutton').prop('disabled', true);

    /*
    # 0: full arm
    # 1: part arm 1
    # 2: part arm 2
    # 3: part arm 3
    # 4: disarmed
    # 5: alarm
    */

    if (mode == 4)
    {
        $('#status_-1').text(T_STATUS_DISARMING);

        $.post('arm.php?auth=' + AUTHLINE, { MODE: mode })
            .done(ArmResponseHandler)
            .fail(ArmFailureHandler);

        return;
    }

    if ($('#zone_-1').attr('data-state') != 4)
    {
        EnableDisableArmButtons();
        ShowModal(T_ALERT_ALERADY_ARMED);
        return;
    }

    if ($('#zone_-1').attr('data-tamper') != 0 || $('#zone_-1').attr('data-fault') != 0 || $('#zone_-1').attr('data-battery') != 0)
    {
        EnableDisableArmButtons();
        ShowModal(T_ALERT_CANNOT_ARM_PANEL_ISSUES);
        return;
    }

    if (mode < 0 || mode > 3)
        throw 'Invalid mode.';

    var active_zones = [ ];

    $('.zone').each(function(idx) {
        if ($(this).attr('data-zoneid') == -1)
            return;

        if ($(this).attr('data-state') == 0 &&
            $(this).attr('data-tamper') == 0 &&
            $(this).attr('data-fault') == 0)
        {
            return;
        }

        active_zones.push(parseInt($(this).attr('data-zoneid'), 10));
    });

    if (mode == 0 && active_zones.length != 0)
    {
        EnableDisableArmButtons();
        ShowModal(T_ALERT_CANNOT_ARM);
        return;
    }

    var omits;

    switch (mode)
    {
        case 1:
            omits = PART1OMIT;
            break;
        case 2:
            omits = PART2OMIT;
            break;
        case 3:
            omits = PART3OMIT;
            break;
    }

    for (var i = 0; i < active_zones.length; i++)
    {
        if (omits.indexOf(active_zones[i]) != -1)
            continue;

        EnableDisableArmButtons();
        ShowModal(T_ALERT_CANNOT_PARTARM);
        return;
    }

    $('#status_-1').text(T_STATUS_ARMING);

    $.post('arm.php?auth=' + AUTHLINE, { MODE: mode })
        .done(ArmResponseHandler)
        .fail(ArmFailureHandler);
}

function ArmResponseHandler(data)
{
    if (data.length == 0)
        return;

    EnableDisableArmButtons();
    UpdateZoneStatus(-1);
    ShowModal(data);
}

function ArmFailureHandler(xhr, textStatus, errorThrown)
{
    EnableDisableArmButtons();
    UpdateZoneStatus(-1);
    ShowModal(xhr.responseText);
}

function ShowModal(text)
{
    $('#modalcontent').text(text);
    $('#modal').show();
    $('#content').addClass('blur');
    $('body').addClass('modal');
}

function CloseModal()
{
    $('#modal').hide();
    $('#content').removeClass('blur');
    $('body').removeClass('modal');
}
