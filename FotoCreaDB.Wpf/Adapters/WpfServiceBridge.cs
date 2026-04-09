using Foto_CreaDB2;
using FotoCreaDB.Wpf.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace FotoCreaDB.Wpf.Adapters
{
    public class WpfServiceBridge
    {
        public WpfServiceBridge()
        {
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
            RunOnUiThread(() => AnalysisState.Reset("Analisi"));
        }

        public void ResetReport()
        {
            RunOnUiThread(() => ReportState.Reset("Report"));
        }

        public void ResetDeletion()
        {
            RunOnUiThread(() => DeletionState.Reset("Cancellazione"));
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
            if (progress == null)
            {
                return;
            }

            RunOnUiThread(() =>
            {
                AnalysisState.IsRunning = progress.ProcessedFiles < progress.TotalFiles || progress.TotalFiles == 0;
                AnalysisState.ProcessedItems = progress.ProcessedFiles;
                AnalysisState.TotalItems = progress.TotalFiles;
                AnalysisState.Percentage = progress.Percentage;
                AnalysisState.CurrentItem = progress.CurrentFile ?? string.Empty;
                AnalysisState.CurrentStep = "Analisi file";
            });
        }

        public void OnReportProgress(DuplicateReportProgress? progress)
        {
            if (progress == null)
            {
                return;
            }

            RunOnUiThread(() =>
            {
                ReportState.IsRunning = progress.ProcessedGroups < progress.TotalGroups || progress.TotalGroups == 0;
                ReportState.ProcessedItems = progress.ProcessedGroups;
                ReportState.TotalItems = progress.TotalGroups;
                ReportState.Percentage = progress.Percentage;
                ReportState.CurrentItem = string.Empty;
                ReportState.CurrentStep = progress.CurrentStep ?? string.Empty;
            });
        }

        public void OnDeletionProgress(DeletionProgress? progress)
        {
            if (progress == null)
            {
                return;
            }

            RunOnUiThread(() =>
            {
                DeletionState.IsRunning = progress.ProcessedFiles < progress.TotalFiles || progress.TotalFiles == 0;
                DeletionState.ProcessedItems = progress.ProcessedFiles;
                DeletionState.TotalItems = progress.TotalFiles;
                DeletionState.Percentage = progress.Percentage;
                DeletionState.CurrentItem = progress.CurrentFile ?? string.Empty;
                DeletionState.CurrentStep = "Cancellazione file";
            });
        }

        public IProgress<AnalysisProgress> CreateAnalysisProgress()
        {
            return new Progress<AnalysisProgress>(OnAnalysisProgress);
        }

        public IProgress<DuplicateReportProgress> CreateReportProgress()
        {
            return new Progress<DuplicateReportProgress>(OnReportProgress);
        }

        public IProgress<DeletionProgress> CreateDeletionProgress()
        {
            return new Progress<DeletionProgress>(OnDeletionProgress);
        }

        private static void RunOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            DispatcherObject? dispatcherObject = System.Windows.Application.Current;

            if (dispatcherObject == null || dispatcherObject.Dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcherObject.Dispatcher.Invoke(action);
        }
    }
}