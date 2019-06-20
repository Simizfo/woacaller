﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Calls;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WoADialer.Model;

namespace WoADialer.Pages
{
    public sealed partial class InCallUI : Page
    {
        private PhoneLine currentPhoneLine;
        private Timer callLengthCounter;
        private DateTime? callStartTime;

        public InCallUI()
        {
            this.InitializeComponent();
            Task<PhoneLine> getDefaultLineTask = GetDefaultPhoneLineAsync();

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            PhoneCallManager.CallStateChanged += PhoneCallManager_CallStateChanged;

            getDefaultLineTask.Wait(500);
            if (getDefaultLineTask.IsCompletedSuccessfully)
            {
                currentPhoneLine = getDefaultLineTask.Result;
            }
        }

        private async void TimerCallback(object state)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => callTimerText.Text = (DateTime.Now - callStartTime)?.ToString("mm\\:ss"));
        }

        private void StartTimer()
        {
            callStartTime = DateTime.Now;
            callLengthCounter = new Timer(TimerCallback, null, 0, 1000);
        }

        private void StopTimer()
        {
            callLengthCounter.Dispose();
            callStartTime = null;
        }

        private void PhoneCallManager_CallStateChanged(object sender, object e)
        {
            if (!callStartTime.HasValue && PhoneCallManager.IsCallActive)
            {
                StartTimer();
            }
            else if (callStartTime.HasValue && !PhoneCallManager.IsCallActive)
            {
                StopTimer();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            switch (e.Parameter)
            {
                case CallInfo info:
                    if (info.IsActive)
                    {
                        StartTimer();
                    }
                    callerNumberText.Text = info.Number.ToString("nice");
                    break;
            }
            getHistory();
            base.OnNavigatedTo(e);
        }

        private async void getHistory()
        {
            try
            {
                PhoneCallHistoryStore a = await PhoneCallHistoryManager.RequestStoreAsync(PhoneCallHistoryStoreAccessType.AllEntriesReadWrite);
                IReadOnlyList<PhoneCallHistoryEntry> list = await a.GetEntryReader().ReadBatchAsync();
                foreach (PhoneCallHistoryEntry entry in list)
                {
                    Debug.WriteLine("Entry ------");
                    Debug.WriteLine("Address: " + entry.Address);
                    Debug.WriteLine("Id: " + entry.Id);
                    Debug.WriteLine("SourceId: " + entry.SourceId);
                    Debug.WriteLine("Ringing: " + entry.IsRinging);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("!!!!!!!!");
                Debug.WriteLine(e.Message);
                Debug.WriteLine("!!!!!!!!");
            }
        }

        private async Task<PhoneLine> GetDefaultPhoneLineAsync()
        {
            PhoneCallStore phoneCallStore = await PhoneCallManager.RequestStoreAsync();
            Guid lineId = await phoneCallStore.GetDefaultLineAsync();
            return await PhoneLine.FromIdAsync(lineId);
        }


        private async void CloseCallButton_Click(object sender, RoutedEventArgs e)
        {
            //create consoleapp helper and restart data service
            string closeCallCommand = "woadialerhelper:closecall";
            Uri uri = new Uri(closeCallCommand);
            var result = await Windows.System.Launcher.LaunchUriAsync(uri);
            //go back to the previous page or close the app if the call was received
            Frame.Navigate(typeof(MainPage));
        }
    }
}
