using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Landmarks
{
    public partial class SettingsPage : ContentPage
    {

        IEnumerable<Locale> locales;


        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (locales == null)
            {
                locales = await TextToSpeech.GetLocalesAsync();
                PickerLocales.ItemsSource = locales.ToList();
                PickerLocales.ItemDisplayBinding = new Binding("Name");
            }
            PickerLocales.SelectedIndex = Preferences.Get("Locale", 0);
            SliderVolume.Value = Preferences.Get("Volume", 100);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Preferences.Set("Volume", (int)SliderVolume.Value);
            Preferences.Set("Locale", PickerLocales.SelectedIndex);
        }
    }
}
