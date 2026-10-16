using System.Runtime.ExceptionServices;
using System.IO;
using System.Windows.Threading;
using Foto_CreaDB2;
using FotoCreaDB.Wpf.ViewModels;

namespace FotoCreaDB.Tests.Wpf;

[Collection("Report UI filesystem")]
public class ReportViewModelRegressionTests
{
    [Fact]
    public void SelectedPhotoScope_FiltersReportAndInvalidatesSummaryWhenChanged()
    {
        WithViewModel((folder, vm) =>
        {
            vm.DatabasePath = CreateDatabase(folder, "photos.db", 2);
            vm.FotoPath = Path.Combine(folder, "0.jpg");
            Generate(vm);
            Assert.Equal(0, vm.LastFilesToDeleteCount);
            vm.FotoPath = folder;
            Assert.Null(vm.LastFilesToDeleteCount);
            Generate(vm);
            Assert.Equal(1, vm.LastFilesToDeleteCount);
        });
    }

    [Fact]
    public void Preferences_ReloadFromTheSameFileAfterChangingLaunchDirectory()
    {
        WithViewModel((folder, vm) =>
        {
            string config = Path.Combine(folder, "appsettings.json");
            Assert.True(AppConfigLoader.SaveToFile(new AppConfig { EstensioniPermesse = new() { "gif" } }, config));
            vm.FotoPath = Path.Combine(folder, "my-photos");
            vm.DatabasePath = Path.Combine(folder, "my-photos.db");
            vm.SavePreferences();
            string launch = Path.Combine(folder, "launch");
            Directory.CreateDirectory(launch);
            Directory.SetCurrentDirectory(launch);
            using var reloaded = new MainViewModel(config);
            Assert.Equal(vm.FotoPath, reloaded.FotoPath);
            Assert.Equal(vm.DatabasePath, reloaded.DatabasePath);
            Assert.Equal("gif", Assert.Single(AppConfigLoader.LoadConfiguration(config).EstensioniPermesse));
            reloaded.SavePreferences();
            Assert.False(File.Exists(Path.Combine(launch, "appsettings.json")));
        });
    }

    [Fact]
    public void FailedRefresh_DoesNotKeepPreviousReportNumbers()
    {
        WithViewModel((folder, vm) =>
        {
            string db = CreateDatabase(folder, "photos.db", 2);
            vm.DatabasePath = db;
            Generate(vm);
            Assert.Equal(1, vm.LastFilesToDeleteCount);
            File.WriteAllText(db, "invalid database");

            Generate(vm);

            Assert.Null(vm.LastDuplicateGroupsCount);
            Assert.Null(vm.LastFilesToDeleteCount);
            Assert.Equal("—", vm.LastFilesToDeleteSizeFormatted);
            Assert.Contains("Errore", vm.StatusMessage);
        });
    }

    [Fact]
    public void SwitchingDatabase_InvalidatesReportSummaryAndProgress()
    {
        WithViewModel((folder, vm) =>
        {
            vm.DatabasePath = CreateDatabase(folder, "first.db", 2);
            Generate(vm);
            Assert.Equal(100, vm.ReportState.Percentage);

            vm.DatabasePath = Path.Combine(folder, "other.db");

            Assert.Null(vm.LastFilesToDeleteCount);
            Assert.Equal("—", vm.LastFilesToDeleteSizeFormatted);
            Assert.Equal(0, vm.ReportState.Percentage);
        });
    }

    [Fact]
    public void DatabaseChangedDuringReport_DoesNotPublishResultsForAnotherDatabase()
    {
        WithViewModel((folder, vm) =>
        {
            string selected = CreateDatabase(folder, "first.db", 2);
            string other = CreateDatabase(folder, "other.db", 3);
            vm.DatabasePath = selected;
            vm.ReportCommand.Execute(null);
            Assert.True(vm.IsBusy);

            // Programmatic change while the dispatcher has not processed completion.
            vm.DatabasePath = other;
            PumpUntil(() => !vm.IsBusy);

            Assert.Null(vm.LastFilesToDeleteCount);
            Assert.Contains("Database cambiato", vm.StatusMessage);
            Assert.Equal(0, vm.ReportState.Percentage);
        });
    }

    [Fact]
    public void ValidEmptyReport_ShowsZeroInsteadOfUnavailableNumbers()
    {
        WithViewModel((folder, vm) =>
        {
            Assert.Null(vm.LastFilesToDeleteCount);
            vm.DatabasePath = CreateDatabase(folder, "empty.db", 0);

            Generate(vm);

            Assert.Equal(0, vm.LastFilesToDeleteCount);
            Assert.Equal(0, vm.LastDuplicateGroupsCount);
            Assert.Equal("0 B", vm.LastFilesToDeleteSizeFormatted);
        });
    }

    private static string CreateDatabase(string folder, string name, int rows)
    {
        string path = Path.Combine(folder, name);
        using var db = new DatabaseManager(path, false);
        db.Initialize();
        using var repository = new FotoRepository(db.Connection!);
        for (int i = 0; i < rows; i++)
            repository.Insert(new Foto { percorsoCompleto = Path.Combine(folder, i + ".jpg"), hashSha256 = new string('a', 64), dimensione = 100 });
        return path;
    }

    private static void Generate(MainViewModel vm)
    {
        vm.ReportCommand.Execute(null);
        PumpUntil(() => !vm.IsBusy);
    }

    internal static void WithViewModel(Action<string, MainViewModel> test)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            string folder = Path.Combine(Path.GetTempPath(), "FotoCreaDB_ReportUi_" + Guid.NewGuid().ToString("N"));
            string previousDirectory = Directory.GetCurrentDirectory();
            Directory.CreateDirectory(folder);
            try
            {
                // MainViewModel saves preferences after a successful report.
                Directory.SetCurrentDirectory(folder);
                using var vm = new MainViewModel(Path.Combine(folder, "appsettings.json"));
                test(folder, vm);
            }
            catch (Exception ex) { error = ex; }
            finally
            {
                Directory.SetCurrentDirectory(previousDirectory);
                Directory.Delete(folder, true);
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Report UI test timed out.");
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }

    internal static void PumpUntil(Func<bool> ready)
    {
        var frame = new DispatcherFrame();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => { if (ready() || elapsed.Elapsed > TimeSpan.FromSeconds(5)) frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); }
        Assert.True(ready(), "Report did not complete.");
    }
}

[CollectionDefinition("Report UI filesystem", DisableParallelization = true)]
public class ReportUiFilesystemCollection;
