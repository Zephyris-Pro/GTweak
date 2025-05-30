﻿using GTweak.Utilities.Configuration;
using GTweak.Utilities.Controls;
using GTweak.Utilities.Helpers.Animation;
using GTweak.Windows;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace GTweak
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            BtnNotification.StateNA = SettingsRepository.IsViewNotification;
            BtnUpdate.StateNA = SettingsRepository.IsUpdateCheckRequired;
            BtnTopMost.StateNA = Topmost = SettingsRepository.IsTopMost;
            BtnSoundNtn.IsChecked = SettingsRepository.IsPlayingSound;
            SliderVolume.Value = SettingsRepository.Volume;
            LanguageSelectionMenu.SelectedIndex = GetSelectedIndex(SettingsRepository.Language, "en", SettingsRepository.AvailableLangs);
            ThemeSelectionMenu.SelectedIndex = GetSelectedIndex(SettingsRepository.Theme, "Dark", SettingsRepository.AvailableThemes);

            App.ImportTweaksUpdate += delegate { BtnMore.IsChecked = true; };
            App.ThemeChanged += delegate { Close(); new RebootWindow().ShowDialog(); };
        }

        private int GetSelectedIndex(string value, string defaultValue, params string[] listing)
        {
            int index = Array.IndexOf(listing, value);
            return index >= 0 ? index : Array.IndexOf(listing, defaultValue);
        }

        #region Button Title/Animation Window
        private void ButtonHelp_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => Process.Start(new ProcessStartInfo("https://github.com/Greedeks/GTweak/issues/new/choose") { UseShellExecute = true });

        private void SettingsMenuAnimation()
        {
            Dispatcher.Invoke(() =>
            {
                Storyboard storyboard = new Storyboard();

                DoubleAnimation rotateAnimation = new DoubleAnimation()
                {
                    From = 0.0,
                    To = 360,
                    EasingFunction = new QuadraticEase(),
                    SpeedRatio = 2.0,
                    Duration = TimeSpan.FromSeconds(1)
                };

                DoubleAnimation widthAnimation = new DoubleAnimation()
                {
                    From = SettingsMenu.Width != 400 ? 0 : 400,
                    To = SettingsMenu.Width != 400 ? 400 : 0,
                    EasingFunction = new QuadraticEase(),
                    SpeedRatio = 2.0,
                    Duration = TimeSpan.FromSeconds(1)
                };

                Timeline.SetDesiredFrameRate(rotateAnimation, 240);
                Timeline.SetDesiredFrameRate(widthAnimation, 240);

                Storyboard.SetTarget(rotateAnimation, ImageSettings);
                Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));

                storyboard.Children.Add(rotateAnimation);
                storyboard.Begin();
                SettingsMenu.BeginAnimation(WidthProperty, widthAnimation);
            });
        }

        private void SettingsMenu_QueryCursor(object sender, QueryCursorEventArgs e)
        {
            if (SettingsMenu.Width == 400)
                SettingsMenuAnimation();
        }

        private void ButtonSettings_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SettingsMenu.Width == 0 || SettingsMenu.Width == 400)
                SettingsMenuAnimation();
        }

        private void ButtonMinimized_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => WindowState = WindowState.Minimized;

        private void ButtonExit_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => Close();

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            BeginAnimation(OpacityProperty, FadeAnimation.FadeTo(0.1, () => { Close(); }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BeginAnimation(OpacityProperty, FadeAnimation.FadeIn(1, 0.3,
                async () =>
                {
                    if (SystemDiagnostics.IsNeedUpdate && SettingsRepository.IsUpdateCheckRequired)
                    {
                        await Task.Delay(500);
                        new UpdateWindow().ShowDialog();
                    }
                }));
            new TypewriterAnimation(UtilityTitle.Text, UtilityTitle, TimeSpan.FromSeconds(0.4));
        }
        #endregion

        #region Settings Menu
        private void BtnNotification_ChangedState(object sender, EventArgs e) => SettingsRepository.IsViewNotification = !BtnNotification.State;

        private void BtnUpdate_ChangedState(object sender, EventArgs e) => SettingsRepository.IsUpdateCheckRequired = !BtnUpdate.State;

        private void BtnTopMost_ChangedState(object sender, EventArgs e)
        {
            SettingsRepository.IsTopMost = !BtnTopMost.State;
            Topmost = !BtnTopMost.State;
        }

        private void BtnSoundNtn_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => SettingsRepository.IsPlayingSound = (bool)!BtnSoundNtn.IsChecked;

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SliderVolume.Value = SliderVolume.Value == 0 ? 1 : SliderVolume.Value;
            SettingsRepository.Volume = (int)SliderVolume.Value;
            SettingsRepository.waveOutSetVolume(IntPtr.Zero, ((uint)(double)(ushort.MaxValue / 100 * SliderVolume.Value) & 0x0000ffff) | ((uint)(double)(ushort.MaxValue / 100 * SliderVolume.Value) << 16));
        }

        private void LanguageSelectionMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedLang = SettingsRepository.AvailableLangs.ElementAtOrDefault(LanguageSelectionMenu.SelectedIndex) ?? SettingsRepository.AvailableLangs.Last();
            SettingsRepository.Language = selectedLang;
            App.Language = selectedLang;
        }

        private void ThemeSelectionMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedTheme = SettingsRepository.AvailableThemes.ElementAtOrDefault(ThemeSelectionMenu.SelectedIndex) ?? SettingsRepository.AvailableThemes.Last();
            SettingsRepository.Theme = selectedTheme;
            App.Theme = selectedTheme;
        }

        private void BtnExport_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => SettingsRepository.SaveFileConfig();

        private void BtnImport_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => SettingsRepository.OpenFileConfig();

        private void BtnDelete_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => SettingsRepository.SelfRemoval();

        private void BtnContats_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo(((Image)sender).Uid switch
            {
                "git" => "https://github.com/Greedeks",
                "tg" => "https://t.me/Greedeks",
                _ => "https://steamcommunity.com/id/greedeks/"
            })
            { UseShellExecute = true });
        }
        #endregion
    }
}

