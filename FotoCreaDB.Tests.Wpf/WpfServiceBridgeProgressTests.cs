using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using Foto_CreaDB2;
using FotoCreaDB.Wpf.Adapters;

namespace FotoCreaDB.Tests.Wpf;

public class WpfServiceBridgeProgressTests
{
    [Fact]
    public void AnalysisBurstOf150000Reports_DoesNotQueuePerFileUiWork()
    {
        OnSta(() =>
        {
            using var bridge = new WpfServiceBridge();
            bridge.ResetAnalysis();
            var progress = bridge.CreateAnalysisProgress();
            int posted = 0;
            int publications = 0;
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            DispatcherHookEventHandler hook = (_, _) => Interlocked.Increment(ref posted);
            bridge.AnalysisState.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "ProcessedItems") publications++;
                Assert.True(dispatcher.CheckAccess());
            };
            dispatcher.Hooks.OperationPosted += hook;
            try
            {
                // Keep the dispatcher unpumped during the burst. A per-file
                // synchronous UI dispatch would deadlock this worker.
                Exception? workerError = null;
                var worker = new Thread(() =>
                {
                    try
                    {
                        for (int i = 1; i <= 150_000; i++)
                            progress.Report(new AnalysisProgress { ProcessedFiles = i, TotalFiles = 150_000, CurrentFile = "last.jpg" });
                    }
                    catch (Exception ex) { workerError = ex; }
                }) { IsBackground = true };
                worker.Start();
                Assert.True(worker.Join(TimeSpan.FromSeconds(5)));
                if (workerError != null) ExceptionDispatchInfo.Capture(workerError).Throw();
                Assert.InRange(posted, 0, 2);
                Assert.Equal(0, publications);
                Assert.Equal(0, bridge.AnalysisState.ProcessedItems);

                PumpUntil(() => bridge.AnalysisState.ProcessedItems == 150_000);

                Assert.Equal(1, publications);
                Assert.Equal(100, bridge.AnalysisState.Percentage);
                Assert.Equal("last.jpg", bridge.AnalysisState.CurrentItem);
                // The operation, not its counters, determines when it has ended.
                Assert.True(bridge.AnalysisState.IsRunning);
                bridge.CompleteAnalysis();
                Assert.False(bridge.AnalysisState.IsRunning);
            }
            finally { dispatcher.Hooks.OperationPosted -= hook; }
        });
    }

    [Fact]
    public void Complete_FlushesPartialProgressImmediatelyAndRejectsLateReports()
    {
        OnSta(() =>
        {
            using var bridge = new WpfServiceBridge();
            bridge.ResetAnalysis();
            var progress = bridge.CreateAnalysisProgress();
            progress.Report(new AnalysisProgress { ProcessedFiles = 42, TotalFiles = 100, CurrentFile = "partial.jpg" });

            // Same completion path as a failed operation in MainViewModel.finally.
            bridge.CompleteAnalysis();

            Assert.Equal(42, bridge.AnalysisState.ProcessedItems);
            Assert.Equal("partial.jpg", bridge.AnalysisState.CurrentItem);
            Assert.False(bridge.AnalysisState.IsRunning);
            progress.Report(new AnalysisProgress { ProcessedFiles = 99, TotalFiles = 100 });
            PumpFor(TimeSpan.FromMilliseconds(350));
            Assert.Equal(42, bridge.AnalysisState.ProcessedItems);
            Assert.False(bridge.AnalysisState.IsRunning);
        });
    }

    [Fact]
    public void Reset_IgnoresReportsFromPreviousOperation()
    {
        OnSta(() =>
        {
            using var bridge = new WpfServiceBridge();
            bridge.ResetAnalysis();
            var previous = bridge.CreateAnalysisProgress();
            previous.Report(new AnalysisProgress { ProcessedFiles = 90, TotalFiles = 100 });
            bridge.ResetAnalysis();
            var current = bridge.CreateAnalysisProgress();
            current.Report(new AnalysisProgress { ProcessedFiles = 5, TotalFiles = 10 });
            previous.Report(new AnalysisProgress { ProcessedFiles = 100, TotalFiles = 100 });

            bridge.CompleteAnalysis();

            Assert.Equal(5, bridge.AnalysisState.ProcessedItems);
            Assert.Equal(10, bridge.AnalysisState.TotalItems);
            Assert.Equal(50, bridge.AnalysisState.Percentage);
        });
    }

    [Fact]
    public void Timer_UpdatesAllOperationsAndCompletesZeroTotals()
    {
        OnSta(() =>
        {
            using var bridge = new WpfServiceBridge();
            bridge.ResetAnalysis();
            bridge.ResetReport();
            bridge.ResetDeletion();
            bridge.CreateAnalysisProgress().Report(new AnalysisProgress { TotalFiles = 100, ProcessedFiles = 25 });
            bridge.CreateReportProgress().Report(new DuplicateReportProgress
            {
                TotalGroups = 20, ProcessedGroups = 10, CurrentStep = "Reading groups"
            });
            bridge.CreateDeletionProgress().Report(new DeletionProgress { TotalFiles = 40, ProcessedFiles = 30 });

            PumpUntil(() => bridge.DeletionState.ProcessedItems == 30);

            Assert.Equal(25, bridge.AnalysisState.Percentage);
            Assert.Equal(50, bridge.ReportState.Percentage);
            Assert.Equal("Reading groups", bridge.ReportState.CurrentStep);
            Assert.Equal(75, bridge.DeletionState.Percentage);
            bridge.CreateReportProgress().Report(new DuplicateReportProgress
            {
                TotalGroups = 20, ProcessedGroups = 20, CurrentStep = "Completed"
            });
            bridge.CreateDeletionProgress().Report(new DeletionProgress { TotalFiles = 40, ProcessedFiles = 40 });
            bridge.CompleteAnalysis();
            bridge.CompleteReport();
            bridge.CompleteDeletion();
            Assert.Equal(20, bridge.ReportState.ProcessedItems);
            Assert.Equal("Completed", bridge.ReportState.CurrentStep);
            Assert.Equal(40, bridge.DeletionState.ProcessedItems);
            Assert.False(bridge.ReportState.IsRunning);
            Assert.False(bridge.DeletionState.IsRunning);
            bridge.ResetDeletion();
            bridge.CreateDeletionProgress().Report(new DeletionProgress { TotalFiles = 0, ProcessedFiles = 0 });
            bridge.CompleteDeletion();
            Assert.Equal(0, bridge.DeletionState.ProcessedItems);
            Assert.False(bridge.DeletionState.IsRunning);
        });
    }

    [Fact]
    public void Dispose_DropsPendingAndLateUpdates()
    {
        OnSta(() =>
        {
            var bridge = new WpfServiceBridge();
            bridge.ResetAnalysis();
            var progress = bridge.CreateAnalysisProgress();
            progress.Report(new AnalysisProgress { ProcessedFiles = 1, TotalFiles = 10 });

            bridge.Dispose();
            progress.Report(new AnalysisProgress { ProcessedFiles = 2, TotalFiles = 10 });
            bridge.OnLog(new ServiceLogMessage { Message = "after close" });
            bridge.Dispose();
            PumpFor(TimeSpan.FromMilliseconds(350));

            Assert.Equal(0, bridge.AnalysisState.ProcessedItems);
            Assert.Empty(bridge.LogMessages);
        });
    }

    private static void OnSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "WPF dispatcher test timed out.");
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }

    private static void PumpUntil(Func<bool> ready)
    {
        var frame = new DispatcherFrame();
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) =>
        {
            if (ready() || timeout.Elapsed > TimeSpan.FromSeconds(2)) frame.Continue = false;
        };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); }
        Assert.True(ready(), "Progress was not published by the UI timer.");
    }

    private static void PumpFor(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = duration };
        timer.Tick += (_, _) => frame.Continue = false;
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); }
    }
}
