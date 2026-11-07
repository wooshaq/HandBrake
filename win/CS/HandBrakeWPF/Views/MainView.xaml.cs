// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainView.xaml.cs" company="HandBrake Project (http://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   Interaction logic for MainView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HandBrakeWPF.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;

    using HandBrakeWPF.ViewModels;
    using HandBrakeWPF.ViewModels.Interfaces;

    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainView"/> class.
        /// </summary>
        public MainView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Add to Queue button context menu handling.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void AddToQueue_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // If we've clicked the dropdown part of the button, display the context menu below the button.
            Button button = (sender as Button);
            if (button != null)
            {
                HitTestResult result = VisualTreeHelper.HitTest(button, e.GetPosition(button));
                FrameworkElement element = result.VisualHit as FrameworkElement;
                if (element != null)
                {
                    if (element.Name == "dropdown" || element.Name == "dropdownArrow")
                    {
                        button.ContextMenu.IsEnabled = true;
                        button.ContextMenu.PlacementTarget = button;
                        button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                        button.ContextMenu.IsOpen = true;
                        return;
                    }
                }
            }

            // Otherwise assume it's a main area click and add to queue.
            ((IMainViewModel)this.DataContext).AddToQueueWithErrorHandling();
        }

        private void PresetHeader_OnClick(object sender, RoutedEventArgs e)
        {
            // Collapsible preset section in the right-hand column: click to expand, click again to collapse.
            ((MainViewModel)this.DataContext).TogglePresetPane();
        }

        private void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl && e.AddedItems.Count > 0)
            {
                TabItem tab = e.AddedItems[0] as TabItem;
                if (tab != null && Properties.Resources.MainView_SummaryTab.Equals(tab.Header))
                {
                    ((MainViewModel)this.DataContext).SummaryViewModel.UpdateDisplayedInfo();
                }

                this.tabControl.Focus();
            }
        }

        private void InfoButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If we've clicked the dropdown part of the button, display the context menu below the button.
            Button button = (sender as Button);
            if (button != null)
            {
                HitTestResult result = VisualTreeHelper.HitTest(button, e.GetPosition(button));
                FrameworkElement element = result.VisualHit as FrameworkElement;
                if (element != null)
                {
                    button.ContextMenu.IsEnabled = true;
                    button.ContextMenu.PlacementTarget = button;
                    button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    button.ContextMenu.IsOpen = true;
                }
            }
        }

        private void Titles_OnDropDownClosed(object sender, EventArgs e)
        {
            this.Titles.DisplayMemberPath = "ItemDisplayTextClosed";
        }

        private void Titles_OnDropDownOpened(object sender, EventArgs e)
        {
            this.Titles.DisplayMemberPath = "ItemDisplayText";
        }

        private void AddToQueueBtn_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                ((IMainViewModel)this.DataContext).AddToQueueWithErrorHandling();
            }
            else if (e.Key == Key.Down)
            {
                this.OpenButtonMenu(sender as Button);
                e.Handled = true;
            }
        }

        private void InfoButton_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space || e.Key == Key.Enter || e.Key == Key.Down)
            {
                this.OpenButtonMenu(sender as Button);
                e.Handled = true;
            }
        }

        private void OpenButtonMenu(Button button)
        {
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.IsEnabled = true;
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
