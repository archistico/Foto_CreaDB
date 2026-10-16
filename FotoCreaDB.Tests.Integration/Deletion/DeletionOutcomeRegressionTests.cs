using Foto_CreaDB2;
using FotoCreaDB.Tests.Integration.TestHelpers;
using static FotoCreaDB.Tests.Integration.Deletion.ConfiguredDeletionScopeTests;

namespace FotoCreaDB.Tests.Integration.Deletion;

public class DeletionOutcomeRegressionTests
{
    [Fact]
    public void PartialDeletion_ReportsDeletedAndBlockedFilesSeparately()
    {
        using TemporaryTestFolder folder = new();
        string keeper = Photo(folder, "keeper-with-longest-name.jpg");
        string modified = Photo(folder, "modified.jpg");
        string good = Photo(folder, "good.jpg");
        string db = Scan(folder, Allowed("jpg"));
        File.WriteAllText(modified, "changed after scan");
        var logs = new List<ServiceLogMessage>();

        var result = new DuplicateDeletionService().Run(new() { NomeDb = db, Configuration = Allowed("jpg") }, log: logs.Add);

        Assert.False(result.Success);
        Assert.Equal(2, result.FilesToDeleteCount);
        Assert.Equal(1, result.FilesDeletedCount);
        Assert.Equal(1, result.FilesBlockedCount);
        Assert.Equal(1, result.DatabaseRecordsDeletedCount);
        Assert.Equal(0, result.DatabaseErrorsCount);
        Assert.Contains("incompleta", result.Message);
        Assert.Equal(ServiceLogLevel.Warning, logs[^1].Level);
        Assert.True(File.Exists(keeper));
        Assert.True(File.Exists(modified));
        Assert.False(File.Exists(good));
    }

    [Fact]
    public void DatabaseCleanupFailure_DoesNotReportSuccessfulDeletion()
    {
        using TemporaryTestFolder folder = new();
        Photo(folder, "keeper-with-longest-name.jpg");
        string candidate = Photo(folder, "copy.jpg");
        string db = Scan(folder, Allowed("jpg"));
        using (var manager = new DatabaseManager(db, false))
        {
            manager.Initialize();
            using var command = manager.Connection!.CreateCommand();
            command.CommandText = "CREATE TRIGGER deny_delete BEFORE DELETE ON FOTO BEGIN SELECT RAISE(ABORT, 'test cleanup failure'); END";
            command.ExecuteNonQuery();
        }

        var result = new DuplicateDeletionService().Run(new() { NomeDb = db, Configuration = Allowed("jpg") });

        Assert.False(result.Success);
        Assert.Equal(1, result.FilesDeletedCount);
        Assert.Equal(0, result.FilesBlockedCount);
        Assert.Equal(0, result.DatabaseRecordsDeletedCount);
        Assert.Equal(1, result.DatabaseErrorsCount);
        Assert.False(File.Exists(candidate));
        using var after = new DatabaseManager(db, false);
        after.Initialize();
        using var repository = new FotoRepository(after.Connection!);
        Assert.NotNull(repository.GetByPercorso(candidate));
    }
}
