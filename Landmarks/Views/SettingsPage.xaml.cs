using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Landmarks.Views
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
            SliderPitch.Value = Preferences.Get("Pitch", 100);
            PickerLocales.SelectedIndex = Preferences.Get("Locale", 0);
            SwitchText2Speech.On = Preferences.Get("Text2Speech", false);
            SliderVolume.IsEnabled = SwitchText2Speech.On;
            SliderPitch.IsEnabled = SwitchText2Speech.On;
            PickerLocales.IsEnabled = SwitchText2Speech.On;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Preferences.Set("Volume", (int)SliderVolume.Value);
            Preferences.Set("Pitch", (int)SliderPitch.Value);
            Preferences.Set("Locale", PickerLocales.SelectedIndex);
            Preferences.Set("Text2Speech", SwitchText2Speech.On);
        }

        void Handle_OnSwitchText2SpeechChanged(object sender, Xamarin.Forms.ToggledEventArgs e)
        {
            SliderVolume.IsEnabled = SwitchText2Speech.On;
            SliderPitch.IsEnabled = SwitchText2Speech.On;
            PickerLocales.IsEnabled = SwitchText2Speech.On;
        }

        void Handle_VolumeValueChanged(object sender, Xamarin.Forms.ValueChangedEventArgs e)
        {
            LabelVolumeValue.Text = ((int)SliderVolume.Value).ToString();
        }

        void Handle_PitchValueChanged(object sender, Xamarin.Forms.ValueChangedEventArgs e)
        {
            LabelPitchValue.Text = ((int)SliderPitch.Value).ToString();
        }
    }
}
