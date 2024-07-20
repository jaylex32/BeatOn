using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;


namespace WpfApp1
{
    public partial class SettingsWindow : Window
    {
        private Settings settings;
        private Dictionary<string, string> deezerPresets;
        private Dictionary<string, string> qobuzPresets;
        public SettingsWindow(Settings settings)
        {
            InitializeComponent();
            this.settings = settings;
            DataContext = settings;
            InitializePresets();
            this.Loaded += SettingsWindow_Loaded; // Subscribe to the Loaded event
            GeneralSection.Visibility = Visibility.Visible;
            RestoreLastSection();
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            UpdateDeezerPresetSelection();
            UpdateQobuzPresetSelection();
        }

        private void LoadSettings()
        {
            try
            {
                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);
                DataContext = settings;
                SetQobuzQualityComboBox(settings.QobuzQuality);
                QobuzFolderPathTextBox.Text = settings.QobuzFolderPath;
                DeezerFolderPathTextBox.Text = settings.DeezerFolderPath;
                QobuzQualityComboBox.SelectedItem = FindComboBoxItem(QobuzQualityComboBox, settings.QobuzQuality);
                DeezerQualityComboBox.SelectedItem = FindComboBoxItem(DeezerQualityComboBox, settings.DeezerQuality);
                QobuzFileNameFormatTextBox.Text = settings.saveLayout.qobuzAlbum;
                DeezerFileNameFormatTextBox.Text = settings.saveLayout.album;
                LidarrEnabledCheckBox.IsChecked = settings.LidarrEnabled;
                LidarrUrlTextBox.Text = settings.LidarrUrl;
                LidarrApiKeyTextBox.Text = settings.LidarrApiKey;
                BeatOnServerEnabledCheckBox.IsChecked = settings.WebUIEnabled;
                BeatOnUrlTextBox.Text = settings.WebUIUrl;

                InitializePresets(); // Make sure presets are initialized
                UpdateDeezerPresetSelection();
                UpdateQobuzPresetSelection();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading settings: {ex.Message}");
            }
        }

        private async void ArlButton_Click(object sender, RoutedEventArgs e)
        {
            DeezerArlScraper scraper = new DeezerArlScraper();
            string arl = await scraper.GetRandomArlAsync();

            if (!string.IsNullOrEmpty(arl))
            {
                settings.cookies.arl = arl; // Access cookies through the settings object
                DeezerArl.Text = arl; // Update the TextBox if needed
            }
            else
            {
                // Handle case where no ARLs are found
                System.Windows.MessageBox.Show("No ARLs found. Please try again later."); // Corrected MessageBox reference
            }
        }

        private void InitializePresets()
        {
            // Presets for Deezer
            deezerPresets = new Dictionary<string, string>
        {
            { "Track", "{ALB_TITLE}/{SNG_TITLE}" },
            { "Album", "{ART_NAME}/{ALB_TITLE}/{SNG_TITLE}" },
            { "Playlist", "Playlist/{TITLE}/{SNG_TITLE}" },
            { "Server", "{ART_NAME}/{ART_NAME} - {ALB_TITLE}/{NO_TRACK_NUMBER}{ART_NAME} - {SNG_TITLE}" },           
        };

            // Presets for Qobuz
            qobuzPresets = new Dictionary<string, string>
        {
            { "Track", "{alb_title}/{no_track_number}{title}" },
            { "Album", "{alb_artist}/{alb_title}/{title}" },
            { "Artist", "{alb_artist}/{alb_title}/{no_track_number}{alb_artist} - {title}" },
            { "Server", "{alb_artist}/{alb_artist} - {alb_title}/{alb_artist} - {title}" },
            
        };

            PopulatePresetComboBoxes();
        }

        private void PopulatePresetComboBoxes()
        {
            // Populate Deezer presets
            deezerPresetComboBox.Items.Clear();
            foreach (var preset in deezerPresets)
            {
                deezerPresetComboBox.Items.Add(new ComboBoxItem { Content = preset.Key });
            }

            // Populate Qobuz presets
            qobuzPresetComboBox.Items.Clear();
            foreach (var preset in qobuzPresets)
            {
                qobuzPresetComboBox.Items.Add(new ComboBoxItem { Content = preset.Key });
            }
        }

