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
                "Run \"shutdown /a\" within 5 minutes to cancel."}, new Tuple<string, string>[] { });
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

            Thread.Sleep(1000 * 60 * 10);
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
            return req;
        }



        static DateTime timeFirstNeedForRestart = DateTime.MinValue;
        static DateTime timeAcceptedNotification = DateTime.MinValue;
        static DateTime remindTime = DateTime.MinValue;



        static void Main(string[] args)
        {
            Thread.Sleep(5 * 1000);
            Notification n = new Notification(new string[] {
                "Restart Windows",
                "Windows update will automatically restart your machine." },
                new Tuple<string, string>[] {
                    new Tuple<string, string>("Restart At 2AM", "Restart"), 
                    new Tuple<string, string>("Remind me in 1 hour", "Remind") 
                });
            n.Activated += (Notification x, string e) =>
            {
                if (e != null)
                {
                    if (e == "Restart")
                        timeAcceptedNotification = DateTime.Now;
                    if (e == "Remind")
                        remindTime = DateTime.Now;
                }
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
                                n.ShowToast();
                            }
                        }
                        else
                        {
                            if (cur.Hour == 2 && (cur - timeAcceptedNotification).TotalHours > 1)
                            {
                                Restart();
                            }
                        }
                    }
                }


                Thread.Sleep(1000 * 60 * 10);

            }


        }
    }
}
