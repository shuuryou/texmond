using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

#if !UNIX && !DEBUG
#error You are not on UNIX and compiling a RELEASE build. This is a Linux daemon and must be compiled with Mono.
#endif

namespace texmond
{
    public static class Program
    {
        private const int MAX_SECONDS_BETWEEN_IDLE_COMMAND = 50;

        private static Queue<UnsolicitedMessage> UNSOLICITED_MESSAGE_QUEUE = new Queue<UnsolicitedMessage>();

#if !UNIX
        private static ManualResetEvent WAITHANDLE_TO_DEBUG_THIS_ON_WINDOWS = new ManualResetEvent(false);
#endif

        private static string SETTING_COM_PORT, SETTING_UDL_CODE;
        private static string SETTING_EVENTSCRIPT, SETTING_ARMFILE;
        private static int POLL_PANEL_POWER_MINS;
        private static bool SETTING_SNYC_TIME, SETTING_DEBUG_LOG, SETTING_LOG_EVENTSCRIPT;

        public static int Main(string[] args)
        {
            Logging.Open();

            Logging.Log(SyslogLevel.LOG_INFO, "Starting texmond.");
            
            // Hi Texecom, please don't sue me. :-(
            Logging.Log(SyslogLevel.LOG_NOTICE, "THIS SOFTWARE IS NOT DEVELOPED BY TEXECOM, LTD.");
            Logging.Log(SyslogLevel.LOG_NOTICE, "THIS SOFTWARE IS NOT ENDORSED  BY TEXECOM, LTD.");
            Logging.Log(SyslogLevel.LOG_NOTICE, "THIS SOFTWARE IS NOT SUPPORTED BY TEXECOM, LTD.");
            Logging.Log(SyslogLevel.LOG_NOTICE, "THE SOFTWARE IS PROVIDED  AS IS, WITHOUT WARRANTY OF ANY KIND,");
            Logging.Log(SyslogLevel.LOG_NOTICE, "EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES");
            Logging.Log(SyslogLevel.LOG_NOTICE, "OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NON-");
            Logging.Log(SyslogLevel.LOG_NOTICE, "INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS");
            Logging.Log(SyslogLevel.LOG_NOTICE, "BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN");
            Logging.Log(SyslogLevel.LOG_NOTICE, "AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF");
            Logging.Log(SyslogLevel.LOG_NOTICE, "OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS");
            Logging.Log(SyslogLevel.LOG_NOTICE, "IN THE SOFTWARE.");

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Logging.Log(SyslogLevel.LOG_INFO, "Registered unhandled exception handler.");

            using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("texmond.BuildDate.txt"), Encoding.ASCII))
                Logging.Log(SyslogLevel.LOG_INFO, "This version was built {0}.", reader.ReadToEnd().Trim());

            Logging.Log(SyslogLevel.LOG_INFO, "Loading settings.");

            try

