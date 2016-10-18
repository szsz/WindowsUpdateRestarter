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

        static Notification restart = new Notification(new string[] {
                "Restarting Windows",
                "Run \"shutdown /a\" within 5 minutes to cancel."}, new Tuple<string, string>[] {
                    new Tuple<string, string>("OK", "OK")
                }, Notification.Scenario.reminder);
        static void Restart()
        {
            Console.WriteLine("Restarting");
            string param = "/f /r /t 600";
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.FileName = "cmd";
            proc.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Arguments = "/C shutdown " + param;
            Process.Start(proc);

            Thread.Sleep(1000 * 15);
            restart.ShowToast();

            Thread.Sleep(1000 * 15);

            Environment.Exit(0);
        }

        static bool NeedsResstart()
        {

            bool req = false;
            RegistryKey localKey =
                RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine,
                    RegistryView.Registry64);
            localKey = localKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
            if (localKey != null)
            {
                req = true;
            }
#if DEBUG
            req = true;
#endif
            return req;
        }



        static DateTime timeFirstNeedForRestart = DateTime.MinValue;
        static DateTime timeAcceptedNotification = DateTime.MinValue;
        static DateTime remindTime = DateTime.MinValue;

        // this should be min value if there is currently no notification displayed
        static DateTime notificationDisplayedTime = DateTime.MinValue;


        static void Main(string[] args)
        {
            Thread.Sleep(5 * 1000);
            Notification n = new Notification(new string[] {
                "Restart Windows",
                "Windows update will automatically restart your machine." },
                new Tuple<string, string>[] {
                    new Tuple<string, string>("Restart at 2 a.m.", "Restart"),
                    new Tuple<string, string>("Remind me in 1 hour", "Remind")
                }, Notification.Scenario.reminder);
            Notification ack = new Notification(new string[] {
                "Windows Will Restart",
                "Windows update will automatically restart your machine at 2 a.m." },
                new Tuple<string, string>[] {
                    new Tuple<string, string>("OK", "OK")
                });
            n.Activated += (Notification x, string e) =>
            {
                if (e != null)
                {
                    if (e == "Restart")
                    {
                        timeAcceptedNotification = DateTime.Now;
                        ack.ShowToast();
                    }
                    if (e == "Remind")
                        remindTime = DateTime.Now;
                }
            };

            n.Closed += (Notification x) =>
                {
                    notificationDisplayedTime = DateTime.MinValue;
                };


            while (true)
            {
                DateTime cur = DateTime.Now;
                if ((cur - remindTime).TotalHours > 1)
                {
                    if (timeFirstNeedForRestart == DateTime.MinValue)
                    {
                        //check restart and set
                        if (NeedsResstart())
                        {
                            timeFirstNeedForRestart = cur;
                            continue;
                        }
                    }
                    else
                    {
                        if (timeAcceptedNotification == DateTime.MinValue)
                        {
                            if ((cur - timeFirstNeedForRestart).TotalHours > 72 && cur.Hour == 2)
                            {
                                Restart();
                            }
                            else if ((cur - timeFirstNeedForRestart).TotalHours > 96)
                            {
                                Restart();
                            }
                            else
                            {
                                if ((cur - notificationDisplayedTime).TotalHours > 12)
                                {
                                    notificationDisplayedTime = DateTime.Now;
                                    n.ShowToast();
                                }
                            }
                        }
                        else
                        {
#if DEBUG
                            Restart();
#endif
                            if (cur.Hour == 2 && (cur - timeAcceptedNotification).TotalHours > 1)
                            {
                                Restart();
                            }
                        }
                    }
                }

                int sleepTime = 1000 * 60 * 10; // 10 minutes
#if DEBUG
                sleepTime = 1000 * 10; // 10 seconds
#endif
                Thread.Sleep(sleepTime);

            }


        }
    }
}
