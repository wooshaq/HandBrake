// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShellView.xaml.cs" company="HandBrake Project (http://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   Interaction logic for ShellView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HandBrakeWPF.Views
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Resources;

    using HandBrakeWPF.Commands;
    using HandBrakeWPF.Helpers;
    using HandBrakeWPF.Model.Options;
    using HandBrakeWPF.Services.Interfaces;
    using HandBrakeWPF.Utilities;
    using HandBrakeWPF.ViewModels;
    using HandBrakeWPF.ViewModels.Interfaces;

    using Application = System.Windows.Application;
    using FlowDirection = System.Windows.FlowDirection;

    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : Window
    {
        private const int DwmwaCloak = 13;

        private INotifyIconService notifyIconService;
        private bool isCloaked;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowHelper.SetDarkMode(this);

            // WsQ: no bright flash on startup. Paint the native surface with the theme background instead of the
            // default white, and keep the window cloaked (invisible but "shown") until WPF has rendered the first frame.
            IntPtr handle = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(handle);
            if (source?.CompositionTarget != null && this.Background is SolidColorBrush brush)
            {
                source.CompositionTarget.BackgroundColor = brush.Color;
            }

            try
            {
                int cloak = 1;
                this.isCloaked = DwmSetWindowAttribute(handle, DwmwaCloak, ref cloak, sizeof(int)) == 0;
            }
            catch (Exception)
            {
                this.isCloaked = false;
            }

            if (this.isCloaked)
            {
                this.ContentRendered += this.ShellView_ContentRendered;
            }
        }

        private void ShellView_ContentRendered(object sender, EventArgs e)
        {
            this.ContentRendered -= this.ShellView_ContentRendered;

            // Wait for the first full layout + render pass of the main view before revealing the window.
            this.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (!this.isCloaked)
                    {
                        return;
                    }

                    this.isCloaked = false;
                    int cloak = 0;
                    DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, DwmwaCloak, ref cloak, sizeof(int));
                }),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellView"/> class.
        /// </summary>
        public ShellView()
        {
            this.DataContext = IoCHelper.Get<IShellViewModel>();

            this.InitializeComponent();

            IUserSettingService userSettingService = IoCHelper.Get<IUserSettingService>();
            bool minimiseToTray = userSettingService.GetUserSetting<bool>(UserSettingConstants.MainWindowMinimize);

            if (minimiseToTray)
            {
                StreamResourceInfo streamResourceInfo = Application.GetResourceStream(new Uri("pack://application:,,,/handbrakepineapple.ico"));
                if (streamResourceInfo != null)
                {
                    Stream iconStream = streamResourceInfo.Stream;

                    notifyIconService = IoCHelper.Get<INotifyIconService>();
                    notifyIconService.Setup(new Icon(iconStream));
                    this.notifyIconService.SetClickCallback(() => this.NotifyIconClick());
                }

                this.StateChanged += this.ShellViewStateChanged;
            }

            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.E, ModifierKeys.Control)), new KeyGesture(Key.E, ModifierKeys.Control))); // Start Encode
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.P, ModifierKeys.Alt)), new KeyGesture(Key.P, ModifierKeys.Alt))); // Pause Encode
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.K, ModifierKeys.Control)), new KeyGesture(Key.K, ModifierKeys.Control))); // Stop Encode
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.L, ModifierKeys.Control)), new KeyGesture(Key.L, ModifierKeys.Control))); // Open Log Window
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.Q, ModifierKeys.Control)), new KeyGesture(Key.Q, ModifierKeys.Control))); // Open Queue Window
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.A, ModifierKeys.Alt)), new KeyGesture(Key.A, ModifierKeys.Alt))); // Add to Queue
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt)), new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Alt))); // Add all to Queue
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift)), new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift))); // Add selection to Queue

            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.O, ModifierKeys.Control)), new KeyGesture(Key.O, ModifierKeys.Control))); // File Scan
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.O, ModifierKeys.Alt)), new KeyGesture(Key.O, ModifierKeys.Alt))); // Scan Window
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift)), new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift))); // Scan a Folder
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift)), new KeyGesture(Key.G, ModifierKeys.Control | ModifierKeys.Shift))); // Garbage Collection
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.F1, ModifierKeys.None)), new KeyGesture(Key.F1, ModifierKeys.None))); // Help
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.S, ModifierKeys.Control)), new KeyGesture(Key.S, ModifierKeys.Control))); // Browse Destination

            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.OemPlus, ModifierKeys.Control)), new KeyGesture(Key.OemPlus, ModifierKeys.Control))); // Next Title
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.OemMinus, ModifierKeys.Control)), new KeyGesture(Key.OemMinus, ModifierKeys.Control))); // Previous Title
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.Add, ModifierKeys.Control)), new KeyGesture(Key.Add, ModifierKeys.Control))); // Next Title
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.Subtract, ModifierKeys.Control)), new KeyGesture(Key.Subtract, ModifierKeys.Control))); // Previous Title

            // Tabs Switching
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.D1, ModifierKeys.Control)), new KeyGesture(Key.D1, ModifierKeys.Control))); 
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.D2, ModifierKeys.Control)), new KeyGesture(Key.D2, ModifierKeys.Control))); 
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.D3, ModifierKeys.Control)), new KeyGesture(Key.D3, ModifierKeys.Control))); 
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.D4, ModifierKeys.Control)), new KeyGesture(Key.D4, ModifierKeys.Control))); 
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.D5, ModifierKeys.Control)), new KeyGesture(Key.D5, ModifierKeys.Control)));
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.D6, ModifierKeys.Control)), new KeyGesture(Key.D6, ModifierKeys.Control)));
            this.InputBindings.Add(new InputBinding(new ProcessShortcutCommand(new KeyGesture(Key.D7, ModifierKeys.Control)), new KeyGesture(Key.D7, ModifierKeys.Control)));

            // Enable Windows Taskbar progress indication.
            if (this.TaskbarItemInfo == null)
            {
                this.TaskbarItemInfo = WindowsTaskbar.GetTaskBar();
            }

            // Setup the Right To Left Mode
            RightToLeftMode rightToLeft = (RightToLeftMode)userSettingService.GetUserSetting<int>(UserSettingConstants.RightToLeftUi);
            switch (rightToLeft)
            {
                case RightToLeftMode.EntireInterface:
                    if (Application.Current.MainWindow != null)
                    {
                        Application.Current.MainWindow.FlowDirection = FlowDirection.RightToLeft;
                    }
                    break;
                case RightToLeftMode.TextOnly:
                    FrameworkElement.FlowDirectionProperty.OverrideMetadata(typeof(TextBlock), new FrameworkPropertyMetadata(FlowDirection.RightToLeft));
                    FrameworkElement.FlowDirectionProperty.OverrideMetadata(typeof(TextBox), new FrameworkPropertyMetadata(FlowDirection.RightToLeft));
                    break;
            }
        }

        /// <summary>
        /// Check with the user before closing.
        /// </summary>
        /// <param name="e">
        /// The CancelEventArgs.
        /// </param>
        protected override void OnClosing(CancelEventArgs e)
        {
            IShellViewModel shellViewModel = this.DataContext as IShellViewModel;

            if (shellViewModel != null)
            {
                bool canClose = shellViewModel.CanClose();
                if (!canClose)
                {
                    e.Cancel = true;
                }
            }

            this.notifyIconService?.SetVisibility(false);

            base.OnClosing(e);
        }

        private void NotifyIconClick()
        {
            // The window is hidden while in the tray, so it must be shown again before restoring.
            this.Show();
            this.WindowState = WindowState.Normal;

            // Bit of a hack
            this.Topmost = true; 
            this.Topmost = false;
        }

        private void ShellViewStateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                this.notifyIconService?.SetVisibility(true);

                // Hiding the window removes it from Alt+Tab.
                this.Hide();
            }
            else if (this.WindowState == WindowState.Normal)
            {
                this.notifyIconService?.SetVisibility(false);
                this.ShowInTaskbar = true;
            }
        }

        private void ShellView_OnDrop(object sender, DragEventArgs e)
        {
            ((ShellViewModel)this.DataContext).FilesDroppedOnWindow(e);
        }
    }
}