            {
#if UNIX
                const string config_ini = "/etc/texmond.ini";
#else
                string config_ini = string.Format(CultureInfo.InvariantCulture, "{0}{1}settings.ini",
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Path.DirectorySeparatorChar);
#endif

                FuckINI settings = new FuckINI(config_ini);
                SETTING_COM_PORT = settings.Get("texmond", "port");
                SETTING_UDL_CODE = settings.Get("texmond", "udlcode");
                SETTING_EVENTSCRIPT = settings.Get("texmond", "eventscript");
                SETTING_ARMFILE = settings.Get("texmond", "armfile");

                if (string.IsNullOrEmpty(SETTING_COM_PORT))
                    throw new ArgumentException("Missing COM port setting.");

                foreach (char c in SETTING_UDL_CODE)
                    if (c < '0' || c > '9') throw new ArgumentException("UDL code is not a number.");

                if (SETTING_UDL_CODE.Length < 4 || SETTING_UDL_CODE.Length > 8)
                    throw new ArgumentOutOfRangeException("UDL code length is invalid.");

                if (string.IsNullOrWhiteSpace(SETTING_EVENTSCRIPT))
                    SETTING_EVENTSCRIPT = null;
                else
                    Logging.Log(SyslogLevel.LOG_INFO, "Event script execution is enabled.");

                if (string.IsNullOrWhiteSpace(SETTING_ARMFILE))
                    SETTING_ARMFILE = null;
                else
                    Logging.Log(SyslogLevel.LOG_INFO, "Arm file processing is enabled.");


                if (SETTING_EVENTSCRIPT != null)
                {
#if UNIX
                    Mono.Unix.UnixFileInfo info = new Mono.Unix.UnixFileInfo(SETTING_EVENTSCRIPT);
#else
                    FileInfo info = new FileInfo(SETTING_EVENTSCRIPT);
#endif

                    if (!info.Exists)
                        throw new FileNotFoundException("The specified event script file does not exist.", SETTING_EVENTSCRIPT);

#if UNIX
                    if (!info.CanAccess(AccessModes.R_OK & AccessModes.X_OK))
                        throw new ArgumentException("Event script not readable or executable.");
#endif
                }

                SETTING_SNYC_TIME = settings.Get("texmond", "synctime") == "1";
                SETTING_DEBUG_LOG = settings.Get("texmond", "debuglog") == "1";
                SETTING_LOG_EVENTSCRIPT = settings.Get("texmond", "logeventscript") == "1";

                {
                    string tmp = settings.Get("texmond", "pollpanelpower");

                    if (string.IsNullOrWhiteSpace(tmp))
                        POLL_PANEL_POWER_MINS = -1;

                    if (!int.TryParse(tmp, NumberStyles.None, CultureInfo.InvariantCulture, out POLL_PANEL_POWER_MINS))
                        throw new ArgumentException("PollPanelPower is not a number.");

                    if (POLL_PANEL_POWER_MINS < 1)
                        throw new ArgumentOutOfRangeException("PollPanelPower must be greater than 1.");
                }

                if (POLL_PANEL_POWER_MINS != -1)
                    Logging.Log(SyslogLevel.LOG_INFO, "Panel power information will be polled every {0:n0} minute(s).", POLL_PANEL_POWER_MINS);
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_CRIT, "Unable to load settings: {0}", e);
                return 1;
            }

            Logging.EnableDebugLog = SETTING_DEBUG_LOG;

            PanelSerialController controller = new PanelSerialController(SETTING_COM_PORT);
            controller.MessageReceived += Panel_MessageReceived;

            try
            {
                controller.Open();
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to connect to panel: {0}", e);
                return 1;
            }

            if (!LogonPanelAndSubscribeEvents(controller, SETTING_UDL_CODE))
                return 1;

            if (!PrintProtocolVersion(controller))
                return 1;

#if UNIX
            Mono.Unix.UnixSignal[] signals = new Mono.Unix.UnixSignal[] {
                new Mono.Unix.UnixSignal(Signum.SIGINT),
                new Mono.Unix.UnixSignal(Signum.SIGTERM),
                new Mono.Unix.UnixSignal(Signum.SIGUSR1)
            };
#endif

            Logging.Log(SyslogLevel.LOG_INFO, "Starting event loop.");

            DateTime last_power_poll = DateTime.MinValue;

            do
            {
                Logging.Log(SyslogLevel.LOG_DEBUG, "New iteration of event loop.");
                
                int sleep_dur = Math.Max(0, MAX_SECONDS_BETWEEN_IDLE_COMMAND -
                    (int)(DateTime.Now - controller.LastMessageSent).TotalSeconds);

                Logging.Log(SyslogLevel.LOG_DEBUG, "Sleep duration is {0}s because last message sent was on {1:F}.",
                    sleep_dur, controller.LastMessageSent);

                sleep_dur *= 1000;

#if UNIX
                int id = Mono.Unix.UnixSignal.WaitAny(signals, sleep_dur);

                if (id >= 0 && id < signals.Length)
                    if (signals[id].IsSet)
                        if (signals[id].Signum == Signum.SIGUSR1)
                        {
                            Logging.Log(SyslogLevel.LOG_DEBUG, "Woke up on SIGUSR1.");
                            signals[id].Reset();
                        }
                        else
                        {
                            Logging.Log(SyslogLevel.LOG_DEBUG, "Woke up on SIGINT or SIGTERM. Bailing out of idle loop.");
                            break;
                        }
#else
                if (WAITHANDLE_TO_DEBUG_THIS_ON_WINDOWS.WaitOne(sleep_dur))
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Woke up on WAITHANDLE_TO_DEBUG_THIS_ON_WINDOWS.");
                    WAITHANDLE_TO_DEBUG_THIS_ON_WINDOWS.Reset();
                }
