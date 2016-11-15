using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.IO;

namespace WindowsUpdateRestarter
{
    public class Notification
    {
        public enum Scenario
        {
            reminder,
            Default,
            alarm,
            incomingCall

        };

        Scenario scen;

        public Notification(string[] message, Tuple<string, string>[] buttons, Scenario s = Scenario.Default)
        {
            this.scen = s;
            this.message = message;
            if (message.Length != 2)
                throw (new ArgumentException());
            this.buttons = buttons;
        }

        string[] message;
        Tuple<string, string>[] buttons;

        private const String APP_ID = "WindowsUpdateRestarter";

        public void ShowToast()
        {
            // Specify the absolute path to an image
            String imagePath = "file:///" + AppDomain.CurrentDomain.BaseDirectory.Replace(@"\", "/") + "Restart.png";

            string actions = "";
            foreach (Tuple<string, string> item in buttons)
            {
                actions += string.Format("<action content=\"{0}\" arguments=\"{1}\"/>", item.Item1, item.Item2);
            }

            string s = String.Format("<toast scenario=\"{4}\">"
                + "<visual><binding template=\"ToastGeneric\">"
                + "<image placement=\"AppLogoOverride\" src=\"{0}\"/>"
                + " <text>{1}</text><text>{2}</text></binding></visual>"
                + " <actions>{3}</actions>"
                + "</toast>", imagePath, message[0], message[1], actions, scen);

            // Create the toast and attach event listeners
            XmlDocument toastXml = new XmlDocument();
            toastXml.LoadXml(s);

            ToastNotification toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;
            toast.Dismissed += ToastDismissed;
            toast.Failed += ToastFailed;

            Log("Show.");
            // Show the toast. Be sure to specify the AppUserModelId on your application's shortcut!
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }

        public event Action<Notification, string> Activated;

        public event Action<Notification> Closed;

        private void Log(string msg)
        {
            Logger.Log($"[Toast - {message[0]}] {msg}");
        }

        private void ToastActivated(ToastNotification sender, object e)
        {
            Log("The user activated the toast.");

            if (e is ToastActivatedEventArgs)
                Activated?.Invoke(this, ((ToastActivatedEventArgs)e).Arguments);

            Closed?.Invoke(this);
        }

        private void ToastDismissed(ToastNotification sender, ToastDismissedEventArgs e)
        {
            var outputText = "";
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

            Log(outputText);
            Closed?.Invoke(this);
        }

        private void ToastFailed(ToastNotification sender, ToastFailedEventArgs e)
        {
            Closed?.Invoke(this);
            Log($"Toast error: {e.ErrorCode}");
        }
    }
}
