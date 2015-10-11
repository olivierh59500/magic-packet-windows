﻿using Coding4Fun.Toolkit.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MagicPacketSender
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {

        private bool isSendButtonEnabled = false;
        public bool IsSendButtonEnabled
        {
            get { return isSendButtonEnabled; }
            set
            {
                isSendButtonEnabled = value;
                OnPropertyChanged("IsSendButtonEnabled");
            }
        }

        private ObservableCollection<RequestInfo> recentRequests = new ObservableCollection<RequestInfo>();
        public ObservableCollection<RequestInfo> RecentRequests
        {
            get { return recentRequests; }
            set
            {
                recentRequests = value;
                OnPropertyChanged("RecentRequests");
            }
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = this;

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (RecentRequests.Count == 0)
            {
                var requests = await FileUtils.GetRequestInfo();
                if (requests != null)
                {
                    foreach (var request in requests)
                    {
                        RecentRequests.Add(request);
                    }
                }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            HostName targetHost;
            try
            {
                targetHost = new HostName(HostnameBox.Text);
            }
            catch(ArgumentException ex)
            {
                MessageDialog errorDialog = new MessageDialog("You've entered an invalid hostname or IP address.\nNote that the hostname field should not contain port numbers.");
                errorDialog.Title = "Invalid Hostname or IP address";
                await errorDialog.ShowAsync();
                return;
            }
            uint port = Convert.ToUInt32(PortNumberBox.Text);
            string macAddress = MacAddressBox.Text;

            MagicPacketSender magicSender = new MagicPacketSender();
            bool success = await magicSender.SendMagicPacket(targetHost, port, macAddress);

            RequestInfo info = new RequestInfo
            (
                HostnameBox.Text,
                Convert.ToUInt32(PortNumberBox.Text),
                MacAddressBox.Text
            );

            if (!RecentRequests.Contains(info))
            {
                if (RecentRequests.Count > 20)
                {
                    for (int i = 20; i <= RecentRequests.Count; i++)
                    {
                        RecentRequests.RemoveAt(i);
                    }
                }
                RecentRequests.Insert(0, info);
                await FileUtils.SaveRequestInfo(RecentRequests.ToList());
            }
            if (success)
            {
                ToastPrompt toast = new ToastPrompt();
                toast.Title = "Magic Packet";
                toast.Message = " Magic packet sent!";                
                toast.TextOrientation = Orientation.Horizontal;
                toast.MillisecondsUntilHidden = 3000;
                toast.Show();
            }
            else
            {
                ToastPrompt toast = new ToastPrompt();
                toast.Title = "Magic Packet";
                toast.Message = " Sending failed! =(";
                toast.TextOrientation = Orientation.Horizontal;
                toast.MillisecondsUntilHidden = 3000;
                toast.Show();
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
            {
                return;
            }

            RequestInfo info = e.AddedItems[0] as RequestInfo;
            if (info != null)
            {
                HostnameBox.Text = info.RequestHostName;
                PortNumberBox.Text = info.Port.ToString();
                MacAddressBox.Text = info.MacAddress;
            }
            UpdateSendButtonEnabled();
            (sender as ListView).SelectedItem = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void UpdateSendButtonEnabled()
        {
            if (!String.IsNullOrWhiteSpace(HostnameBox.Text)
                && !String.IsNullOrWhiteSpace(PortNumberBox.Text)
                && !String.IsNullOrWhiteSpace(MacAddressBox.Text))
            {
                IsSendButtonEnabled = true;
            }
            else
            {
                IsSendButtonEnabled = false;
            }
        }

        private void HostnameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSendButtonEnabled();
        }

        private void PortNumberBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSendButtonEnabled();
        }

        private void MacAddressBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSendButtonEnabled();
        }

        private void MenuFlyoutDelete_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var itemToRemove = (e.OriginalSource as MenuFlyoutItem).DataContext;
            RecentRequests.Remove(itemToRemove as RequestInfo);
        }        

        private void StackPanel_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {            
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);

            flyoutBase.ShowAt(senderElement);
        }
    }
}
