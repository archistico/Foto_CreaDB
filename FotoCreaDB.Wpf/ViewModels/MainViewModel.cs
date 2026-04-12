using FotoCreaDB.Wpf.Adapters;
using FotoCreaDB.Wpf.Commands;
using Foto_CreaDB2;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace FotoCreaDB.Wpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AnalysisService _analysisService;
        private readonly DuplicateReportService _reportService;
        private readonly DuplicateDeletionService _deletionService;
        private readonly WpfServiceBridge _bridge;

        private string _fotoPath = string.Empty;
        private string _databasePath = "foto.db";
        private bool _verboseDuplicates;
        private bool _isBusy;
        private string _statusMessage = "Pronto";
        private int _lastDuplicateGroupsCount;
        private int _lastFilesToDeleteCount;
        private string _lastFilesToDeleteSizeFormatted = "0 B";

        public MainViewModel()
        {
            _analysisService = new AnalysisService();
            _reportService = new DuplicateReportService();
            _deletionService = new DuplicateDeletionService();
            _bridge = new WpfServiceBridge();

            AnalyzeCommand = new RelayCommand(async _ => await RunAnalyzeAsync(), _ => CanRunAnalyze());
            ReportCommand = new RelayCommand(async _ => await RunReportAsync(), _ => CanRunReport());
            DeleteCommand = new RelayCommand(async _ => await RunDeleteAsync(), _ => CanRunDelete());
            ClearLogCommand = new RelayCommand(_ => _bridge.ClearLogs());

            BrowseFotoPathCommand = new RelayCommand(_ => BrowseFotoPath(), _ => !IsBusy);
            BrowseDatabasePathCommand = new RelayCommand(_ => BrowseDatabasePath(), _ => !IsBusy);

            OpenPathCommand = new RelayCommand(path => OpenPath(path as string));
            ShowPathInExplorerCommand = new RelayCommand(path => ShowPathInExplorer(path as string));
            DeleteAnalysisDatabaseCommand = new RelayCommand(_ => DeleteAnalysisDatabase(), _ => CanDeleteDatabase());

            // Carica preferenze salvate da appsettings.json (se presenti).
            try
            {
                string baseConfigPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                string currentConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

                AppConfig? cfg = AppConfigLoader.LoadFromFile(baseConfigPath) ?? AppConfigLoader.LoadFromFile(currentConfigPath);

                if (cfg != null)
                {
                    if (cfg.Paths != null && cfg.Paths.Length > 0 && !string.IsNullOrWhiteSpace(cfg.Paths[0]))
                    {
                        string loadedFoto = cfg.Paths[0];
                        // normalize to absolute
                        try
                        {
                            FotoPath = Path.GetFullPath(loadedFoto);
                        }
                        catch
                        {
                            FotoPath = loadedFoto;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(cfg.NomeDb))
                    {
                        string dbPath = cfg.NomeDb;
                        try
                        {
                            DatabasePath = Path.GetFullPath(dbPath);
                        }
                        catch
                        {
                            DatabasePath = dbPath;
                        }
                    }
                    // Loggare le estensioni permesse e le estensioni immagine per metadati
                    try
                    {
                        string allowed = cfg.EstensioniPermesse != null
                            ? string.Join(", ", cfg.EstensioniPermesse)
                            : string.Empty;

                        string imageExt = cfg.ImageExtensions != null
                            ? string.Join(", ", cfg.ImageExtensions)
                            : string.Empty;

                        if (!string.IsNullOrWhiteSpace(allowed))
                        {
                            ServiceCallbackHelper.Info(_bridge.OnLog, "Estensioni processabili: " + allowed);
                        }

                        if (!string.IsNullOrWhiteSpace(imageExt))
                        {
                            ServiceCallbackHelper.Info(_bridge.OnLog, "Estensioni immagini (metadati): " + imageExt);
                        }
                    }
                    catch
                    {
                        // ignorare problemi di log
                    }
                }
            }
            catch
            {
                // ignore loading errors
            }
        }

        /// <summary>
        /// Salva le preferenze correnti (cartella foto e database) in `appsettings.json`
        /// nella cartella di lavoro corrente.
        /// </summary>
        public void SavePreferences()
        {
            try
            {
                string configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                PreferencesSaver.SavePreferences(FotoPath, DatabasePath, configPath);
            }
            catch
            {
                // non fare nulla se il salvataggio fallisce
            }
        }

        private static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
            {
                return (bytes / (double)GB).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " GB";
            }

            if (bytes >= MB)
            {
                return (bytes / (double)MB).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " MB";
            }

            if (bytes >= KB)
            {
                return (bytes / (double)KB).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " KB";
            }

            return bytes + " B";
        }

        public string FotoPath
        {
            get => _fotoPath;
            set
            {
                if (SetProperty(ref _fotoPath, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string DatabasePath
        {
            get => _databasePath;
            set
            {
                if (SetProperty(ref _databasePath, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool VerboseDuplicates
        {
            get => _verboseDuplicates;
            set => SetProperty(ref _verboseDuplicates, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public int LastDuplicateGroupsCount
        {
            get => _lastDuplicateGroupsCount;
            set => SetProperty(ref _lastDuplicateGroupsCount, value);
        }

        public int LastFilesToDeleteCount
        {
            get => _lastFilesToDeleteCount;
            set => SetProperty(ref _lastFilesToDeleteCount, value);
        }

        public string LastFilesToDeleteSizeFormatted
        {
            get => _lastFilesToDeleteSizeFormatted;
            set => SetProperty(ref _lastFilesToDeleteSizeFormatted, value);
        }

        private bool CanDeleteDatabase()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(DatabasePath);
        }

        private void DeleteAnalysisDatabase()
        {
            if (string.IsNullOrWhiteSpace(DatabasePath))
            {
                return;
            }

            var result = System.Windows.MessageBox.Show(
                "Vuoi cancellare il database di analisi indicato?\n" + DatabasePath,
                "Conferma cancellazione database",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                string full = DatabasePath;
                try
                {
                    full = Path.GetFullPath(DatabasePath);
                }
                catch
                {
                }

                bool deleted = DatabaseFileManager.DeleteDatabaseFiles(full, _bridge.OnLog);
                if (deleted)
                {
                    StatusMessage = "Database cancellato.";
                }
                else
                {
                    StatusMessage = "Database non trovato.";
                }

                // update commands
                CommandManager.InvalidateRequerySuggested();
            }
            catch (System.Exception ex)
            {
                ServiceCallbackHelper.Error(_bridge.OnLog, "Errore durante la cancellazione del database.", ex);
                StatusMessage = "Errore durante la cancellazione del database.";
            }
        }

        public ProgressStateViewModel AnalysisState => _bridge.AnalysisState;

        public ProgressStateViewModel ReportState => _bridge.ReportState;

        public ProgressStateViewModel DeletionState => _bridge.DeletionState;

        public ObservableCollection<LogMessageViewModel> LogMessages => _bridge.LogMessages;

        public ICommand AnalyzeCommand { get; }

        public ICommand ReportCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand ClearLogCommand { get; }

        public ICommand BrowseFotoPathCommand { get; }

        public ICommand BrowseDatabasePathCommand { get; }
        public ICommand DeleteAnalysisDatabaseCommand { get; }

        public ICommand OpenPathCommand { get; }

        public ICommand ShowPathInExplorerCommand { get; }

        private bool CanRunAnalyze()
        {
            return !IsBusy
                && !string.IsNullOrWhiteSpace(FotoPath)
                && !string.IsNullOrWhiteSpace(DatabasePath);
        }

        private bool CanRunReport()
        {
            return !IsBusy
                && !string.IsNullOrWhiteSpace(DatabasePath);
        }

        private bool CanRunDelete()
        {
            return !IsBusy
                && !string.IsNullOrWhiteSpace(DatabasePath);
        }

        private void BrowseFotoPath()
        {
            using FolderBrowserDialog dialog = new FolderBrowserDialog();

            dialog.Description = "Seleziona la cartella delle foto";
            dialog.UseDescriptionForTitle = true;

            if (!string.IsNullOrWhiteSpace(FotoPath) && Directory.Exists(FotoPath))
            {
                dialog.InitialDirectory = FotoPath;
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FotoPath = dialog.SelectedPath;
            }
        }

        private void BrowseDatabasePath()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.Title = "Seleziona il database SQLite";
            dialog.Filter = "Database SQLite (*.db)|*.db|Tutti i file (*.*)|*.*";
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = true;

            if (!string.IsNullOrWhiteSpace(DatabasePath))
            {
                try
                {
                    string? directory = Path.GetDirectoryName(DatabasePath);
                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    {
                        dialog.InitialDirectory = directory;
                    }

                    string? fileName = Path.GetFileName(DatabasePath);
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        dialog.FileName = fileName;
                    }
                }
                catch
                {
                }
            }

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                DatabasePath = dialog.FileName;
            }
        }

        private void OpenPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                string normalizedPath = path.Trim().Trim('"');

                if (File.Exists(normalizedPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = normalizedPath,
                        UseShellExecute = true
                    });

                    return;
                }

                if (Directory.Exists(normalizedPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = normalizedPath,
                        UseShellExecute = true
                    });

                    return;
                }

                string? parentDirectory = Path.GetDirectoryName(normalizedPath);
                if (!string.IsNullOrWhiteSpace(parentDirectory) && Directory.Exists(parentDirectory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = parentDirectory,
                        UseShellExecute = true
                    });

                    return;
                }

                System.Windows.MessageBox.Show(
                    "Il file o la cartella non esistono più:\n" + normalizedPath,
                    "Percorso non trovato",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Errore durante l'apertura del file:\n" + ex.Message,
                    "Errore",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ShowPathInExplorer(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                string normalizedPath = path.Trim().Trim('"');

                if (File.Exists(normalizedPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "/select,\"" + normalizedPath + "\"",
                        UseShellExecute = true
                    });

                    return;
                }

                if (Directory.Exists(normalizedPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "\"" + normalizedPath + "\"",
                        UseShellExecute = true
                    });

                    return;
                }

                string? parentDirectory = Path.GetDirectoryName(normalizedPath);
                if (!string.IsNullOrWhiteSpace(parentDirectory) && Directory.Exists(parentDirectory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "\"" + parentDirectory + "\"",
                        UseShellExecute = true
                    });

                    return;
                }

                System.Windows.MessageBox.Show(
                    "Il file o la cartella non esistono più:\n" + normalizedPath,
                    "Percorso non trovato",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Errore durante l'apertura di Esplora Risorse:\n" + ex.Message,
                    "Errore",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task RunAnalyzeAsync()
        {
            IsBusy = true;
            StatusMessage = "Analisi in corso...";
            _bridge.ClearLogs();
            _bridge.ResetAnalysis();

            try
            {
                AnalysisResult result = await Task.Run(() =>
                    _analysisService.Run(
                        new AnalysisRequest
                        {
                            Paths = new[] { FotoPath },
                            NomeDb = DatabasePath,
                            CancellaDbSeEsiste = false,
                            LogDettagliato = false,
                            ProgressEvery = 1000,
                            VerboseDuplicates = VerboseDuplicates
                        },
                        _bridge.CreateAnalysisProgress(),
                        _bridge.OnLog));

                StatusMessage = result.Message;
                AnalysisState.IsRunning = false;

                // salva preferenze (cartella foto e database) su appsettings.json
                SavePreferences();
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Errore in analisi.";
                _bridge.OnLog(new ServiceLogMessage
                {
                    Timestamp = System.DateTime.Now,
                    Level = ServiceLogLevel.Error,
                    Message = "Errore durante l'analisi.",
                    Exception = ex
                });
                AnalysisState.IsRunning = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RunReportAsync()
        {
            IsBusy = true;
            StatusMessage = "Report in corso...";
            _bridge.ClearLogs();
            _bridge.ResetReport();

            try
            {
                DuplicateReportResult result = await Task.Run(() =>
                    _reportService.Run(
                        new DuplicateReportRequest
                        {
                            NomeDb = DatabasePath,
                            VerboseDuplicates = VerboseDuplicates
                        },
                        _bridge.CreateReportProgress(),
                        _bridge.OnLog));

                LastDuplicateGroupsCount = result.DuplicateGroupsCount;
                LastFilesToDeleteCount = result.FilesToDeleteCount;
                LastFilesToDeleteSizeFormatted = FormatBytes(result.FilesToDeleteSize);

                StatusMessage = "Report completato.";
                ReportState.IsRunning = false;

                // salva preferenze
                SavePreferences();
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Errore nel report.";
                _bridge.OnLog(new ServiceLogMessage
                {
                    Timestamp = System.DateTime.Now,
                    Level = ServiceLogLevel.Error,
                    Message = "Errore durante il report.",
                    Exception = ex
                });
                ReportState.IsRunning = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RunDeleteAsync()
        {
            System.Windows.MessageBoxResult confirm = System.Windows.MessageBox.Show(
                "Procedo alla cancellazione dei duplicati?",
                "Conferma cancellazione",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = "Cancellazione in corso...";
            _bridge.ClearLogs();
            _bridge.ResetDeletion();

            try
            {
                DeletionResult result = await Task.Run(() =>
                    _deletionService.Run(
                        new DeletionRequest
                        {
                            NomeDb = DatabasePath,
                            VerboseDuplicates = VerboseDuplicates
                        },
                        _bridge.CreateDeletionProgress(),
                        _bridge.OnLog));

                StatusMessage = result.Message;
                DeletionState.IsRunning = false;

                // salva preferenze
                SavePreferences();
            }
            catch (System.Exception ex)
            {
                StatusMessage = "Errore in cancellazione.";
                _bridge.OnLog(new ServiceLogMessage
                {
                    Timestamp = System.DateTime.Now,
                    Level = ServiceLogLevel.Error,
                    Message = "Errore durante la cancellazione.",
                    Exception = ex
                });
                DeletionState.IsRunning = false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}