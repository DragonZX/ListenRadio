using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace ListenRadio
{
    [Activity(Label = "ListenRadio", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int count = 1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button playButton = FindViewById<Button>(Resource.Id.PlayButton);
            Button stopButton = FindViewById<Button>(Resource.Id.StopButton);
            Button pauseButton = FindViewById<Button>(Resource.Id.StopButton);

            playButton.Click += delegate { SendAudioCommand(StreamService.ActionPlay); };
            stopButton.Click += delegate { SendAudioCommand(StreamService.ActionStop);  };
        }

        private void SendAudioCommand(string action)
        {
            var intent = new Intent(action);
            StartService(intent);
        }

        protected override void OnDestroy()
        {
            var intent = new Intent(StreamService.ActionStop);
            StartService(intent);
            base.OnDestroy();
        }
    }
}

