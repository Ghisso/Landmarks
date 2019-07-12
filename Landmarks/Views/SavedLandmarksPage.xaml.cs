using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Linq;

namespace Landmarks.Views
{
    public partial class SavedLandmarksPage : ContentPage
    {
        public List<string> files { get; set; }


        public SavedLandmarksPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            files = Directory.EnumerateFiles(Path.Combine(FileSystem.AppDataDirectory, "SavedImages")).ToList();
            //ViewLandmarks.ItemsSource = files;
        }
    }
}
