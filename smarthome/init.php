<?php
if (!defined('IN_SMARTHOME')) die();

if (! empty($_SERVER['REQUEST_METHOD']) && strtoupper($_SERVER['REQUEST_METHOD']) == 'HEAD') exit();

error_reporting(E_ALL);

set_error_handler('ErrorCallback');
set_exception_handler('ExceptionCallback');

assert_options(ASSERT_ACTIVE, 1);
assert_options(ASSERT_WARNING, 0);
assert_options(ASSERT_BAIL, 0);
assert_options(ASSERT_CALLBACK, 'AssertCallback');

require_once('config.php');

session_start();

if (empty($_SESSION['AUTH']) || $_SESSION['AUTH'] !== TRUE)
{
    if (empty($_GET['auth']) || $_GET['auth'] != AUTHLINE)
    {
        session_destroy();

        http_response_code(403);
        echo 'Access denied.';
        exit();
    }

    $_SESSION['AUTH'] = TRUE;

    session_write_close();
}

session_abort();


function AssertCallback($File, $Line)
{
    if (ob_get_level() != 0) ob_clean();

    http_response_code(500);
    vprintf('*** STOP: Assertion failed in "%s" on line %d.', array(
        $File,
        $Line
    ));

    exit(1);
}

function ErrorCallback($Severity, $Message, $File, $Line)
{
    if (error_reporting() == 0) return;
    throw new ErrorException($Message, 0, $Severity, $File, $Line);
}

function ExceptionCallback(Throwable $Exception)
{
    if (ob_get_level() != 0) ob_clean();

    http_response_code(500);
    vprintf('*** STOP: Unhandled exception of type "%s" (%s) on line %d in file "%s". Trace: %s', array(
        get_class($Exception),
        $Exception->getMessage(),
        $Exception->getLine(),
        $Exception->getFile(),
        $Exception->getTraceAsString()
    ));

    exit(1);
}

function ReadInt($file)
{
    return intval(trim(file_get_contents($file)));
}

function file_build_path(...$segments)
{
    return join(DIRECTORY_SEPARATOR, $segments);
}

function __($Constant, array $Arguments = array(), $Format = TRUE)
{
    if (! defined($Constant)) throw new Exception(Fix(sprintf('Language definition "%s" has not been defined.', $Constant)));

    if ($Format)
        return nl2br(Fix(vsprintf(constant($Constant), $Arguments)));
    else
        return vsprintf(constant($Constant), $Arguments);
}

function UnFix($String)
{
    return html_entity_decode($String, ENT_QUOTES, 'UTF-8');
}

function Fix($String)
{
    return htmlentities($String, ENT_COMPAT, 'UTF-8');
}
