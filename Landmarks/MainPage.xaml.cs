using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Xamarin.Forms;
using Xamarin.Essentials;
using Plugin.InputKit;
using System.Threading;

namespace Landmarks
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(true)]
    public partial class MainPage : ContentPage
    {
        CancellationTokenSource cts;
        private static readonly HttpClient client = new HttpClient();
        private static readonly string url = "https://japaneast.api.cognitive.microsoft.com/customvision/v3.0/Prediction/76f0d893-7cda-4625-90ec-8470bd025024/classify/iterations/TestIteration/image";
        IEnumerable<Locale> locales;

        public MainPage()
        {
            InitializeComponent();
            client.DefaultRequestHeaders.Add("Prediction-Key", "c7b31743df8a4b599690ec34bf7dd568");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (locales == null)
            {
                locales = await TextToSpeech.GetLocalesAsync();
            }
        }

        public async Task<List<Locale>> GetLocales()
        {
            var l = await TextToSpeech.GetLocalesAsync();
            return l.ToList();
        }

        public void CancelSpeech()
        {
            if (cts == null)
                return;
            if (cts?.IsCancellationRequested ?? false)
                return;

            cts.Cancel();
        }


        public static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }


        public static async Task<Landmark> AnalyzeImage(Stream stream)
        {
            var imageArray = ReadFully(stream);
            var response = await client.PostAsync(url, new ByteArrayContent(imageArray));
            var replyBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Result>(replyBody);
            var landmark = new Landmark()
            {
                Name = result.Predictions[0].TagName,
                Confidence = result.Predictions[0].Probability
            };
            return landmark;
        }

        async void ButtonTakeImageClicked(object sender, System.EventArgs e)
        {
            CancelSpeech();
            var stopwatch = new Stopwatch();
            Landmark landmark;
            var cameraStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Camera);
            var storageStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Storage);

            //if (cameraStatus != PermissionStatus.Granted || storageStatus != PermissionStatus.Granted)
            //{
            //    var results = await CrossPermissions.Current.RequestPermissionsAsync(new[] { Permission.Camera, Permission.Storage });
            //    cameraStatus = results[Permission.Camera];
            //    storageStatus = results[Permission.Storage];
            //}

            if (cameraStatus == PermissionStatus.Granted && storageStatus == PermissionStatus.Granted)
            {
                var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                {
                    Directory = "Sample",
                    Name = "test.jpg"
                });

                ImageTaken.Source = ImageSource.FromStream(() =>
                {
                    var stream = file.GetStream();
                    return stream;
                });
                stopwatch.Start();
                landmark = await AnalyzeImage(file.GetStream());
                landmark.Description = $"Description of {landmark.Name}";
                stopwatch.Stop();
            }
            else
            {
                var response = await client.GetAsync("https://s27363.pcdn.co/wp-content/uploads/2017/03/Himeji-Castle-Japan-1163x775.jpg.optimal.jpg");
                var fileStream = await response.Content.ReadAsStreamAsync();
                fileStream.Position = 0;
                var stream = await response.Content.ReadAsStreamAsync();
                ImageTaken.Source = ImageSource.FromStream(() =>
                {
                    return stream;
                });
                stopwatch.Start();
                landmark = await AnalyzeImage(fileStream);
                landmark.Description = string.Concat(Enumerable.Repeat($"Description of {landmark.Name} ", 5));
                stopwatch.Stop();
            }

            EntryLandmarkName.Text = landmark.Name;
            EditorLandmarkDescription.Text = landmark.Description;
            EntryTime.Text = stopwatch.ElapsedMilliseconds.ToString();
            if (CheckBoxText2Speech.IsChecked)
            {

                var locale = locales.Last();
                var settings = new SpeechOptions()
                {
                    Volume = Preferences.Get("Volume", 1.0f)/100,
                    Pitch = 1.0f,
                    Locale = locales.ElementAt(Preferences.Get("Locale", 0))
                };
                cts = new CancellationTokenSource();
                await TextToSpeech.SpeakAsync(landmark.Description, settings, cts.Token);
            }
        }
    }
}
