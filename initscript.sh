#!/bin/sh
### BEGIN INIT INFO
# Provides:          texmond
# Required-Start:    $remote_fs $network
# Required-Stop:     $remote_fs
# Default-Start:     2 3 4 5
# Default-Stop:      0 1 6
# Short-Description: Logging of Texecom panel events
# Description:       Logs events from a Texecom panel connected via serial port.
### END INIT INFO

PATH=/sbin:/bin:/usr/bin

NAME=texmond
DESC="logging of Texecom panel events"
PID_FILE=/var/run/texmond.pid
DAEMON=/opt/texmond/texmond.exe
MONO=/usr/bin/mono-sgen

test -x $MONO || exit 0

. /lib/lsb/init-functions

set -e

case "$1" in
  start)
    log_daemon_msg "Starting $DESC" "$NAME"
    start-stop-daemon --start -m --pidfile $PID_FILE --exec "$MONO" -d $(dirname "$DAEMON") --background --quiet -- $DAEMON
    log_end_msg $?
    ;;
  stop)
    log_daemon_msg "Stopping $DESC" "$NAME"
    start-stop-daemon --stop --pidfile $PID_FILE --exec "$MONO"
    log_end_msg $?
    rm -f $PID_FILE
    ;;
  restart)
    $0 stop
        sleep 2
    $0 start
    ;;
  status)
    status_of_proc "$DAEMON" "$NAME" && exit 0 || exit $?
    ;;
  trigger)
    start-stop-daemon --stop --signal USR1 --pidfile $PID_FILE --exec $MONO
    ;;
  *)
    echo "Usage: $0 {start|stop|restart|status|trigger}" >&2
    exit 1
    ;;
esac

exit 0
