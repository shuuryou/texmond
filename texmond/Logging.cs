using Mono.Unix.Native;
using System;
using System.Globalization;
#if UNIX
using Mono.Unix;
using System.Runtime.InteropServices;
#endif

internal static class Logging
{
    internal static bool EnableDebugLog { get; set; }

#if UNIX
    private static IntPtr DAEMON_NAME_HANDLE;
#endif

    public static void Open()
    {
#if UNIX
            const string DAEMON_NAME = "texmond";
            DAEMON_NAME_HANDLE = Marshal.StringToHGlobalAuto(DAEMON_NAME);

            Syscall.openlog(DAEMON_NAME_HANDLE, SyslogOptions.LOG_PERROR | SyslogOptions.LOG_PID,
                SyslogFacility.LOG_LOCAL5);
#endif
    }

    public static void Close()
    {
#if UNIX
            Syscall.closelog();
            Marshal.FreeHGlobal(DAEMON_NAME_HANDLE);
#endif
    }

    public static void Log(SyslogLevel level, string format, params object[] args)
    {
        Log(level, string.Format(CultureInfo.InvariantCulture, format, args));
    }

    public static void Log(SyslogLevel level, string message)
    {
        if (level == SyslogLevel.LOG_DEBUG && !EnableDebugLog)
            return;

#if UNIX
            Syscall.syslog(level, message);
#else
        Console.WriteLine("{0}: [{1}] {2}", DateTime.Now, level, message);
#endif
    }
}