#endif

                if (UNSOLICITED_MESSAGE_QUEUE.Count != 0)
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Received unsolicited messages for processing.");

                    while (UNSOLICITED_MESSAGE_QUEUE.Count > 0)
                        InterpretUnsolicitedMessage(UNSOLICITED_MESSAGE_QUEUE.Dequeue(), controller);
                }
                
                bool problem = false;

                if (SETTING_ARMFILE != null)
                {
                    if (!CheckAndProcessArmFile(controller))
                        problem = true;
                }

                if (POLL_PANEL_POWER_MINS != -1 && DateTime.Now.Subtract(last_power_poll).TotalMinutes >= POLL_PANEL_POWER_MINS)
                {
                    if (!PollPanelPower(controller))
                        problem = true;
                    else
                        last_power_poll = DateTime.Now;
                }
                
                int secs_last_message_sent = (int)((DateTime.Now - controller.LastMessageSent).TotalSeconds);

                if (problem || secs_last_message_sent >= MAX_SECONDS_BETWEEN_IDLE_COMMAND)
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Anti-Idle ({0:n0}s; {1:n0}s; Problem? {2}). Check panel date/time.",
                        secs_last_message_sent, MAX_SECONDS_BETWEEN_IDLE_COMMAND, problem);

                    if (!TryUpdateDateTime(controller, SETTING_SNYC_TIME))
                        if (!LogonPanelAndSubscribeEvents(controller, SETTING_UDL_CODE))
                        {
                            Logging.Log(SyslogLevel.LOG_CRIT, "Something is wrong with the panel. Exiting event loop.");
                            DoEventScript("ERROR", "PANEL");
                            break;
                        }

                    Logging.Log(SyslogLevel.LOG_DEBUG, "Force GC collection.");
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                }
            } while (true);

            try
            {
                controller.MessageReceived -= Panel_MessageReceived;
                controller.Dispose();
                controller = null;
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to clean up panel connection: {0}", e);
            }

            Logging.Log(SyslogLevel.LOG_INFO, "Exited event loop. Terminating now.");

            Logging.Close();

