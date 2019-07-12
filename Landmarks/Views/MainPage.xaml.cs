using System.Collections.Generic;
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
using System.IO;
using Landmarks.Models;
using Landmarks.Services;
using System;

namespace Landmarks.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(true)]
    public partial class MainPage : ContentPage
    {
        byte[] imageBytes;
        string filePath;
        CancellationTokenSource cts;
        HttpClient client = HttpClientFactory.Create();
        IEnumerable<Locale> locales;
        Landmark landmark = new Landmark();


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


        byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
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
            var stopwatch = new Stopwatch();

            // Cancel speech if one was ongoing
            CancelSpeech();

            landmark = await CheckAndRequestPermissions() ? 
                        await TakePhotoAndAnalyze(stopwatch) : 
                        await FetchPhotoFromInternetAndAnalyze(stopwatch);

            if (landmark == null)
                return;
            
            await UpdateUIAfterAnalysis(stopwatch);
        }


        async void ButtonChooseImageClicked(object sender, System.EventArgs e)
        {
            var stopwatch = new Stopwatch();

            // Cancel speech if one was ongoing
            CancelSpeech();

            bool permission = await CheckAndRequestPermissions();
            if (!permission)
                return;

            landmark = await ChoosePhotoAndAnalyze(stopwatch);

            if (landmark == null)
                return;

            await UpdateUIAfterAnalysis(stopwatch);
        }


        async void ButtonSaveImageClicked(object sender, System.EventArgs e)
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "SavedImages", landmark.Name.Replace(" ", string.Empty) + ".jpg");
            if (!Directory.Exists(Path.Combine(FileSystem.AppDataDirectory, "SavedImages")))
            {
                Directory.CreateDirectory(Path.Combine(FileSystem.AppDataDirectory, "SavedImages"));
            }

            File.WriteAllBytes(path, imageBytes);
            await DisplayAlert("Landmark saved", "This landmark has been saved", "OK");
        }


        void ButtonDeleteImagesClicked(object sender, System.EventArgs e)
        {
        }


        private async Task<Landmark> TakePhotoAndAnalyze(Stopwatch stopwatch)
        {
            ResetUI();

            // Added image size reductions because custom vision API only accepts images max 4MB
            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                Directory = "Landmarks",
                Name = "test.jpg",
                PhotoSize = PhotoSize.Large,
                CompressionQuality = 92,
                SaveMetaData = true
            });

            
            if (file == null)
                return null;
            filePath = file.Path;
            imageBytes = ReadFully(file.GetStream());
            ImageTaken.Source = ImageSource.FromStream(() =>
            {
                var stream = file.GetStream();
                return stream;
            });
            ActivityIndicator.IsRunning = true;

            stopwatch.Start();
            Stream streamToAnalyze = file.GetStream();
            landmark = await AnalyzeImage(landmark, streamToAnalyze);
            stopwatch.Stop();
            return landmark;
        }


        private async Task<Landmark> FetchPhotoFromInternetAndAnalyze(Stopwatch stopwatch)
        {
            ResetUI();

            var response = await client.GetAsync("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRR-xWsHUM-W2j4dorc-yafy35Z5xPgRbbnf1pEG_MbGgxE2YSAzA");

            // Need the 2 here or else the image won't appear in the interface (but analysis will be performed) ...
            var fileStream = await response.Content.ReadAsStreamAsync();
            var imageStream = await response.Content.ReadAsStreamAsync();

            imageBytes = ReadFully(fileStream);
            ImageTaken.Source = ImageSource.FromStream(() =>
            {
                return imageStream;
            });
            ActivityIndicator.IsRunning = true;


            stopwatch.Start();
            landmark = await AnalyzeImage(landmark, fileStream);
            stopwatch.Stop();
            return landmark;
        }


        private async Task<Landmark> ChoosePhotoAndAnalyze(Stopwatch stopwatch)
        {
            ResetUI();

            // Added image size reductions because custom vision API only accepts images max 4MB
            var file = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions()
            {
                PhotoSize = PhotoSize.Large,
                CompressionQuality = 92
            });

            if (file == null)
                return null;

            filePath = file.Path;

            imageBytes = ReadFully(file.GetStream());

            ImageTaken.Source = ImageSource.FromStream(() =>
            {
                var stream = file.GetStream();
                return stream;
            });
            ActivityIndicator.IsRunning = true;


            stopwatch.Start();
            Stream streamToAnalyze = file.GetStream();
            landmark = await AnalyzeImage(landmark, streamToAnalyze);
            stopwatch.Stop();
            return landmark;
        }


        private static async Task<Landmark> AnalyzeImage(Landmark landmark, Stream fileStream)
        {
            // Due to extracting this method out of the calling methods, the stream is disposed after
            // landmark = await LandmarkFinder.AnalyzeImageVisionAPI(fileStream); so must first copy it
            // to use it for the next call to CustomVisionAPI
            MemoryStream ms = new MemoryStream();
            fileStream.CopyTo(ms);
            fileStream.Position = 0;
            landmark = await LandmarkFinder.AnalyzeImageVisionAPI(fileStream);
            if (landmark.Name == "No landmark found")
            {
                landmark = await LandmarkFinder.AnalyzeImageCustomVisionAPI(ms.ToArray());
                if (landmark.Confidence < 0.80)
                {
                    landmark.Name = "Unknown";
                    landmark.Description = "No landmark found";
                    return landmark;
                }
            }
            var desc = await new WikipediaQuery(landmark.Name.Replace(" ", "_")).PerformQuery();
            if (desc == null || desc.Length == 0)
                landmark.Description = landmark.Name;
            else
                landmark.Description = desc;
            return landmark;
        }


        private async Task UpdateUIAfterAnalysis(Stopwatch stopwatch)
        {
            EntryLandmarkName.Text = landmark.Name;
            EditorLandmarkDescription.Text = landmark.Description;
            EntryConfidence.Text = landmark.Confidence.ToString();
            EntryTime.Text = stopwatch.ElapsedMilliseconds.ToString();
            ActivityIndicator.IsRunning = false;
            EditorPath.Text = filePath;

            if (Preferences.Get("Text2Speech", true))
            {
                var settings = new SpeechOptions()
                {
                    // float cast here seems necessary on Android to prevent Cast exception...
                    Volume = (float)Preferences.Get("Volume", 100) / 100,
                    Pitch = (float)Preferences.Get("Pitch", 100) / 100,
                    Locale = locales.ElementAt(Preferences.Get("Locale", 0))
                };
                cts = new CancellationTokenSource();
                await TextToSpeech.SpeakAsync(landmark.Description, settings, cts.Token);
            }
        }


        private void ResetUI()
        {
            EntryLandmarkName.Text = "";
            EditorLandmarkDescription.Text = "";
            EntryTime.Text = "";
            EntryConfidence.Text = "";
            ImageTaken.Source = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSV8WfsQJrn2PMrm-YWig7khnLpSwSyjdrnsYzHOPZlJqirdS-NSw";

            EditorPath.Text = filePath;
        }
    }
}
