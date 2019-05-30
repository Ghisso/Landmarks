using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Landmarks.Pages
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
                PickerLocales.ItemsSource = locales.OrderBy((arg) => arg.Name).ToList();
                PickerLocales.ItemDisplayBinding = new Binding("Name");
                PickerLocales.Title = "Choose a language";
            }
            SliderVolume.Value = Preferences.Get("Volume", 100);
            PickerLocales.SelectedIndex = Preferences.Get("Locale", 0);
            SwitchText2Speech.On = Preferences.Get("Text2Speech", true);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Preferences.Set("Volume", (int)SliderVolume.Value);
            Preferences.Set("Locale", PickerLocales.SelectedIndex);
            Preferences.Set("Text2Speech", SwitchText2Speech.On);
        }
    }
}