#if !UNIX
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
#endif

            return 0;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logging.Log(SyslogLevel.LOG_ERR, "Crashed because of an unhandled exception: {0}", e.ExceptionObject);
            DoEventScript("ERROR", "CRASH");
        }

        private static bool PollPanelPower(PanelSerialController controller)
        {
            if (controller == null) throw new ArgumentNullException("controller");

            try
            {
                PanelMessage rmsg =
                    controller.SendMessageSync(PanelSimpleProtocol.GetSystemPower());

                GetSystemPowerResponse response = PanelResponsePayload.Parse(rmsg.Payload)
                    as GetSystemPowerResponse;

                if (!response.Success)
                {
                    Logging.Log(SyslogLevel.LOG_CRIT, "Panel refused to return its current power status.");
                    return false;
                }

                Logging.Log(SyslogLevel.LOG_INFO, "System voltage: {0:n2}V / Battery voltage: {1:n2}V / System current: {2:n2}mA / Battery charging current: {3:n2}mA / Reference voltage: {4:n2}V",
                    response.SystemVoltage, response.BatteryVoltage, response.SystemCurrent, response.BatteryChargingCurrent, response.ReferenceVoltage);

                //            1        2                       3                        4                       5                                6
                DoEventScript("POWER", response.SystemVoltage, response.BatteryVoltage, response.SystemCurrent, response.BatteryChargingCurrent, response.ReferenceVoltage);
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to get power status: {0}", e);
                return false;
            }

            return true;
        }

        private static bool CheckAndProcessArmFile(PanelSerialController controller)
        {
            if (controller == null) throw new ArgumentNullException("controller");

            if (string.IsNullOrEmpty(SETTING_ARMFILE))
                throw new InvalidOperationException("Arm file setting is missing.");

            if (!File.Exists(SETTING_ARMFILE))
            {
                Logging.Log(SyslogLevel.LOG_DEBUG, "Arm file does not exist. Nothing to do.");
                return true;
            }

            bool success = false;

            string[] content;

            try
            {
                string tmp = File.ReadAllText(SETTING_ARMFILE, Encoding.ASCII).Trim().ToUpperInvariant();
                content = tmp.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to get contents of arm file: {0}", e);
                goto done;
            }

            if (content.Length < 2)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Content of arm file does not include a command and at least one area number.");
                goto done;
            }

            List<int> areas = new List<int>();

            for (int i = 1; i < content.Length; i++)
            {
                if (!int.TryParse(content[i], NumberStyles.None, CultureInfo.InvariantCulture, out int area))
                {
                    Logging.Log(SyslogLevel.LOG_ERR, "Arm file command (\"{0}\") was not understood.", content[0]);
                    goto done;
                }

                if (area < 1)
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Area out of range: {0}", area);
                    goto done;
                }

                Logging.Log(SyslogLevel.LOG_DEBUG, "Arm file request covers this area number: {0}", area);

                areas.Add(area);
            }

            PanelMessage msg = null;

            switch (content[0])
            {
                case "PART1ARM":
                    Logging.Log(SyslogLevel.LOG_INFO, "Received a PART ARM 1 request via arm file.");
                    msg = PanelSimpleProtocol.ArmAreas(TexecomElitePanelType.Elite12_64, PanelAreaArmType.PartArm1, areas.ToArray());
                    break;
                case "PART2ARM":
                    Logging.Log(SyslogLevel.LOG_INFO, "Received a PART ARM 2 request via arm file.");
                    msg = PanelSimpleProtocol.ArmAreas(TexecomElitePanelType.Elite12_64, PanelAreaArmType.PartArm2, areas.ToArray());
                    break;
                case "PART3ARM":
                    Logging.Log(SyslogLevel.LOG_INFO, "Received a PART ARM 3 request via arm file.");
                    msg = PanelSimpleProtocol.ArmAreas(TexecomElitePanelType.Elite12_64, PanelAreaArmType.PartArm3, areas.ToArray());
                    break;
                case "FULLARM":
                    Logging.Log(SyslogLevel.LOG_INFO, "Received a FULL ARM request via arm file.");
                    msg = PanelSimpleProtocol.ArmAreas(TexecomElitePanelType.Elite12_64, PanelAreaArmType.FullArm, areas.ToArray());
                    break;
                case "DISARM":
                    Logging.Log(SyslogLevel.LOG_INFO, "Received a DISARM request via arm file.");
                    msg = PanelSimpleProtocol.DisarmAreas(TexecomElitePanelType.Elite12_64, areas.ToArray());
                    break;
                default:
                    Logging.Log(SyslogLevel.LOG_ERR, "Arm file command (\"{0}\") was not understood.", content[0]);
                    goto done;
            }

            Logging.Log(SyslogLevel.LOG_DEBUG, "The arm file request is now being submitted to the panel.");

            try
            {
                PanelMessage rmsg = controller.SendMessageSync(msg);

                PayloadResponse response = PanelResponsePayload.Parse(rmsg.Payload);

                success = response.Success;

                if (!success)
                {
                    Logging.Log(SyslogLevel.LOG_CRIT, "The panel refused to process the request.");
                    goto done;
                }
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to send the arm file request: {0}", e);
                goto done;
            }

            Logging.Log(SyslogLevel.LOG_DEBUG, "The arm file request was submitted to the panel successfully.");

        done:
            Logging.Log(SyslogLevel.LOG_DEBUG, "Deleting the arm file after processing.");

            try
            {
                File.Delete(SETTING_ARMFILE);
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to delete the arm file after processing: {0}", e);
                return false;
            }

            Logging.Log(SyslogLevel.LOG_DEBUG, "The arm file was deleted successfully.");

            return success;
        }

        private static bool TryUpdateDateTime(PanelSerialController controller, bool actuallyUpdateIt)
        {
            if (controller == null) throw new ArgumentNullException("controller");

            DateTime tmp;

            try
            {
                PanelMessage rmsg =
                    controller.SendMessageSync(PanelSimpleProtocol.GetDateTime());

                GetDateTimeResponse response = PanelResponsePayload.Parse(rmsg.Payload)
                    as GetDateTimeResponse;

                if (!response.Success)
                {
                    Logging.Log(SyslogLevel.LOG_CRIT, "Panel refused to return its current date and time.");
                    return false;
                }

                if (!actuallyUpdateIt) return true;

                tmp = response.PanelDateTime;
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to get panel date and time: {0}", e);
                return false;
            }

            if (Math.Abs(DateTime.Now.Subtract(tmp).TotalSeconds) < 5)
            {
                Logging.Log(SyslogLevel.LOG_DEBUG, "No need to adjust panel date/time. Panel: {0:F} - Us: {1:F}. That's still OK.", tmp, DateTime.Now);
                return true;
            }

            try
            {
                PanelMessage rmsg =
                    controller.SendMessageSync(PanelSimpleProtocol.SetDateTime(DateTime.Now));

                SetDateTimeResponse response = PanelResponsePayload.Parse(rmsg.Payload)
                    as SetDateTimeResponse;

                if (!response.Success)
                {
                    Logging.Log(SyslogLevel.LOG_ERR, "Panel refused to set its date and time.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to set panel date and time: {0}", e);
                return false;
            }

            Logging.Log(SyslogLevel.LOG_NOTICE, "Adjusted incorrect date/time on panel. Panel: {0:F} - Us: {1:F}.", tmp, DateTime.Now);
            return true;
        }

        private static bool LogonPanelAndSubscribeEvents(PanelSerialController controller, string udlcode)
        {
            if (controller == null) throw new ArgumentNullException("controller");

            try
            {
                PanelMessage responsemsg =
                    controller.SendMessageSync(PanelSimpleProtocol.LogOn(udlcode));

                PayloadResponse responsepayload =
                    PanelResponsePayload.Parse(responsemsg.Payload);

                if (!responsepayload.Success)
                {
                    Logging.Log(SyslogLevel.LOG_ERR, "Logging on to panel failed. UDL code might be incorrect.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to log on to panel: {0}", e);
                return false;
            }

            try
            {
                PanelMessage responsemsg =
                    controller.SendMessageSync(PanelSimpleProtocol.SetEventMessages(
                    PanelEventMessages.AreaEventMessages | PanelEventMessages.Debug |
                    PanelEventMessages.LogEvents | PanelEventMessages.OutputEventMessages |
                    PanelEventMessages.UserEventMessages | PanelEventMessages.ZoneEventMessages));

                PayloadResponse responsepayload =
                    PanelResponsePayload.Parse(responsemsg.Payload);

                if (!responsepayload.Success)
                {
                    Logging.Log(SyslogLevel.LOG_ERR, "Panel refused to set unsolicited event messages.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to request unsolicited event messages from panel: {0}", e);
                return false;
            }

            return true;
        }

        private static bool PrintProtocolVersion(PanelSerialController controller)
        {
            if (controller == null) throw new ArgumentNullException("controller");

            try
            {
                PanelMessage responsemsg =
                    controller.SendMessageSync(PanelSimpleProtocol.GetProtocolVersion());

                PayloadResponse responsepayload =
                    PanelResponsePayload.Parse(responsemsg.Payload);

                if (!responsepayload.Success)
                {
                    Logging.Log(SyslogLevel.LOG_ERR, "Getting Texecom Connect protocol version from panel failed.");
                    return false;
                }

                if (!(responsepayload is GetProtocolVersionResponse response)) // WTF?!
                    throw new ArgumentNullException("response");

                Logging.Log(SyslogLevel.LOG_INFO, "This panel supports Texecom Connect protocol version {0}.", response.ProtocolVersion);
            }
            catch (Exception e)
            {
                Logging.Log(SyslogLevel.LOG_ERR, "Unable to get Texecom Connect protocol version from panel: {0}", e);
                return false;
            }

            return true;
        }

        private static void Panel_MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            switch (args.Response.Type)
            {
                default:
                    Logging.Log(SyslogLevel.LOG_ERR, "Unsupported message type in MessageReceived handler: {0}", args.Response.Type);
                    return;
                case PanelMessageType.Unsolicited:
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Received unsolicited message 0x{0:x2}.", args.Response.SequenceNumber);

                    UnsolicitedMessage msg;

                    try
                    {
                        msg = PanelUnsolicitedPayload.Parse(args.Response.Payload);
                    }
                    catch (Exception e)
                    {
                        Logging.Log(SyslogLevel.LOG_ERR, "Exception while parsing unsolicited message payload from panel: {0}", e);
                        return;
                    }

                    UNSOLICITED_MESSAGE_QUEUE.Enqueue(msg);

#if UNIX
                    int currentPID = Syscall.getpid();
                    Syscall.kill(currentPID, Signum.SIGUSR1);
#else
                    WAITHANDLE_TO_DEBUG_THIS_ON_WINDOWS.Set();
#endif

                    return;
            }
        }

        private static Dictionary<int, string> s_AreaTextCache = new Dictionary<int, string>();
        private static Dictionary<int, string> s_ZoneTextCache = new Dictionary<int, string>();
        private static Dictionary<int, string> s_UserNameCache = new Dictionary<int, string>();

        private static void InterpretUnsolicitedMessage(UnsolicitedMessage msg, PanelSerialController controller)
        {
            if (msg == null) throw new ArgumentNullException("msg");
            if (controller == null) throw new ArgumentNullException("controller");

            Logging.Log(SyslogLevel.LOG_DEBUG, "Now interpreting unsolicited message of type \"{0}\"", msg.GetType().Name);

            #region LogEventMessage
            if (msg is LogEventMessage lem)
            {
                PanelRawLogEvent log = lem.Entry;

                switch (log.EventKind)
                {
                    case PanelEventLogKind.NonZoneEvent:
                        List<string> affectedAreas = new List<string>();

                        for (int i = 1; i <= 4; i++)
                        {
                            if (log.IsAreaAffected(i))
                                continue;

                            string areaText = null;

                            if (s_AreaTextCache.ContainsKey(i))
                            {
                                Logging.Log(SyslogLevel.LOG_DEBUG, "Cache hit for area {0} text.", i);

                                areaText = s_AreaTextCache[i];
                            }
                            else
                            {
                                Logging.Log(SyslogLevel.LOG_DEBUG, "Cache miss for area {0} text.", i);

                                PanelMessage resp =
                                    controller.SendMessageSync(PanelSimpleProtocol.GetAreaText(i, 1));

                                if (resp == null)
                                    goto fail;

                                GetAreaTextResponse atr;

                                try
                                {
                                    atr = PanelResponsePayload.Parse(resp.Payload) as GetAreaTextResponse;
                                }
                                catch (Exception e)
                                {
                                    Logging.Log(SyslogLevel.LOG_ERR, "Exception while parsing area text response from panel: {0}", e);
                                    goto fail;
                                }

                                if (!atr.Success)
                                    goto fail;

                                areaText = string.Format("{0:n0} (\"{1}\")", i, atr.AreaText[0]);
                                s_AreaTextCache.Add(i, areaText);

                            fail:
                                if (string.IsNullOrEmpty(areaText)) areaText = "<ERROR>";
                            }

                            affectedAreas.Add(areaText);
                        }

                        Logging.Log(SyslogLevel.LOG_INFO, "[LOG EVENT] On {0:F}: {1} {2} in area(s) {3}. Parameter={4} Delayed? {5} Communicated? {6}",
                            log.Date, log.EventType, log.GroupType, string.Join(", ", affectedAreas), log.Parameter, log.CommunicationIsDelayed, log.EventIsCommunicated);

                        //            1      2       3         4              5              6
                        DoEventScript("LOG", "AREA", log.Date, log.EventType, log.GroupType, string.Join(", ", affectedAreas),
                            //  7          8                           9
                            log.Parameter, log.CommunicationIsDelayed, log.EventIsCommunicated);
                        break;
                    case PanelEventLogKind.ZoneEvent:
                        string zoneText = null;

                        if (s_ZoneTextCache.ContainsKey(log.Parameter))
                        {
                            Logging.Log(SyslogLevel.LOG_DEBUG, "Cache hit for zone {0} text.", log.Parameter);

                            zoneText = s_ZoneTextCache[log.Parameter];
                        }
                        else
                        {
                            Logging.Log(SyslogLevel.LOG_DEBUG, "Cache miss for zone {0} text.", log.Parameter);

                            PanelMessage resp =
                                controller.SendMessageSync(PanelSimpleProtocol.GetZoneText(TexecomElitePanelType.Elite12_64, log.Parameter, 1));

                            if (resp == null)
                                goto fail;

                            GetZoneTextResponse ztr;

                            try
                            {
                                ztr = PanelResponsePayload.Parse(resp.Payload) as GetZoneTextResponse;
                            }
                            catch (Exception e)
                            {
                                Logging.Log(SyslogLevel.LOG_ERR, "Exception while parsing zone text response from panel: {0}", e);
                                goto fail;
                            }

                            if (!ztr.Success)
                                goto fail;

                            zoneText = ztr.ZoneText[0];
                            s_ZoneTextCache.Add(log.Parameter, zoneText);

                        fail:
                            if (string.IsNullOrEmpty(zoneText)) zoneText = "<ERROR>";
                        }

                        Logging.Log(SyslogLevel.LOG_INFO, "[LOG EVENT] On {0:F}: {1} {2} in zone {3} (\"{4}\"). Delayed? {5} Communicated? {6}",
                            log.Date, log.EventType, log.GroupType, log.Parameter, zoneText, log.CommunicationIsDelayed, log.EventIsCommunicated);

                        //            1      2       3         4              5              6              7         8                           9
                        DoEventScript("LOG", "ZONE", log.Date, log.EventType, log.GroupType, log.Parameter, zoneText, log.CommunicationIsDelayed, log.EventIsCommunicated);
                        break;
                }

                return;
            }
            #endregion

            #region UserEventMessage
            if (msg is UserEventMessage uem)
            {
                string userName = null, logonType = null;

                logonType = uem.UserState == 0 ? "USER CODE" : "TAG";

                if (uem.UserNumber == 0)
                {
                    userName = "<ENGINEER>";
                    goto fail;
                }

                if (s_UserNameCache.ContainsKey(uem.UserNumber))
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Cache hit for user {0} name.", uem.UserNumber);

                    userName = s_UserNameCache[uem.UserNumber];
                }
                else
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Cache miss for user {0} name.", uem.UserNumber);

                    PanelMessage resp =
                        controller.SendMessageSync(PanelSimpleProtocol.GetUser(TexecomElitePanelType.Elite12_64, uem.UserNumber));

                    if (resp == null)
                        goto fail;

                    GetUserResponse gur;

                    try
                    {
                        gur = PanelResponsePayload.Parse(resp.Payload) as GetUserResponse;
                    }
                    catch (Exception e)
                    {
                        Logging.Log(SyslogLevel.LOG_ERR, "Exception while parsing get user response from panel: {0}", e);
                        goto fail;
                    }

                    if (!gur.Success)
                        goto fail;

                    userName = gur.Username;
                    s_UserNameCache.Add(uem.UserNumber, userName);
                }

            fail:

                if (string.IsNullOrEmpty(userName)) userName = "<ERROR>";
                if (string.IsNullOrEmpty(logonType)) logonType = "<ERROR>";

                Logging.Log(SyslogLevel.LOG_NOTICE, "[USER EVENT] User {0:n0} (\"{1}\") logged on using {2}.", uem.UserNumber, userName, logonType);

                //            1       2               3         4
                DoEventScript("USER", uem.UserNumber, userName, logonType);
                return;
            }
            #endregion

            #region OutputEventMessage
            if (msg is OutputEventMessage oem)
            {
                Logging.Log(SyslogLevel.LOG_NOTICE, "[OUTPUT EVENT] Location 0x{0:x2} changed state to 0x{1:x2}", oem.OutputLocation, oem.OutputState);

                //            1         2                   3
                DoEventScript("OUTPUT", oem.OutputLocation, oem.OutputState);
                return;
            }
            #endregion

            #region AreaEventMessage
            if (msg is AreaEventMessage aem)
            {
                string areaText = null;

                if (s_AreaTextCache.ContainsKey(aem.AreaNumber))
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Cache hit for area {0} text.", aem.AreaNumber);

                    areaText = s_AreaTextCache[aem.AreaNumber];
                }
                else
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Cache miss for area {0} text.", aem.AreaNumber);

                    PanelMessage resp =
                        controller.SendMessageSync(PanelSimpleProtocol.GetAreaText(aem.AreaNumber, 1));

                    if (resp == null)
                        goto fail;

                    GetAreaTextResponse atr;
                    try
                    {
                        atr = PanelResponsePayload.Parse(resp.Payload) as GetAreaTextResponse;
                    }
                    catch (Exception e)
                    {
                        Logging.Log(SyslogLevel.LOG_ERR, "Exception while parsing area text response from panel: {0}", e);
                        goto fail;
                    }

                    if (!atr.Success)
                        goto fail;

                    areaText = atr.AreaText[0];
                    s_AreaTextCache.Add(aem.AreaNumber, areaText);
                }

            fail:
                if (string.IsNullOrEmpty(areaText)) areaText = "<ERROR>";

                Logging.Log(SyslogLevel.LOG_NOTICE, "[AREA EVENT] Area {0:n0} (\"{1}\") changed state to: {2}", aem.AreaNumber, areaText,
                    aem.AreaState.AreaState);

                //            1       2               3         4
                DoEventScript("AREA", aem.AreaNumber, areaText, aem.AreaState.AreaState);
                return;
            }
            #endregion

            #region ZoneEventMessage
            if (msg is ZoneEventMessage zem)
            {
                string zoneText = null;

                if (s_ZoneTextCache.ContainsKey(zem.ZoneNumber))
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Cache hit for zone {0} text.", zem.ZoneNumber);

                    zoneText = s_ZoneTextCache[zem.ZoneNumber];
                }
                else
                {
                    Logging.Log(SyslogLevel.LOG_DEBUG, "Cache miss for zone {0} text.", zem.ZoneNumber);

                    PanelMessage resp =
                        controller.SendMessageSync(PanelSimpleProtocol.GetZoneText(TexecomElitePanelType.Elite12_64, zem.ZoneNumber, 1));

                    if (resp == null)
                        goto fail;

                    GetZoneTextResponse ztr;

                    try
                    {
                        ztr = PanelResponsePayload.Parse(resp.Payload) as GetZoneTextResponse;
                    }
                    catch (Exception e)
                    {
                        Logging.Log(SyslogLevel.LOG_ERR, "Exception while parsing zone text response from panel: {0}", e);
                        goto fail;
                    }

                    if (!ztr.Success)
                        goto fail;

                    zoneText = ztr.ZoneText[0];
                    s_ZoneTextCache.Add(zem.ZoneNumber, zoneText);
                }

            fail:
                if (string.IsNullOrEmpty(zoneText)) zoneText = "<ERROR>";

                Logging.Log(SyslogLevel.LOG_NOTICE, "[ZONE EVENT] Zone {0:n0} (\"{1}\") changed to state: {2}. Alarmed? {3}. Auto bypassed? {4}. " +
                    "Manual bypassed? {6}. Failed test? {5}. Faulty? {6}. Masked? {7}.", zem.ZoneNumber, zoneText, zem.ZoneState.ZoneState,
                    zem.ZoneState.Alarmed, zem.ZoneState.AutoBypassed, zem.ZoneState.FailedTest, zem.ZoneState.ManualBypassed,
                    zem.ZoneState.ZoneInFault, zem.ZoneState.ZoneMasked);


                //            1       2               3         4                        5                      6
                DoEventScript("ZONE", zem.ZoneNumber, zoneText, zem.ZoneState.ZoneState, zem.ZoneState.Alarmed, zem.ZoneState.AutoBypassed,
                    //  7                         8                             9                          10
                    zem.ZoneState.FailedTest, zem.ZoneState.ManualBypassed, zem.ZoneState.ZoneInFault, zem.ZoneState.ZoneMasked);
                return;
            }
            #endregion

            #region DebugMessage
            if (msg is DebugMessage dem)
            {
                Logging.Log(SyslogLevel.LOG_INFO, "[PANEL DEBUG] {0}", dem.ToString());

                //            1        2
                DoEventScript("DEBUG", dem.ToString());
                return;
            }
            #endregion

            throw new ArgumentException("Don't know how to interpret msg object.", "msg");
        }

        private static void DoEventScript(string eventType, params object[] args)
        {
            if (SETTING_EVENTSCRIPT == null)
            {
                Logging.Log(SETTING_LOG_EVENTSCRIPT ? SyslogLevel.LOG_NOTICE : SyslogLevel.LOG_DEBUG,
                    "No event script specified in settings. Nothing to do.");

                return;
            }

            Logging.Log(SyslogLevel.LOG_DEBUG, "Converting arguments for event script.");

            List<string> stringargs = new List<string>
            {
                eventType
            };

            foreach (object o in args)
            {
                if (o is string s)
                {
                    stringargs.Add(s);
                    continue;
                }

                if (o is bool b)
                {
                    stringargs.Add(b ? "TRUE" : "FALSE");
                    continue;
                }

                if (o is DateTime d)
                {
                    stringargs.Add(d.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString(CultureInfo.InvariantCulture));
                    continue;
                }

                stringargs.Add(o.ToString());
            }

            Logging.Log(SyslogLevel.LOG_DEBUG, "Preparing event script command line.");

            #region stringargs list to command line
            string cmdargs = null;

            {
                StringBuilder sb = new StringBuilder();
                foreach (string s in stringargs)
                {
                    sb.Append('"');

                    // Escape double quotes (") and backslashes (\).
                    int searchIndex = 0;

                    for (; ; )
                    {
                        // Put this test first to support zero length strings.
                        if (searchIndex >= s.Length)
                            break;

                        int quoteIndex = s.IndexOf('"', searchIndex);

                        if (quoteIndex < 0)
                            break;

                        sb.Append(s, searchIndex, quoteIndex - searchIndex);
                        EscapeBackslashes(sb, s, quoteIndex - 1);
                        sb.Append('\\');
                        sb.Append('"');
                        searchIndex = quoteIndex + 1;
                    }

                    sb.Append(s, searchIndex, s.Length - searchIndex);
                    EscapeBackslashes(sb, s, s.Length - 1);

                    sb.Append(@""" ");
                }

                cmdargs = sb.ToString(0, Math.Max(0, sb.Length - 1));
            }
            #endregion

            using (Process p = Process.Start(SETTING_EVENTSCRIPT, cmdargs)) { }

            Logging.Log(SETTING_LOG_EVENTSCRIPT ? SyslogLevel.LOG_NOTICE : SyslogLevel.LOG_DEBUG,
                "Event script: {0} {1}", SETTING_EVENTSCRIPT, cmdargs);
        }

        private static void EscapeBackslashes(StringBuilder mysb, string mys, int mylsi)
        {
            for (int i = mylsi; i >= 0; i--)
                if (mys[i] != '\\')
                    break;
                else
                    mysb.Append('\\');
        }
    }
}
