using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FritzboxPhonebookConv.Models;
using FritzboxPhonebookConv.Services;
using Microsoft.Win32;

namespace FritzboxPhonebookConv.ViewModels
{
    /// <summary>
    /// Main view-model.  Orchestrates Fritz.Box connectivity, phonebook download,
    /// XSLT profile management, and the transform/save workflow.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AppSettings _settings;

        // Raw XML downloaded from the selected phonebook (null until downloaded).
        private byte[] _downloadedXmlBytes;

        private bool _isBusy;
        private string _statusMessage = "Ready.";
        private Phonebook _selectedPhonebook;
        private XsltProfile _selectedXsltProfile;
        private string _outputFilePath = string.Empty;
        private string _newProfileName = string.Empty;
        private string _newProfilePath = string.Empty;

        public MainViewModel()
        {
            _settings = SettingsService.Load();
            XsltProfiles = new ObservableCollection<XsltProfile>(_settings.XsltProfiles ?? new List<XsltProfile>());
            OutputFilePath = _settings.LastOutputDirectory ?? string.Empty;

            ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsBusy);
            DownloadPhonebookCommand = new AsyncRelayCommand(
                DownloadPhonebookAsync,
                () => !IsBusy && SelectedPhonebook != null);
            AddXsltProfileCommand = new RelayCommand(AddXsltProfile, () => !IsBusy);
            RemoveXsltProfileCommand = new RelayCommand(
                RemoveXsltProfile,
                () => SelectedXsltProfile != null && !IsBusy);
            BrowseXsltFileCommand = new RelayCommand(BrowseXsltFile);
            BrowseOutputCommand = new RelayCommand(BrowseOutputFile, () => !IsBusy);
            TransformAndSaveCommand = new AsyncRelayCommand(
                TransformAndSaveAsync,
                () => !IsBusy
                      && _downloadedXmlBytes != null
                      && SelectedXsltProfile != null
                      && !string.IsNullOrEmpty(OutputFilePath));
        }

        #region Properties

        public ObservableCollection<Phonebook> Phonebooks { get; } = new ObservableCollection<Phonebook>();
        public ObservableCollection<XsltProfile> XsltProfiles { get; }

        public string Host
        {
            get => _settings.Host;
            set { _settings.Host = value; OnPropertyChanged(); }
        }

        public int Port
        {
            get => _settings.Port;
            set { _settings.Port = value; OnPropertyChanged(); }
        }

        public string Username
        {
            get => _settings.Username;
            set { _settings.Username = value; OnPropertyChanged(); }
        }

        // Password is held only in memory and never persisted.
        public string Password { get; set; } = string.Empty;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        public Phonebook SelectedPhonebook
        {
            get => _selectedPhonebook;
            set
            {
                _selectedPhonebook = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public XsltProfile SelectedXsltProfile
        {
            get => _selectedXsltProfile;
            set
            {
                _selectedXsltProfile = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string OutputFilePath
        {
            get => _outputFilePath;
            set
            {
                _outputFilePath = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string NewProfileName
        {
            get => _newProfileName;
            set { _newProfileName = value; OnPropertyChanged(); }
        }

        public string NewProfilePath
        {
            get => _newProfilePath;
            set { _newProfilePath = value; OnPropertyChanged(); }
        }

        /// <summary>True once a phonebook has been successfully downloaded.</summary>
        public bool HasDownloadedXml => _downloadedXmlBytes != null;

        #endregion

        #region Commands

        public ICommand ConnectCommand { get; }
        public ICommand DownloadPhonebookCommand { get; }
        public ICommand AddXsltProfileCommand { get; }
        public ICommand RemoveXsltProfileCommand { get; }
        public ICommand BrowseXsltFileCommand { get; }
        public ICommand BrowseOutputCommand { get; }
        public ICommand TransformAndSaveCommand { get; }

        #endregion

        #region Command Implementations

        private async Task ConnectAsync()
        {
            IsBusy = true;
            StatusMessage = "Connecting to Fritz.Box…";
            Phonebooks.Clear();
            _downloadedXmlBytes = null;
            OnPropertyChanged(nameof(HasDownloadedXml));

            try
            {
                using (var service = new FritzBoxService(Host, Port, Username, Password))
                {
                    List<Phonebook> phonebooks = await service.GetPhonebooksAsync().ConfigureAwait(true);
                    foreach (Phonebook pb in phonebooks)
                        Phonebooks.Add(pb);

                    if (Phonebooks.Count > 0)
                        SelectedPhonebook = Phonebooks[0];
                }

                StatusMessage = $"Connected. Found {Phonebooks.Count} phonebook(s).";
                PersistSettings();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection failed: {ex.Message}";
                MessageBox.Show(
                    $"Could not connect to Fritz.Box:\n\n{ex.Message}",
                    "Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DownloadPhonebookAsync()
        {
            if (SelectedPhonebook == null) return;

            IsBusy = true;
            StatusMessage = $"Downloading '{SelectedPhonebook.Name}'…";
            _downloadedXmlBytes = null;
            OnPropertyChanged(nameof(HasDownloadedXml));

            try
            {
                using (var service = new FritzBoxService(Host, Port, Username, Password))
                {
                    string phonebookXml = await service.DownloadPhonebookXmlAsync(SelectedPhonebook.Url).ConfigureAwait(true);
                    _downloadedXmlBytes = System.Text.Encoding.UTF8.GetBytes(phonebookXml);
                }

                OnPropertyChanged(nameof(HasDownloadedXml));
                CommandManager.InvalidateRequerySuggested();
                StatusMessage = $"'{SelectedPhonebook.Name}' downloaded successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Download failed: {ex.Message}";
                MessageBox.Show(
                    $"Could not download phonebook:\n\n{ex.Message}",
                    "Download Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void AddXsltProfile()
        {
            if (string.IsNullOrWhiteSpace(NewProfileName))
            {
                MessageBox.Show("Please enter a name for the XSLT profile.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(NewProfilePath) || !File.Exists(NewProfilePath))
            {
                MessageBox.Show("Please select a valid XSLT file path.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var profile = new XsltProfile { Name = NewProfileName.Trim(), FilePath = NewProfilePath };
            XsltProfiles.Add(profile);
            NewProfileName = string.Empty;
            NewProfilePath = string.Empty;
            PersistSettings();
        }

        private void RemoveXsltProfile()
        {
            if (SelectedXsltProfile == null) return;
            XsltProfiles.Remove(SelectedXsltProfile);
            SelectedXsltProfile = null;
            PersistSettings();
        }

        private void BrowseXsltFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select XSLT file",
                Filter = "XSLT files (*.xslt;*.xsl)|*.xslt;*.xsl|All files (*.*)|*.*",
                CheckFileExists = true,
            };
            if (dlg.ShowDialog() == true)
                NewProfilePath = dlg.FileName;
        }

        private void BrowseOutputFile()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save transformed XML",
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                DefaultExt = ".xml",
                OverwritePrompt = true,
            };

            if (!string.IsNullOrEmpty(_settings.LastOutputDirectory)
                && Directory.Exists(_settings.LastOutputDirectory))
            {
                dlg.InitialDirectory = _settings.LastOutputDirectory;
            }

            if (dlg.ShowDialog() == true)
            {
                OutputFilePath = dlg.FileName;
                _settings.LastOutputDirectory = Path.GetDirectoryName(dlg.FileName) ?? string.Empty;
                PersistSettings();
            }
        }

        private async Task TransformAndSaveAsync()
        {
            if (_downloadedXmlBytes == null || SelectedXsltProfile == null || string.IsNullOrEmpty(OutputFilePath))
                return;

            IsBusy = true;
            StatusMessage = "Transforming…";

            try
            {
                string xmlInput = System.Text.Encoding.UTF8.GetString(_downloadedXmlBytes);
                string xsltPath = SelectedXsltProfile.FilePath;
                string outputPath = OutputFilePath;

                byte[] result = await Task.Run(() =>
                    XsltTransformService.TransformToBytes(xmlInput, xsltPath)).ConfigureAwait(true);

                XsltTransformService.SaveToFile(result, outputPath);

                StatusMessage = $"Saved successfully: {outputPath}";
                MessageBox.Show(
                    $"File saved successfully:\n{outputPath}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Transform/save failed: {ex.Message}";
                MessageBox.Show(
                    $"Transform or save failed:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        private void PersistSettings()
        {
            _settings.XsltProfiles = new List<XsltProfile>(XsltProfiles);
            SettingsService.Save(_settings);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
