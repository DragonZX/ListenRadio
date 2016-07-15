using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace ListenRadio
{
    [Service]
    [IntentFilter(new [] {ActionPlay, ActionPause, ActionStop })]
    class StreamService : Service, AudioManager.IOnAudioFocusChangeListener
    {
        public const string ActionPlay = "com.action.PLAY";
        public const string ActionPause = "com.action.PAUSE";
        public const string ActionStop = "com.action.STOP";

        private string _radio;

        private MediaPlayer player;
        private AudioManager audioManager;
        private WifiManager wifiManager;
        private WifiManager.WifiLock wifiLock;
        private bool paused;

        private const int NotificationId = 1;

        public override void OnCreate()
        {
            base.OnCreate();
            audioManager = (AudioManager)GetSystemService(AudioService);
            wifiManager = (WifiManager)GetSystemService(WifiService);
            _radio = GetString(Resource.String.RadioUrl);
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {

            switch (intent.Action)
            {
                case ActionPlay: Play(); break;
                case ActionStop: Stop(); break;
                case ActionPause: Pause(); break;
            }

            return StartCommandResult.Sticky;
        }

        private void IntializePlayer()
        {
            player = new MediaPlayer();

            player.SetAudioStreamType(Stream.Music);
        
            player.SetWakeMode(ApplicationContext, WakeLockFlags.Partial);

            player.Prepared += (sender, args) => player.Start();

            player.Completion += (sender, args) => Stop();

            player.Error += (sender, args) =>
            {
                //playback error
                Console.WriteLine("Error in playback resetting: " + args.What);
                Stop();
            };
        }

        private async void Play()
        {

            if (paused && player != null)
            {
                paused = false;
                player.Start();
                StartForeground();
                return;
            }

            if (player == null)
            {
                IntializePlayer();
            }

            if (player.IsPlaying)
                return;

            try
            {
                await player.SetDataSourceAsync(ApplicationContext, Android.Net.Uri.Parse(_radio));

                var focusResult = audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
                if (focusResult != AudioFocusRequest.Granted)
                {
                    //could not get audio focus
                    Console.WriteLine("Could not get audio focus");
                }

                player.PrepareAsync();
                AquireWifiLock();
                StartForeground();
            }
            catch (Exception ex)
            {
                //unable to start playback log error
                Console.WriteLine("Unable to start playback: " + ex);
            }
        }

        private void StartForeground()
        {
            //Intent for showing notification
            var pendingIntent = PendingIntent.GetActivity(ApplicationContext, 0,
                            new Intent(ApplicationContext, typeof(MainActivity)),
                            PendingIntentFlags.UpdateCurrent);

            //Custom notification and build it
            var builder = new Notification.Builder(this)
            .SetContentText("Radio is playing")
            .SetContentTitle("Listen Radio")
            .SetContentIntent(pendingIntent)
            .SetSmallIcon(Resource.Drawable.Icon)
            .SetOngoing(true);
            Notification notification = builder.Build();

            //Init notification manager and show notification
            NotificationManager notificationManager =
                GetSystemService(Context.NotificationService) as NotificationManager;
            notificationManager.Notify(NotificationId, notification);

        }

        //Pause, can use it if you want
        private void Pause()
        {
            if (player == null)
                return;

            if (player.IsPlaying)
                player.Pause();

            StopForeground(true);
            paused = true;
        }

        //Stop
        private void Stop()
        {
            if (player == null)
                return;

            if (player.IsPlaying)
                player.Stop();

            player.Reset();
            paused = false;
            StopForeground(true);
            ReleaseWifiLock();
        }

        //Wifi lockers, when device go to sleep still play streaming
        private void AquireWifiLock()
        {
            if (wifiLock == null)
            {
                wifiLock = wifiManager.CreateWifiLock(WifiMode.Full, "xamarin_wifi_lock");
            }
            wifiLock.Acquire();
        }

        private void ReleaseWifiLock()
        {
            if (wifiLock == null)
                return;

            wifiLock.Release();
            wifiLock = null;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (player != null)
            {
                player.Release();
                player = null;
            }
        }


        /// <summary>
        /// Focus change, when user change application
        /// </summary>
        /// <param name="focusChange">Check app audio focus</param>
        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    if (player == null)
                        IntializePlayer();

                    if (!player.IsPlaying)
                    {
                        player.Start();
                        paused = false;
                    }

                    player.SetVolume(1.0f, 1.0f);//Turn it up!
                    break;
                case AudioFocus.Loss:
                    //We have lost focus stop!
                    Stop();
                    break;
                case AudioFocus.LossTransient:
                    Pause();
                    break;
                case AudioFocus.LossTransientCanDuck:
                    if (player.IsPlaying)
                        player.SetVolume(.1f, .1f);//turn it down!
                    break;

            }
        }
    }
}