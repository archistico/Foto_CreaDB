using System.IO;
using Foto_CreaDB2;
using static FotoCreaDB.Tests.Wpf.ReportViewModelRegressionTests;

namespace FotoCreaDB.Tests.Wpf;

[Collection("Report UI filesystem")]
public class AnalysisViewModelRegressionTests
{
    [Fact]
    public void InvalidConfiguration_StopsAnalysisBeforeOpeningDatabaseAndIsPreserved()
    {
        WithViewModel((folder, vm) =>
        {
            string config = Path.Combine(folder, "appsettings.json");
            File.WriteAllText(config, "{broken");
            vm.FotoPath = Path.Combine(folder, "photo.jpg");
            File.WriteAllText(vm.FotoPath, "photo");
            vm.DatabasePath = Path.Combine(folder, "must-not-exist.db");
            vm.AnalyzeCommand.Execute(null);
            PumpUntil(() => !vm.IsBusy);
            Assert.Contains("Errore", vm.StatusMessage);
            Assert.False(File.Exists(vm.DatabasePath));
            Assert.Equal("{broken", File.ReadAllText(config));
        });
    }

    [Fact]
    public void Analyze_SnapshotsPathsBeforePublishingBusyState()
    {
        WithViewModel((folder, vm) =>
        {
            string selected = Path.Combine(folder, "selected.jpg");
            string other = Path.Combine(folder, "other.jpg");
            string selectedDb = Path.Combine(folder, "selected.db");
            string otherDb = Path.Combine(folder, "other.db");
            File.WriteAllText(selected, "selected");
            File.WriteAllText(other, "other");
            vm.FotoPath = selected;
            vm.DatabasePath = selectedDb;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(vm.IsBusy) || !vm.IsBusy) return;
                vm.FotoPath = other;
                vm.DatabasePath = otherDb;
            };

            vm.AnalyzeCommand.Execute(null);
            PumpUntil(() => !vm.IsBusy);

            Assert.True(File.Exists(selectedDb));
            Assert.False(File.Exists(otherDb));
            using var db = new DatabaseManager(selectedDb, false);
            db.Initialize();
            using var repository = new FotoRepository(db.Connection!);
            Assert.NotNull(repository.GetByPercorso(selected));
            Assert.Null(repository.GetByPercorso(other));
            Assert.False(vm.AnalysisState.IsRunning);
        });
    }

    [Fact]
    public void Analyze_RejectsReentryWhileAnotherOperationIsRunning()
    {
        WithViewModel((folder, vm) =>
        {
            string photo = Path.Combine(folder, "selected.jpg");
            string db = Path.Combine(folder, "photos.db");
            File.WriteAllText(photo, "photo");
            vm.FotoPath = photo;
            vm.DatabasePath = db;
            vm.IsBusy = true;
            // ICommand.Execute can be called directly without consulting CanExecute.
            vm.AnalyzeCommand.Execute(null);
            // Drain an incorrectly started operation before disposing the fixture.
            if (vm.StatusMessage != "Pronto") PumpUntil(() => !vm.IsBusy);

            Assert.Equal("Pronto", vm.StatusMessage);
            Assert.False(File.Exists(db));
            Assert.True(vm.IsBusy);
            Assert.False(vm.AnalysisState.IsRunning);
        });
    }
}
