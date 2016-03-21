using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MS.WindowsAPICodePack.Internal;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.IO;
using System.Diagnostics;
using ShellHelpers;

namespace WindowsUpdateRestarter
{
    class Notification
    {

        public Notification(string[] message, Tuple<string, string>[] buttons)
        {
            TryCreateShortcut();
            this.message = message;
            if (message.Length != 2)
                throw (new ArgumentException());
            this.buttons = buttons;
        }

        string[] message;
        Tuple<string, string>[] buttons;

        private const String APP_ID = "WindowsUpdateRestarter";

        // In order to display toasts, a desktop application must have a shortcut on the Start menu.
        // Also, an AppUserModelID must be set on that shortcut.
        // The shortcut should be created as part of the installer. The following code shows how to create
        // a shortcut and assign an AppUserModelID using Windows APIs. You must download and include the 
        // Windows API Code Pack for Microsoft .NET Framework for this code to function
        //
        // Included in this project is a wxs file that be used with the WiX toolkit
        // to make an installer that creates the necessary shortcut. One or the other should be used.
        private bool TryCreateShortcut()
        {
            String shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\Start Menu\\Programs\\WindowsUpdateRestarter.lnk";
            if (!File.Exists(shortcutPath))
            {
                InstallShortcut(shortcutPath);
                return true;
            }
            return false;
        }

        private void InstallShortcut(String shortcutPath)
        {
            // Find the path to the current executable
            String exePath = Process.GetCurrentProcess().MainModule.FileName;
            IShellLinkW newShortcut = (IShellLinkW)new CShellLink();

            // Create a shortcut to the exe
            ShellHelpers.ErrorHelper.VerifySucceeded(newShortcut.SetPath(exePath));
            ShellHelpers.ErrorHelper.VerifySucceeded(newShortcut.SetArguments(""));

            // Open the shortcut property store, set the AppUserModelId property
            IPropertyStore newShortcutProperties = (IPropertyStore)newShortcut;

            using (PropVariant appId = new PropVariant(APP_ID))
            {
                ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutProperties.SetValue(SystemProperties.System.AppUserModel.ID, appId));
                ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutProperties.Commit());
            }

            // Commit the shortcut to disk
            IPersistFile newShortcutSave = (IPersistFile)newShortcut;

            ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutSave.Save(shortcutPath, true));

        }

        public void ShowToast()
        {
            // Specify the absolute path to an image
            String imagePath = "file:///" + Path.GetFullPath("Restart.png");

            string actions = "";
            foreach (Tuple<string, string> item in buttons)
            {
                actions += string.Format("<action content=\"{0}\" arguments=\"{1}\"/>", item.Item1, item.Item2);
            }

            string s = String.Format("<toast scenario=\"reminder\">"
                + "<visual><binding template=\"ToastGeneric\">"
                + "<image placement=\"AppLogoOverride\" src=\"{0}\"/>"
                + " <text>{1}</text><text>{2}</text></binding></visual>"
                + " <actions>{3}</actions>"
                + "</toast>", imagePath, message[0], message[1], actions);

            // Create the toast and attach event listeners
            XmlDocument toastXml = new XmlDocument(); toastXml.LoadXml(s);
            ToastNotification toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;
            toast.Dismissed += ToastDismissed;
            toast.Failed += ToastFailed;

            Console.WriteLine("show toast");
            // Show the toast. Be sure to specify the AppUserModelId on your application's shortcut!
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }

        public event Action<Notification, string> Activated;

        private void ToastActivated(ToastNotification sender, object e)
        {
            Console.WriteLine("The user activated the toast.");
            var a = Activated;
            if (a != null && e != null && e is Windows.UI.Notifications.ToastActivatedEventArgs)
                a(this, ((Windows.UI.Notifications.ToastActivatedEventArgs)e).Arguments);

        }

        private void ToastDismissed(ToastNotification sender, ToastDismissedEventArgs e)
        {
            String outputText = "";
            switch (e.Reason)
            {
                case ToastDismissalReason.ApplicationHidden:
                    outputText = "The app hid the toast using ToastNotifier.Hide";
                    break;
                case ToastDismissalReason.UserCanceled:
                    outputText = "The user dismissed the toast";
                    break;
                case ToastDismissalReason.TimedOut:
                    outputText = "The toast has timed out";
                    break;
            }

            Console.WriteLine(outputText);
        }

        private void ToastFailed(ToastNotification sender, ToastFailedEventArgs e)
        {
            Console.WriteLine("error");
        }
    }
}
