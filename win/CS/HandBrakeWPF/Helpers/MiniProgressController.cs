// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MiniProgressController.cs" company="HandBrake Project (https://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   WsQ: shows, updates and hides the floating circular progress indicator while encoding.
//   Must be called on the UI thread.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HandBrakeWPF.Helpers
{
    using System;
    using System.Windows;

    using HandBrakeWPF.Services.Interfaces;
    using HandBrakeWPF.Views;

    public static class MiniProgressController
    {
        // Stored in HandBrake's settings.json so the circle keeps its place and size between runs.
        private const string SettingHasPosition = "WsqMiniProgressHasPosition";
        private const string SettingLeft = "WsqMiniProgressLeft";
        private const string SettingTop = "WsqMiniProgressTop";
        private const string SettingScale = "WsqMiniProgressScale";

        private static MiniProgressWindow window;
        private static bool hiddenByUser;

        public static void Update(double percent, double fps, TimeSpan? timeLeft, bool paused)
        {
            if (hiddenByUser || Application.Current == null)
            {
                return;
            }

            if (window == null)
            {
                window = new MiniProgressWindow();
                LoadPlacement(window);
            }

            window.SetProgress(percent, fps, timeLeft, paused);

            if (!window.IsVisible)
            {
                window.Show();
            }
        }

        public static void SetPaused()
        {
            window?.SetPaused();
        }

        /// <summary>
        /// Close the indicator. resetUserChoice = true when the whole queue finished, so it shows again next time.
        /// </summary>
        public static void Close(bool resetUserChoice)
        {
            if (resetUserChoice)
            {
                hiddenByUser = false;
            }

            CloseWindow();
        }

        public static void HideByUser()
        {
            hiddenByUser = true;
            CloseWindow();
        }

        public static void ShowMainWindow()
        {
            Window main = Application.Current?.MainWindow;
            if (main == null)
            {
                return;
            }

            main.Show();
            if (main.WindowState == WindowState.Minimized)
            {
                main.WindowState = WindowState.Normal;
            }

            main.Activate();
        }

        private static void CloseWindow()
        {
            if (window == null)
            {
                return;
            }

            window.Close();
            window = null;
        }

        public static void SavePlacement(double left, double top, double scale)
        {
            IUserSettingService settings = GetSettings();
            if (settings == null || double.IsNaN(left) || double.IsNaN(top))
            {
                return;
            }

            settings.SetUserSetting(SettingLeft, left);
            settings.SetUserSetting(SettingTop, top);
            settings.SetUserSetting(SettingScale, scale);
            settings.SetUserSetting(SettingHasPosition, true);
        }

        private static void LoadPlacement(MiniProgressWindow w)
        {
            IUserSettingService settings = GetSettings();

            double scale = settings?.GetUserSetting<double>(SettingScale) ?? 0;
            w.ApplyScale(scale > 0 ? scale : 1.0);

            Rect area = SystemParameters.WorkArea;
            double left = area.Right - w.Width - 16;
            double top = area.Bottom - w.Height - 16;

            if (settings != null && settings.GetUserSetting<bool>(SettingHasPosition))
            {
                left = settings.GetUserSetting<double>(SettingLeft);
                top = settings.GetUserSetting<double>(SettingTop);

                // Keep it reachable if the monitor layout changed since last time.
                double vLeft = SystemParameters.VirtualScreenLeft;
                double vTop = SystemParameters.VirtualScreenTop;
                double vRight = vLeft + SystemParameters.VirtualScreenWidth;
                double vBottom = vTop + SystemParameters.VirtualScreenHeight;
                left = Math.Max(vLeft, Math.Min(left, vRight - w.Width));
                top = Math.Max(vTop, Math.Min(top, vBottom - w.Height));
            }

            w.Left = left;
            w.Top = top;
        }

        private static IUserSettingService GetSettings()
        {
            try
            {
                return IoCHelper.Get<IUserSettingService>();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
