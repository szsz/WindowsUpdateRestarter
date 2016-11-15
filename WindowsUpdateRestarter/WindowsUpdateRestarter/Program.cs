using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsUpdateRestarter
{
    class Program
    {
        static readonly Notification RestartNotif = new Notification(new[] { "Restarting Windows", "Run \"shutdown /a\" within 5 minutes to cancel."}, 
            new[] { new Tuple<string, string>("OK", "OK") }, Notification.Scenario.reminder);

        static void Restart()
        {
            Logger.Log("Restarting");

            var p = Process.Start(new ProcessStartInfo { FileName = "cmd", Arguments = "/C shutdown /f /r /t 600", WindowStyle = ProcessWindowStyle.Hidden });
            try
            {
                p?.WaitForExit(5000);
                Logger.Log($"Process result: {p?.ExitCode}");
            }
            catch(Exception e)
            {
                Logger.Log($"Failed to get shutdown process result: {e}");
            }

            Thread.Sleep(1000 * 15);
            Logger.Log("Showing abort notification...");
            RestartNotif.ShowToast();

            Thread.Sleep(1000 * 15);
            Logger.Log("Exiting...");
            Environment.Exit(0);
        }

        static bool NeedsRestart()
        {
            bool req = false;
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var localKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
            {
                if (localKey != null)
                {
                    req = true;
                }
#if DEBUG
            req = true;
#endif
                return req;
            }
        }

        static DateTime detectedRestartNeededTime = DateTime.MinValue;
        static DateTime userAccepted2AmTime = DateTime.MinValue;
        static DateTime userAskedRemindTime = DateTime.MinValue;

        // this should be min value if there is currently no notification displayed
        static DateTime notifOnScreenSince = DateTime.MinValue;

        static void Main(string[] args)
        {
            try
            {
                Logger.Log(ShortcutInstaller.TryCreateShortcut() ? "Installed shortcut" : "Shortcut was already installed.");
            }
            catch (Exception e)
            {
                Logger.Log($"Could not install shortcut: {e}");
            }

            try
            {

                //Thread.Sleep(5 * 1000);
                var askNotif = new Notification(new[] { "Restart Windows", "Windows update will automatically restart your machine." },
                    new[]
                    {
                        new Tuple<string, string>("Restart at 2 a.m.", "Restart"),
                        new Tuple<string, string>("Remind me in 1 hour", "Remind")
                    }, Notification.Scenario.reminder);

                var ack2AmNotif = new Notification(new[] { "Windows Will Restart", "Windows update will automatically restart your machine at 2 a.m." },
                    new[] { new Tuple<string, string>("OK", "OK") });

                askNotif.Activated += (x, e) =>
                {
                    Logger.Log($"User selected the following toast button: {e}.");

                    if (e == null) return;

                    if (e == "Restart")
                    {
                        userAccepted2AmTime = DateTime.Now;
                        ack2AmNotif.ShowToast();
                    }

                    if (e == "Remind")
                        userAskedRemindTime = DateTime.Now;
                };

                askNotif.Closed += x =>
                {
                    Logger.Log("Notification was closed.");
                    notifOnScreenSince = DateTime.MinValue;
                };

                while (true)
                {
                    try
                    {
                        var cur = DateTime.Now;
#if DEBUG
                        if ((cur - userAskedRemindTime).TotalSeconds > 1) // notification not shown yet or user asked to remind him/her in 1 hour which elapsed
#else
                    if ((cur - userAskedRemindTime).TotalHours > 1) // notification not shown yet or user asked to remind him/her in 1 hour which elapsed
#endif
                        {
                            if (detectedRestartNeededTime == DateTime.MinValue) // we did not detect the need of restart yet
                            {
                                //check restart and set
                                if (NeedsRestart())
                                {
                                    Logger.Log("Detected that restart is needed!");
                                    detectedRestartNeededTime = cur;
                                    continue;
                                }
                            }
                            else if (userAccepted2AmTime == DateTime.MinValue) // restart is needed, but the user did not accept 2AM restart
                            {
                                if ((cur - detectedRestartNeededTime).TotalHours > 72 && cur.Hour == 2) // it's 2:XX AM and restart is needed for at least 72 hours
                                {
                                    Logger.Log("Restarting because it's 2:XX AM and restart is needed for at least 72 hours.");
                                    Restart();
                                }
                                else if ((cur - detectedRestartNeededTime).TotalHours > 96) // restart is needed for at least 96 hours
                                {
                                    Logger.Log("Restarting because restart is needed for at least 96 hours.");
                                    Restart();
                                }
                                else if ((cur - notifOnScreenSince).TotalHours > 12) // notification was never shown or was shown 12 hours ago and was not closed since, show it to the user again
                                {
                                    Logger.Log("Notification was never shown or was shown 12 hours ago and was not closed since, show it to the user again.");
                                    notifOnScreenSince = DateTime.Now;
                                    askNotif.ShowToast();
                                }
                            }
                            else // restart is needed and user accepted 2AM restart
                            {
#if DEBUG
                                Thread.Sleep(8000);
                                Restart();
#endif
                                if (cur.Hour == 2 && (cur - userAccepted2AmTime).TotalHours > 1) // it's 2:XX AM and user accepted 2AM restart at least 1 hour ago
                                {
                                    Logger.Log("Restarting because it's 2:XX AM and user accepted 2AM restart at least 1 hour ago.");
                                    Restart();
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log("Exception in main loop: " + e);
                    }

                    int sleepTime = 1000 * 60 * 10; // 10 minutes
#if DEBUG
                    sleepTime = 1000 * 1; // 10 seconds
#endif
                    Thread.Sleep(sleepTime);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Could not start program: {e}");
            }
        }
    }
}
