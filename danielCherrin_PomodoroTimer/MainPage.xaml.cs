using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI;
using PomodoroSettingsLibrary;
using System.Collections.ObjectModel;
using Windows.Media.Playback;
using Windows.Media.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace danielCherrin_PomodoroTimer
{
    public sealed partial class MainPage : Page
    {
        //MAINPAGE TIMER
        TimeSpan pageTimer;
        int checkpoint = 0;

        //MAINPAGE TIMER TASK
        Task timerTask = null;
        bool counting = false;
        MediaPlayer alarmPlayer = new MediaPlayer();
        Task alarmTask = null;

        //MAIN PAGE SETTINGS
        List<int> timeSettingRange = Enumerable.Range(1, 60).ToList<int>();
        bool useDarkTheme;
        bool useDefaultSettings = true;
        DefaultSettings defaultSettings = new DefaultSettings();

        PresetSettings presetSettings = null;
        ObservableCollection<string> presetList = new ObservableCollection<string>();

        public MainPage()
        {
            this.InitializeComponent();
            ElementSoundPlayer.State = ElementSoundPlayerState.On;

            //Creates .db file if doesn't exist.
            SettingsDB.Initialize_DB();
            //Create .db Theme
            SettingsDB.Initialize_Theme();
            SettingsDB.DB_IfNotExistsCreate_USEDARKTHEME();
            useDarkTheme = SettingsDB.DB_Select_USEDARKTHEME();
            RefreshTheme();

            //Retrieve all presetNames 
            presetList.Add("default");
            foreach (string preset in SettingsDB.DB_Retrieve_PRESET_AllNames())
            presetList.Add(preset);

            StartUpSetPresetSettings();
            AssignTimerTaskOrReset();

            SetCmbbxFromSettings();
            UpdatePageTimerTextSync();
        }

        //http://joeljoseph.net/converting-hex-to-color-in-universal-windows-platform-uwp/
        public Color GetSolidColorBrush(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte a = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
            byte r = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
            byte g = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
            byte b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
            Color myBrush = Color.FromArgb(a, r, g, b);
            return myBrush;
        }

        public void RefreshTheme()
        {
            if(useDarkTheme)
            {
                (Application.Current.Resources["scb_BGContent"] as SolidColorBrush).Color = GetSolidColorBrush("#ff1a1a1a");
                (Application.Current.Resources["scb_BGPane"] as SolidColorBrush).Color = GetSolidColorBrush("#f1262626");
                (Application.Current.Resources["scb_FGbtn"] as SolidColorBrush).Color = GetSolidColorBrush("#fff1f1f1");
                (Application.Current.Resources["scb_FGtxt"] as SolidColorBrush).Color = GetSolidColorBrush("#fff1f1f1");
            }
            else
            {
                (Application.Current.Resources["scb_BGContent"] as SolidColorBrush).Color = GetSolidColorBrush("#fff1f1f1");
                (Application.Current.Resources["scb_BGPane"] as SolidColorBrush).Color = GetSolidColorBrush("#f1f1f1f1");
                (Application.Current.Resources["scb_FGbtn"] as SolidColorBrush).Color = GetSolidColorBrush("#ff1a1a1a");
                (Application.Current.Resources["scb_FGtxt"] as SolidColorBrush).Color = GetSolidColorBrush("#ff1a1a1a");
            }
        }

        public void AssignTimerTaskOrReset()
        {
            if (useDefaultSettings)
            {
                pageTimer = defaultSettings.GetSessionTime();
                timerTask = TimerAsync(defaultSettings);
            }
            else
            {
                pageTimer = presetSettings.GetSessionTime();
                timerTask = TimerAsync(presetSettings);
            }
        }

        public async Task FindAlarmFileAsync()
        {
            Windows.Storage.StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync(@"Assets");
            Windows.Storage.StorageFile file = await folder.GetFileAsync("gentle_morning_alarmFaded.wav");
            alarmPlayer.AutoPlay = false;
            alarmPlayer.Source = MediaSource.CreateFromStorageFile(file);
        }

        public void StopAlarm()
        {
            alarmPlayer.Source = null;
        }

        public void StartUpSetPresetSettings()
        {
            //Any presets? Set to first one else use default settings.
            if (presetList.Count <= 1)
            {
                useDefaultSettings = true;
                presetSettings = null;
            }
            else
            {
                useDefaultSettings = false;
                presetSettings = SettingsDB.DB_Select_PRESET(presetList[1]);
            }
        }

        public void SetCmbbxFromSettings()
        {
            if(useDefaultSettings)
            {
                cmbbx_sessionTime.SelectedItem = defaultSettings.GetSessionTime().Minutes;
                cmbbx_shortTime.SelectedItem = defaultSettings.GetShortBreakTime().Minutes;
                cmbbx_longTime.SelectedItem = defaultSettings.GetLongBreakTime().Minutes;
                cmbbx_presetSettings.SelectedItem = "default";
            }
            else
            {
                cmbbx_sessionTime.SelectedItem = presetSettings.GetSessionTime().Minutes;
                cmbbx_shortTime.SelectedItem = presetSettings.GetShortBreakTime().Minutes;
                cmbbx_longTime.SelectedItem = presetSettings.GetLongBreakTime().Minutes;
                cmbbx_presetSettings.SelectedItem = presetSettings.GetPresetName();
            }
        }

        public async Task TimerAsync(DefaultSettings aSettings)
        {
            while (true)
            {
                 await TickSecondPageTimerAsync(aSettings);
                 UpdatePageTimerTextSync();
            }
        }
        public async Task TickSecondPageTimerAsync(DefaultSettings aSettings)
        {
            if(counting)
            {
                pageTimer = pageTimer.Subtract(new TimeSpan(0, 0, 0, 0, 100));
            }
            
            //does the page timer reach 00:00?
            if(pageTimer <= new TimeSpan(0,0,0))
            {
                alarmTask = FindAlarmFileAsync();
                alarmPlayer.Play();
                UpdateCheckpointAdd();
                UpdateCheckpointSymbolSync();
                CheckpointTimeCheckerSync();
                counting = false;
                UpdatePageButtonTextSync();
                
            }
            await Task.Delay(100);
        }
        void UpdateCheckpointAdd()
        {
            if (checkpoint == 0 || checkpoint == 2 || checkpoint == 4)//A session has finished
            {
                checkpoint++;
            }
            else if (checkpoint == 1 || checkpoint == 3 || checkpoint == 5)//A short break has finished
            {
                checkpoint++;
            }
            else if (checkpoint == 6)//The session before a long break has finished
            {
                checkpoint++;
            }
            else if (checkpoint == 7)// A long break has finished
            {
                checkpoint = 0;
            }
        }
        void UpdateCheckpointSymbolSync()
        {
            if (checkpoint == 1)
            {
                txt_checkpointSession00.Text = "\uF127";
            }
            else if (checkpoint == 2)
            {
                txt_checkpointSession00.Text = "\uF127";
                txt_checkpointBreak00.Text = "\uF127";
            }
            else if (checkpoint == 3)
            {
                txt_checkpointSession00.Text = "\uF127";
                txt_checkpointBreak00.Text = "\uF127";
                txt_checkpointSession01.Text = "\uF127";
            }
            else if (checkpoint == 4)
            {
                txt_checkpointSession00.Text = "\uF127";
                txt_checkpointBreak00.Text = "\uF127";
                txt_checkpointSession01.Text = "\uF127";
                txt_checkpointBreak01.Text = "\uF127";
            }
            else if (checkpoint == 5)
            {
                txt_checkpointSession00.Text = "\uF127";
                txt_checkpointBreak00.Text = "\uF127";
                txt_checkpointSession01.Text = "\uF127";
                txt_checkpointBreak01.Text = "\uF127";
                txt_checkpointSession02.Text = "\uF127";
            }
            else if (checkpoint == 6)
            {
                txt_checkpointSession00.Text = "\uF127";
                txt_checkpointBreak00.Text = "\uF127";
                txt_checkpointSession01.Text = "\uF127";
                txt_checkpointBreak01.Text = "\uF127";
                txt_checkpointSession02.Text = "\uF127";
                txt_checkpointBreak02.Text = "\uF127";
            }
            else if (checkpoint == 7)
            {
                txt_checkpointSession00.Text = "\uF127";
                txt_checkpointBreak00.Text = "\uF127";
                txt_checkpointSession01.Text = "\uF127";
                txt_checkpointBreak01.Text = "\uF127";
                txt_checkpointSession02.Text = "\uF127";
                txt_checkpointBreak02.Text = "\uF127";
                txt_checkpointSession03.Text = "\uF127";
            }
            else if (checkpoint == 0)
            {
                txt_checkpointSession00.Text = "\uF126";
                txt_checkpointBreak00.Text = "\uF126";
                txt_checkpointSession01.Text = "\uF126";
                txt_checkpointBreak01.Text = "\uF126";
                txt_checkpointSession02.Text = "\uF126";
                txt_checkpointBreak02.Text = "\uF126";
                txt_checkpointSession03.Text = "\uF126";
            }
        }
        void CheckpointTimeCheckerSync()
        {
            if(useDefaultSettings)
            {
                if (checkpoint == 0 || checkpoint == 2 || checkpoint == 4 || checkpoint == 6)//A session has finished
                {
                    pageTimer = defaultSettings.GetSessionTime();
                }
                else if (checkpoint == 1 || checkpoint == 3 || checkpoint == 5)//A short break has finished
                {
                    pageTimer = defaultSettings.GetShortBreakTime();
                }
                else if (checkpoint == 7)// A long break has finished
                {
                    pageTimer = defaultSettings.GetLongBreakTime();
                }
            }
            else
            {
                if (checkpoint == 0 || checkpoint == 2 || checkpoint == 4 || checkpoint == 6)//A session has finished
                {
                    pageTimer = presetSettings.GetSessionTime();
                }
                else if (checkpoint == 1 || checkpoint == 3 || checkpoint == 5)//A short break has finished
                {
                    pageTimer = presetSettings.GetShortBreakTime();
                }
                else if (checkpoint == 7)// A long break has finished
                {
                    pageTimer = presetSettings.GetLongBreakTime();
                }
            }
        }
        void UpdatePageTimerTextSync()
        {
            txt_timer.Text = pageTimer.ToString("mm\\:ss");
        }
        void UpdatePageButtonTextSync()
        {
            if (counting)
            {
                btn_toggleTimer.Content = "\uEDB4";
            }
            else
            {
                btn_toggleTimer.Content = "\uE768";
            }
        }
        void toggleCounting()
        {
            counting = !counting;
            UpdatePageButtonTextSync();
        }

        private void grd_checkpoint_Tapped(object sender, TappedRoutedEventArgs e)
        {
            StopAlarm();
            ElementSoundPlayer.Play(ElementSoundKind.Focus);
            if (timerTask != null)
            {
                UpdateCheckpointAdd();
                UpdateCheckpointSymbolSync();
                CheckpointTimeCheckerSync();
                counting = false;
                UpdatePageButtonTextSync();
            }
        }

        private void btn_toggleTimer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            StopAlarm();
            if (timerTask == null)
            {
                if (useDefaultSettings)
                {
                    timerTask = TimerAsync(defaultSettings);
                }
                else
                {
                    timerTask = TimerAsync(presetSettings);
                }
            }
            toggleCounting();
        }

        private void btn_refresh_Tapped(object sender, TappedRoutedEventArgs e)
        {
            StopAlarm();
            CheckpointTimeCheckerSync();
            counting = false;
            UpdatePageButtonTextSync();
        }

        private void grd_checkpoint_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            grd_checkpoint.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            grd_checkpoint.BorderThickness = new Thickness(2);
            grd_checkpoint.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 145, 145, 145));
        }

        private void grd_checkpoint_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            grd_checkpoint.Background = new SolidColorBrush(Color.FromArgb(0,0,0,0));
            grd_checkpoint.BorderThickness = new Thickness(2);
            grd_checkpoint.BorderBrush = new SolidColorBrush(Color.FromArgb(0,0,0,0));
        }

        private void btn_settings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            spltvw_settings.IsPaneOpen = !spltvw_settings.IsPaneOpen;
        }

        public void UpdateSessionSettingsFromCmbbxChange()
        {
            if (useDefaultSettings)
            {
                //Do nothing.
            }
            else
            {
                presetSettings.SetSessionTime(new TimeSpan(0, (int)cmbbx_sessionTime.SelectedItem, 0));
                SettingsDB.DB_UPDATE_PRESET(presetSettings);
                CheckpointTimeCheckerSync();
            }
        }

        public void UpdateShortSettingsFromCmbbxChange()
        {
            if (useDefaultSettings)
            {
                //Do nothing.
            }
            else
            {
                presetSettings.SetShortBreakTime(new TimeSpan(0, (int)cmbbx_shortTime.SelectedItem, 0));
                SettingsDB.DB_UPDATE_PRESET(presetSettings);
                CheckpointTimeCheckerSync();
            }
        }

        public void UpdateLongSettingsFromCmbbxChange()
        {
            if (useDefaultSettings)
            {
                //Do nothing.
            }
            else
            {
                presetSettings.SetLongBreakTime(new TimeSpan(0, (int)cmbbx_longTime.SelectedItem, 0));
                SettingsDB.DB_UPDATE_PRESET(presetSettings);
                CheckpointTimeCheckerSync();
            }
        }

        public void UpdateCmbbxsFromPresetCmbbxChange()
        {
            if(cmbbx_presetSettings.SelectedItem == null)
            {
                useDefaultSettings = true;
                SetCmbbxFromSettings();
            }
            else if (cmbbx_presetSettings.SelectedItem.ToString() == "default")
            {
                useDefaultSettings = true;
                SetCmbbxFromSettings();
            }
            else
            {
                useDefaultSettings = false;
                presetSettings = SettingsDB.DB_Select_PRESET(cmbbx_presetSettings.SelectedItem.ToString());
                SetCmbbxFromSettings();
            }

        }

        private void cmbbx_sessionTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSessionSettingsFromCmbbxChange();
        }

        private void cmbbx_shortTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateShortSettingsFromCmbbxChange();
        }

        private void cmbbx_longTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLongSettingsFromCmbbxChange();
        }

        private void cmbbx_presetSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCmbbxsFromPresetCmbbxChange();
            CheckpointTimeCheckerSync();
            counting = false;
            UpdatePageButtonTextSync();
        }

        private void btn_insertPresetName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (txt_insertPresetName.Text == "default" || txt_insertPresetName.Text == "" ||
                txt_insertPresetName.Text == null || presetList.Contains(txt_insertPresetName.Text))
            {
                //do nothing..
            }
            else
            {
                presetList.Add(txt_insertPresetName.Text);
                SettingsDB.DB_Insert_PRESET(new PresetSettings(txt_insertPresetName.Text));
                cmbbx_presetSettings.SelectedItem = txt_insertPresetName.Text;
                txt_insertPresetName.Text = "";
            }
        }

        private void btn_deletePreset_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if((string)cmbbx_presetSettings.SelectedItem == "" || (string)cmbbx_presetSettings.SelectedItem == "default")
            {
                //do nothing..
            }
            else
            {
                string aString = cmbbx_presetSettings.SelectedItem.ToString();
                presetList.Remove(aString);
                SettingsDB.DB_Delete_PRESET(aString);
                cmbbx_presetSettings.SelectedIndex = 0;
            }
        }

        private void btn_sfx_Tapped(object sender, TappedRoutedEventArgs e)
        {
            StopAlarm();
            if (ElementSoundPlayer.State == ElementSoundPlayerState.Off)
            {
                ElementSoundPlayer.State = ElementSoundPlayerState.On;
                btn_sfx.Content = "\uE995";
                alarmPlayer.Volume = 0.5;
            }
            else
            {
                ElementSoundPlayer.State = ElementSoundPlayerState.Off;
                btn_sfx.Content = "\uE992";
                alarmPlayer.Volume = 0;
            }
        }

        private void btn_toggleTheme_Tapped(object sender, TappedRoutedEventArgs e)
        {
            useDarkTheme = !useDarkTheme;
            SettingsDB.DB_Update_USEDARKTHEME(useDarkTheme);
            RefreshTheme();
        }
    }
}