        private void DeezerPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (deezerPresetComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedPreset = selectedItem.Content.ToString();
                if (deezerPresets.TryGetValue(selectedPreset, out var format))
                {
                    DeezerFileNameFormatTextBox.Text = format; // Update the textbox
                }
            }
        }

        private void QobuzPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (qobuzPresetComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectedPreset = selectedItem.Content.ToString();
                if (qobuzPresets.TryGetValue(selectedPreset, out var format))
                {
                    QobuzFileNameFormatTextBox.Text = format; // Update the textbox
                }
            }
        }

        private void QobuzQualityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QobuzQualityComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                settings.QobuzQuality = selectedItem.Tag.ToString();
            }
        }

        private void SetQobuzQualityComboBox(string quality)
        {
            foreach (ComboBoxItem item in QobuzQualityComboBox.Items)
            {
                if (item.Tag.ToString() == quality)
                {
                    QobuzQualityComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void UpdateDeezerPresetSelection()
        {
            string currentFormat = DeezerFileNameFormatTextBox.Text;
            foreach (var preset in deezerPresets)
            {
                if (preset.Value == currentFormat)
                {
                    foreach (ComboBoxItem item in deezerPresetComboBox.Items)
                    {
                        if (item.Content.ToString() == preset.Key)
                        {
                            deezerPresetComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }

        private void UpdateQobuzPresetSelection()
        {
            string currentFormat = QobuzFileNameFormatTextBox.Text;
            foreach (var preset in qobuzPresets)
            {
                if (preset.Value == currentFormat)
                {
                    foreach (ComboBoxItem item in qobuzPresetComboBox.Items)
                    {
                        if (item.Content.ToString() == preset.Key)
                        {
                            qobuzPresetComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }

        private void DeezerFileNameFormatTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDeezerPresetSelection();
        }

        private void QobuzFileNameFormatTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateQobuzPresetSelection();
        }
        private void FormatOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            FormatOptionsPopup popup = new FormatOptionsPopup();
            popup.Show(); // Opens the popup non-modally
        }

        private void Qobuz_FormatOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            Qobuz_FormatOptionsPopup popup = new Qobuz_FormatOptionsPopup();
            popup.Show(); // Opens the popup non-modally
        }

        private System.Windows.Controls.ComboBoxItem FindComboBoxItem(System.Windows.Controls.ComboBox comboBox, string value)
        {
            foreach (System.Windows.Controls.ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    return item;
                }
            }
            return null;  // Not found
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save the modified settings back to the JSON file
            try
            {
                settings.QobuzFolderPath = QobuzFolderPathTextBox.Text;
                settings.DeezerFolderPath = DeezerFolderPathTextBox.Text;
                settings.QobuzQuality = (QobuzQualityComboBox.SelectedItem as ComboBoxItem).Content as string;
                settings.DeezerQuality = (DeezerQualityComboBox.SelectedItem as ComboBoxItem).Content as string;
                settings.saveLayout.qobuzAlbum = QobuzFileNameFormatTextBox.Text;
                settings.saveLayout.album = DeezerFileNameFormatTextBox.Text;
                settings.LidarrEnabled = LidarrEnabledCheckBox.IsChecked ?? false;
                settings.LidarrUrl = LidarrUrlTextBox.Text;
                settings.LidarrApiKey = LidarrApiKeyTextBox.Text;
                settings.WebUIEnabled = BeatOnServerEnabledCheckBox.IsChecked ?? false;
                settings.WebUIUrl = BeatOnUrlTextBox.Text;
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText("d-fi.config.json", json);

                System.Windows.MessageBox.Show("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }

        private void QobuzBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select Qobuz folder path
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    QobuzFolderPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void DeezerBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Open folder browser dialog to select Deezer folder path
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    DeezerFolderPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void SidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                // Hide all sections
                GeneralSection.Visibility = Visibility.Collapsed;
                QobuzSection.Visibility = Visibility.Collapsed;
                DeezerSection.Visibility = Visibility.Collapsed;
                LidarrSection.Visibility = Visibility.Collapsed;
                BeatOnSection.Visibility = Visibility.Collapsed;

                // Show the selected section
                switch (button.Tag.ToString())
                {
                    case "GeneralSection":
                        GeneralSection.Visibility = Visibility.Visible;
                        break;
                    case "QobuzSection":
                        QobuzSection.Visibility = Visibility.Visible;
                        break;
                    case "DeezerSection":
                        DeezerSection.Visibility = Visibility.Visible;
                        break;
                    case "LidarrSection":
                        LidarrSection.Visibility = Visibility.Visible;
                        break;
                    case "BeatOnSection":
                        BeatOnSection.Visibility = Visibility.Visible;
                        break;
                }

                // Optionally, you can change the style of the selected button
                foreach (var child in ((StackPanel)((Border)((Grid)this.Content).Children[0]).Child).Children)
                {
                    if (child is System.Windows.Controls.Button b)
                    {
                        b.Background = (b == button) ? new SolidColorBrush(Color.FromRgb(63, 63, 63)) : Brushes.Transparent;
                    }
                }
            }
        }

        private void SaveCurrentSection()
        {
            if (GeneralSection.Visibility == Visibility.Visible)
                Properties.Settings.Default.LastOpenedSection = "GeneralSection";
            else if (QobuzSection.Visibility == Visibility.Visible)
                Properties.Settings.Default.LastOpenedSection = "QobuzSection";
            else if (DeezerSection.Visibility == Visibility.Visible)
                Properties.Settings.Default.LastOpenedSection = "DeezerSection";
            else if (LidarrSection.Visibility == Visibility.Visible)
                Properties.Settings.Default.LastOpenedSection = "LidarrSection";
            else if (BeatOnSection.Visibility == Visibility.Visible)
                Properties.Settings.Default.LastOpenedSection = "BeatOnSection";

            Properties.Settings.Default.Save();
        }

        private void RestoreLastSection()
        {
            string lastSection = Properties.Settings.Default.LastOpenedSection;
            System.Windows.Controls.Button buttonToClick = null;

            foreach (var child in ((StackPanel)((Border)((Grid)this.Content).Children[0]).Child).Children)
            {
                if (child is System.Windows.Controls.Button b && b.Tag?.ToString() == lastSection)
                {
                    buttonToClick = b;
                    break;
                }
            }

            if (buttonToClick != null)
            {
                SidebarButton_Click(buttonToClick, new RoutedEventArgs());
            }
            else
            {
                // Default to General section if no last section was saved
                GeneralSection.Visibility = Visibility.Visible;
            }
        }

        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveCurrentSection();
        }

    }

    public class Settings
    {
        public int ItemProcess { get; set; }

        [JsonIgnore] // This will prevent the property from being serialized
        public int ItemProcessIndex
        {
            get { return ItemProcess - 1; }
            set { ItemProcess = value + 1; }
        }
        public int concurrency { get; set; }

        [JsonIgnore]
        public int ConcurrencyIndex
        {
            get { return concurrency - 1; }
            set { concurrency = value + 1; }
        }
        public SaveLayout saveLayout { get; set; }
        public PlaylistSettings playlist { get; set; }
        public bool trackNumber { get; set; }
        public bool fallbackTrack { get; set; }
        public bool fallbackQuality { get; set; }
        public bool qobuzDownloadCover { get; set; }
        public bool deezerDownloadCover { get; set; }
        public CoverSize coverSize { get; set; }
        public Cookies cookies { get; set; }
        public QobuzSettings qobuz { get; set; }
        public string QobuzFolderPath { get; set; }
        public string DeezerFolderPath { get; set; }
        public string QobuzQuality { get; set; }
        public string DeezerQuality { get; set; }
        public bool LidarrEnabled { get; set; }
        public string LidarrUrl { get; set; } = "http://localhost:8686";
        public string LidarrApiKey { get; set; } = "";
        public bool WebUIEnabled { get; set; }
        public string WebUIUrl { get; set; } = "http://localhost:5000/";


    }

    public class SaveLayout
    {
        public string track { get; set; }
        public string album { get; set; }
        public string artist { get; set; }
        public string playlist { get; set; }

        [JsonProperty("qobuz-album")]
        public string qobuzAlbum { get; set; }
        [JsonProperty("qobuz-track")]
        public string qobuzTrack { get; set; }
        [JsonProperty("qobuz-artist")]
        public string qobuzArtist { get; set; }
    }
    public class PlaylistSettings
    {
        public bool resolveFullPath { get; set; }
    }

    public class CoverSize
    {
        [JsonProperty("128")]
        public int Size128 { get; set; }

        [JsonProperty("320")]
        public int Size320 { get; set; }

        public int Flac { get; set; }
    }


    public class Cookies
    {
        public string arl { get; set; }
    }

    public class QobuzSettings
    {
        public int app_id { get; set; }
        public string secrets { get; set; }
        public string token { get; set; }
    }
}