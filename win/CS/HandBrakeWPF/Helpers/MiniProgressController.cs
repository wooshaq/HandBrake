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

    using HandBrakeWPF.Views;

    public static class MiniProgressController
    {
        private static MiniProgressWindow window;
        private static bool hiddenByUser;
        private static Point? lastPosition;

        public static void Update(double percent, double fps, TimeSpan? timeLeft, bool paused)
        {
            if (hiddenByUser || Application.Current == null)
            {
                return;
            }

            if (window == null)
            {
                window = new MiniProgressWindow();
                Point position = lastPosition ?? DefaultPosition();
                window.Left = position.X;
                window.Top = position.Y;
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

            lastPosition = new Point(window.Left, window.Top);
            window.Close();
            window = null;
        }

        private static Point DefaultPosition()
        {
            Rect area = SystemParameters.WorkArea;
            return new Point(area.Right - 112 - 16, area.Bottom - 112 - 16);
        }
    }
}
