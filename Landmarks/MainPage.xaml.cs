﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.Threading;

namespace Landmarks
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(true)]
    public partial class MainPage : ContentPage
    {
        CancellationTokenSource cts;
        HttpClient client = new HttpClient();
        IEnumerable<Locale> locales;


        public MainPage()
        {
            InitializeComponent();
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Initializing locales the first time and order them by name so as not to do it every time
            if (locales == null)
            {
                locales = await TextToSpeech.GetLocalesAsync();
                locales = locales.OrderBy((arg) => arg.Name);
            }
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            CancelSpeech();
        }


        public void CancelSpeech()
        {
            if (cts == null)
                return;
            if (cts?.IsCancellationRequested ?? false)
                return;

            cts.Cancel();
        }


        async Task<bool> CheckAndRequestPermissions()
        {
            var cameraStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Camera);
            var storageStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Storage);

            if (cameraStatus != PermissionStatus.Granted || storageStatus != PermissionStatus.Granted)
            {
                var results = await CrossPermissions.Current.RequestPermissionsAsync(new[] { Permission.Camera, Permission.Storage });
                cameraStatus = results[Permission.Camera];
                storageStatus = results[Permission.Storage];
            }
            return cameraStatus == PermissionStatus.Granted && storageStatus == PermissionStatus.Granted;
        }


        async void ButtonTakeImageClicked(object sender, System.EventArgs e)
        {
            Landmark landmark;
            var stopwatch = new Stopwatch();

            // Cancel speech if one was ongoing
            CancelSpeech();

            if (await CheckAndRequestPermissions())
            {
                landmark = await TakePhotoAndAnalyze(stopwatch);
            }
            else
            {
                landmark = await FetchPhotoFromInternetAndAnalyze(stopwatch);
            }

            await UpdateUIAfterAnalysis(landmark, stopwatch);
        }


        private async Task<Landmark> TakePhotoAndAnalyze(Stopwatch stopwatch)
        {
            Landmark landmark;

            // Added image size reductions because custom vision API only accepts images max 4MB
            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                Directory = "Landmarks",
                Name = "test.jpg",
                PhotoSize = PhotoSize.Large,
                CompressionQuality = 92,
                SaveToAlbum = true
            });

            ImageTaken.Source = ImageSource.FromStream(() =>
            {
                var stream = file.GetStream();
                return stream;
            });
            stopwatch.Start();
            landmark = await LandmarkFinder.AnalyzeImage(file.GetStream());
            landmark.Description = $"Description of {landmark.Name}";
            stopwatch.Stop();
            return landmark;
        }


        private async Task<Landmark> FetchPhotoFromInternetAndAnalyze(Stopwatch stopwatch)
        {
            Landmark landmark;

            var response = await client.GetAsync("https://s27363.pcdn.co/wp-content/uploads/2017/03/Himeji-Castle-Japan-1163x775.jpg.optimal.jpg");

            // Need the 2 here or else the image won't appear in the interface (but analysis will be performed) ...
            var fileStream = await response.Content.ReadAsStreamAsync();
            var imageStream = await response.Content.ReadAsStreamAsync();
            ImageTaken.Source = ImageSource.FromStream(() =>
            {
                return imageStream;
            });
            stopwatch.Start();
            landmark = await LandmarkFinder.AnalyzeImage(fileStream);
            landmark.Description = string.Concat(Enumerable.Repeat($"Description of {landmark.Name} ", 5));
            stopwatch.Stop();
            return landmark;
        }


        private async Task UpdateUIAfterAnalysis(Landmark landmark, Stopwatch stopwatch)
        {
            EntryLandmarkName.Text = landmark.Name;
            EditorLandmarkDescription.Text = landmark.Description;
            EntryTime.Text = stopwatch.ElapsedMilliseconds.ToString();
            if (Preferences.Get("Text2Speech", true))
            {
                var settings = new SpeechOptions()
                {
                    // float cast here seems necessary on Android to prevent Cast exception...
                    Volume = (float)Preferences.Get("Volume", 100) / 100,
                    Pitch = 1.0f,
                    Locale = locales.ElementAt(Preferences.Get("Locale", 0))
                };
                cts = new CancellationTokenSource();
                await TextToSpeech.SpeakAsync(landmark.Description, settings, cts.Token);
            }
        }
    }
}
