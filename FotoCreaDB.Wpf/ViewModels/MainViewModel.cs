using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using FotoCreaDB.Wpf.Adapters;
using FotoCreaDB.Wpf.Commands;
using Foto_CreaDB2;
using System.Windows.Forms;

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
        private bool _isBusy;
        private string _statusMessage = "Pronta.";
        private bool _verboseDuplicates;
        private int _lastDuplicateGroupsCount;
        private int _lastFilesToDeleteCount;

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

                StatusMessage = "Report completato.";
                ReportState.IsRunning = false;
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