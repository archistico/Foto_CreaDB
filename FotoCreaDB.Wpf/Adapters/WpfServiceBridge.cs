using Foto_CreaDB2;
using FotoCreaDB.Wpf.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace FotoCreaDB.Wpf.Adapters
{
    public class WpfServiceBridge : IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private readonly DispatcherTimer _progressTimer;
        private CoalescingProgress<AnalysisProgress> _analysisProgress = new();
        private CoalescingProgress<DuplicateReportProgress> _reportProgress = new();
        private CoalescingProgress<DeletionProgress> _deletionProgress = new();
        private volatile bool _disposed;

        public WpfServiceBridge()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _progressTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _progressTimer.Tick += DrainProgress;
            AnalysisState = new ProgressStateViewModel();
            ReportState = new ProgressStateViewModel();
            DeletionState = new ProgressStateViewModel();

            AnalysisState.Reset("Analisi");
            ReportState.Reset("Report");
            DeletionState.Reset("Cancellazione");

            LogMessages = new ObservableCollection<LogMessageViewModel>();
        }

        public ProgressStateViewModel AnalysisState { get; }

        public ProgressStateViewModel ReportState { get; }

        public ProgressStateViewModel DeletionState { get; }

        public ObservableCollection<LogMessageViewModel> LogMessages { get; }

        public void ClearLogs()
        {
            RunOnUiThread(() => LogMessages.Clear());
        }

        public void ResetAnalysis()
        {
            RunOnUiThread(() =>
            {
                _analysisProgress.Complete();
                _analysisProgress = new();
                AnalysisState.Reset("Analisi");
                AnalysisState.IsRunning = true;
                _progressTimer.Start();
            });
        }

        public void ResetReport()
        {
            RunOnUiThread(() =>
            {
                _reportProgress.Complete();
                _reportProgress = new();
                ReportState.Reset("Report");
                ReportState.IsRunning = true;
                _progressTimer.Start();
            });
        }

        public void ResetDeletion()
        {
            RunOnUiThread(() =>
            {
                _deletionProgress.Complete();
                _deletionProgress = new();
                DeletionState.Reset("Cancellazione");
                DeletionState.IsRunning = true;
                _progressTimer.Start();
            });
        }

        public void OnLog(ServiceLogMessage? message)
        {
            if (message == null)
            {
                return;
            }

            RunOnUiThread(() =>
            {
                LogMessages.Add(new LogMessageViewModel
                {
                    Timestamp = message.Timestamp,
                    Level = message.Level,
                    Message = message.Message,
                    ExceptionMessage = message.Exception?.Message ?? string.Empty
                });
            });
        }

        public void OnAnalysisProgress(AnalysisProgress? progress)
        {
            if (progress != null) _analysisProgress.Report(progress);
        }

        public void OnReportProgress(DuplicateReportProgress? progress)
        {
            if (progress != null) _reportProgress.Report(progress);
        }

        public void OnDeletionProgress(DeletionProgress? progress)
        {
            if (progress != null) _deletionProgress.Report(progress);
        }

        public IProgress<AnalysisProgress> CreateAnalysisProgress()
        {
            return _analysisProgress;
        }

        public IProgress<DuplicateReportProgress> CreateReportProgress()
        {
            return _reportProgress;
        }

        public IProgress<DeletionProgress> CreateDeletionProgress()
        {
            return _deletionProgress;
        }

        public void CompleteAnalysis() => RunOnUiThread(() =>
        {
            ApplyAnalysis(_analysisProgress.Complete());
            AnalysisState.IsRunning = false;
            StopTimerIfIdle();
        });

        public void CompleteReport() => RunOnUiThread(() =>
        {
            ApplyReport(_reportProgress.Complete());
            ReportState.IsRunning = false;
            StopTimerIfIdle();
        });

        public void CompleteDeletion() => RunOnUiThread(() =>
        {
            ApplyDeletion(_deletionProgress.Complete());
            DeletionState.IsRunning = false;
            StopTimerIfIdle();
        });

        private void DrainProgress(object? sender, EventArgs e)
        {
            ApplyAnalysis(_analysisProgress.TakeLatest());
            ApplyReport(_reportProgress.TakeLatest());
            ApplyDeletion(_deletionProgress.TakeLatest());
        }

        private void ApplyAnalysis(AnalysisProgress? progress)
        {
            if (progress == null) return;
            AnalysisState.ProcessedItems = progress.ProcessedFiles;
            AnalysisState.TotalItems = progress.TotalFiles;
            AnalysisState.Percentage = progress.Percentage;
            AnalysisState.CurrentItem = progress.CurrentFile ?? string.Empty;
            AnalysisState.CurrentStep = "Analisi file";
        }

        private void ApplyReport(DuplicateReportProgress? progress)
        {
            if (progress == null) return;
            ReportState.ProcessedItems = progress.ProcessedGroups;
            ReportState.TotalItems = progress.TotalGroups;
            ReportState.Percentage = progress.Percentage;
            ReportState.CurrentItem = string.Empty;
            ReportState.CurrentStep = progress.CurrentStep;
        }

        private void ApplyDeletion(DeletionProgress? progress)
        {
            if (progress == null) return;
            DeletionState.ProcessedItems = progress.ProcessedFiles;
            DeletionState.TotalItems = progress.TotalFiles;
            DeletionState.Percentage = progress.Percentage;
            DeletionState.CurrentItem = progress.CurrentFile ?? string.Empty;
            DeletionState.CurrentStep = "Cancellazione file";
        }

        private void StopTimerIfIdle()
        {
            if (!AnalysisState.IsRunning && !ReportState.IsRunning && !DeletionState.IsRunning)
                _progressTimer.Stop();
        }

        public void Dispose()
        {
            RunOnUiThread(() =>
            {
                _disposed = true;
                _progressTimer.Stop();
                _progressTimer.Tick -= DrainProgress;
                _analysisProgress.Complete();
                _reportProgress.Complete();
                _deletionProgress.Complete();
            });
        }

        private void RunOnUiThread(Action action)
        {
            if (_disposed) return;
            if (_dispatcher.CheckAccess()) action();
            else _dispatcher.Invoke(() => { if (!_disposed) action(); });
        }
    }
}
