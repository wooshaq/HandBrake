// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MiniProgressWindow.xaml.cs" company="HandBrake Project (https://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   WsQ: small always-on-top circular encode progress indicator.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HandBrakeWPF.Views
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;

    using HandBrakeWPF.Helpers;

    public partial class MiniProgressWindow : Window
    {
        private const double Center = 48;
        private const double Radius = 41;
        private const double StartAngle = 135; // bottom-left, clockwise through the top to bottom-right
        private const double TotalSweep = 270;
        private const double BaseWindowSize = 112; // 96 px circle + room for the shadow
        private const double MinScale = 0.6;
        private const double MaxScale = 3.0;
        private const double ScaleStep = 1.1;

        private readonly DispatcherTimer saveTimer;
        private double lastPercent = -1;
        private double scale = 1.0;
        private bool isLoaded;

        public MiniProgressWindow()
        {
            this.InitializeComponent();
            this.trackArc.Data = BuildArc(TotalSweep);
            this.progressArc.Data = BuildArc(0);

            // Debounce saving of position / scale so dragging or scrolling doesn't hammer the settings file.
            this.saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            this.saveTimer.Tick += (s, e) =>
            {
                this.saveTimer.Stop();
                MiniProgressController.SavePlacement(this.Left, this.Top, this.scale);
            };

            this.Loaded += (s, e) => this.isLoaded = true;
            this.Closing += (s, e) =>
            {
                if (this.saveTimer.IsEnabled)
                {
                    this.saveTimer.Stop();
                    MiniProgressController.SavePlacement(this.Left, this.Top, this.scale);
                }
            };
        }

        public double Scale => this.scale;

        public void ApplyScale(double newScale)
        {
            this.scale = Math.Max(MinScale, Math.Min(MaxScale, newScale));
            this.scaleTransform.ScaleX = this.scale;
            this.scaleTransform.ScaleY = this.scale;
            this.Width = BaseWindowSize * this.scale;
            this.Height = BaseWindowSize * this.scale;
        }

        public void SetProgress(double percent, double fps, TimeSpan? timeLeft, bool paused)
        {
            percent = Math.Max(0, Math.Min(100, percent));

            if (Math.Abs(percent - this.lastPercent) >= 0.05)
            {
                this.progressArc.Data = BuildArc(TotalSweep * percent / 100.0);
                this.lastPercent = percent;
            }

            this.percentText.Text = string.Format(CultureInfo.InvariantCulture, "{0}%", Math.Floor(percent));
            this.etaText.Text = timeLeft.HasValue && timeLeft.Value > TimeSpan.Zero ? "−" + FormatTime(timeLeft.Value) : string.Empty;

            if (paused)
            {
                this.fpsText.Text = Properties.Resources.MainView_Pause;
                this.progressArc.Opacity = 0.45;
            }
            else
            {
                this.fpsText.Text = fps > 0 ? string.Format(CultureInfo.InvariantCulture, "{0:0} fps", fps) : string.Empty;
                this.progressArc.Opacity = 1;
            }

            this.statusTip.Text = string.Format(CultureInfo.CurrentCulture, "{0:0.0}%  {1}  {2}", percent, this.fpsText.Text, this.etaText.Text).Trim();
        }

        public void SetPaused()
        {
            this.fpsText.Text = Properties.Resources.MainView_Pause;
            this.progressArc.Opacity = 0.45;
        }

        private static string FormatTime(TimeSpan t)
        {
            if (t.TotalHours >= 1)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", t.Minutes, t.Seconds);
        }

        private static Point PointAt(double angleDeg)
        {
            double a = angleDeg * Math.PI / 180.0;
            return new Point(Center + (Radius * Math.Cos(a)), Center + (Radius * Math.Sin(a)));
        }

        private static Geometry BuildArc(double sweep)
        {
            if (sweep <= 0.01)
            {
                return Geometry.Empty;
            }

            sweep = Math.Min(sweep, 359.9);
            PathFigure figure = new PathFigure { StartPoint = PointAt(StartAngle), IsClosed = false, IsFilled = false };
            figure.Segments.Add(new ArcSegment(PointAt(StartAngle + sweep), new Size(Radius, Radius), 0, sweep > 180, SweepDirection.Clockwise, true));

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            geometry.Freeze();
            return geometry;
        }

        private void Window_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MiniProgressController.ShowMainWindow();
                return;
            }

            try
            {
                this.DragMove();
            }
            catch (InvalidOperationException)
            {
                // DragMove can throw if the mouse button was released already.
            }
        }

        private void Window_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                return;
            }

            e.Handled = true;

            double oldSize = this.Width;
            double centerX = this.Left + (oldSize / 2);
            double centerY = this.Top + (oldSize / 2);

            this.ApplyScale(e.Delta > 0 ? this.scale * ScaleStep : this.scale / ScaleStep);

            // Grow / shrink around the centre of the circle.
            this.Left = centerX - (this.Width / 2);
            this.Top = centerY - (this.Height / 2);
            this.RestartSaveTimer();
        }

        private void Window_OnLocationChanged(object sender, EventArgs e)
        {
            if (this.isLoaded)
            {
                this.RestartSaveTimer();
            }
        }

        private void RestartSaveTimer()
        {
            this.saveTimer.Stop();
            this.saveTimer.Start();
        }

        private void ShowMain_OnClick(object sender, RoutedEventArgs e)
        {
            MiniProgressController.ShowMainWindow();
        }

        private void Hide_OnClick(object sender, RoutedEventArgs e)
        {
            MiniProgressController.HideByUser();
        }
    }
}
