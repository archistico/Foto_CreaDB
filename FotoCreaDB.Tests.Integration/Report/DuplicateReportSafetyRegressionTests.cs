using System.Data.SQLite;
using Foto_CreaDB2;
using FotoCreaDB.Tests.Integration.TestHelpers;

namespace FotoCreaDB.Tests.Integration.Report;

public class DuplicateReportSafetyRegressionTests
{
    [Fact]
    public void Run_WithMissingDatabase_RejectsWithoutCreatingFileOrDirectory()
    {
        using TemporaryTestFolder folder = new();
        string directory = folder.GetPath("missing");
        string db = Path.Combine(directory, "wrong.db");

        Assert.ThrowsAny<Exception>(() => Report(db));
        Assert.False(Directory.Exists(directory));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Run_WithEmptyFileOrUnrelatedDatabase_RejectsWithoutModifyingIt(bool unrelatedDatabase)
    {
        using TemporaryTestFolder folder = new();
        string db = folder.GetPath("unrelated.db");
        if (unrelatedDatabase)
        {
            using var connection = new SQLiteConnection(new SQLiteConnectionStringBuilder { DataSource = db }.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE IMPORTANT (VALUE TEXT); INSERT INTO IMPORTANT VALUES ('keep');";
            command.ExecuteNonQuery();
        }
        else File.WriteAllBytes(db, Array.Empty<byte>());
        byte[] original = File.ReadAllBytes(db);

        Assert.ThrowsAny<Exception>(() => Report(db));

        Assert.Equal(original, File.ReadAllBytes(db));
    }

    [Fact]
    public void Run_ReportsCompletionOnlyAfterDetailedLogHasFinished()
    {
        using TemporaryTestFolder folder = new();
        string db = CreateDatabase(folder);
        int detailRows = 0;
        bool completed = false;
        var progress = new SynchronousProgress(value =>
        {
            if (value.CurrentStep != "Report completato") return;
            Assert.Equal(1, detailRows);
            completed = true;
        });

        new DuplicateReportService().Run(new DuplicateReportRequest { NomeDb = db, VerboseDuplicates = true }, progress, message =>
        {
            if (message.Message.TrimStart().StartsWith("Cancellare (", StringComparison.Ordinal)) detailRows++;
        });

        Assert.True(completed);
    }

    [Fact]
    public void Run_WithValidDatabase_DoesNotChangeItsContents()
    {
        using TemporaryTestFolder folder = new();
        string db = CreateDatabase(folder);
        byte[] before = File.ReadAllBytes(db);

        var result = Report(db);

        Assert.Equal(1, result.FilesToDeleteCount);
        Assert.Equal(before, File.ReadAllBytes(db));
    }

    private static DuplicateReportResult Report(string db) =>
        new DuplicateReportService().Run(new DuplicateReportRequest { NomeDb = db });

    [Fact]
    public void OpenExistingReadOnly_RejectsDatabaseWrites()
    {
        using TemporaryTestFolder folder = new();
        string path = CreateDatabase(folder);
        using var manager = new DatabaseManager(path, false);
        manager.OpenExistingReadOnly();
        using var command = manager.Connection!.CreateCommand();
        command.CommandText = "DELETE FROM FOTO";

        Assert.Throws<SQLiteException>(() => command.ExecuteNonQuery());
        command.CommandText = "SELECT COUNT(*) FROM FOTO";
        Assert.Equal(2L, command.ExecuteScalar());
    }

    [Fact]
    public void Run_WithLegacySchema_RejectsWithoutMigrating()
    {
        using TemporaryTestFolder folder = new();
        string path = CreateDatabase(folder);
        using (var manager = new DatabaseManager(path, false))
        {
            manager.Initialize();
            using var command = manager.Connection!.CreateCommand();
            command.CommandText = "DROP INDEX IDX_FOTO_FILE_ESISTE; ALTER TABLE FOTO DROP COLUMN FILE_ESISTE";
            command.ExecuteNonQuery();
        }
        byte[] before = File.ReadAllBytes(path);

        var error = Assert.Throws<InvalidDataException>(() => Report(path));

        Assert.Contains("analisi", error.Message);
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void Run_WhenDetailedLogFails_DoesNotSilentlyDeclareCompletion()
    {
        using TemporaryTestFolder folder = new();
        string path = CreateDatabase(folder);
        bool completed = false;
        var progress = new SynchronousProgress(p => completed |= p.CurrentStep == "Report completato");

        Assert.Throws<InvalidOperationException>(() => new DuplicateReportService().Run(
            new DuplicateReportRequest { NomeDb = path, VerboseDuplicates = true }, progress, message =>
            {
                if (message.Message.StartsWith("Tenere (", StringComparison.Ordinal))
                    throw new InvalidOperationException("Log unavailable");
            }));
        Assert.False(completed);
    }

    private static string CreateDatabase(TemporaryTestFolder folder)
    {
        string path = folder.GetPath("test.db");
        using var db = new DatabaseManager(path, false);
        db.Initialize();
        using var repository = new FotoRepository(db.Connection!);
        foreach (string name in new[] { "a.jpg", "b.jpg" })
            repository.Insert(new Foto
            {
                percorsoCompleto = folder.GetPath(name), hashSha256 = new string('a', 64), dimensione = 100
            });
        return path;
    }

    private sealed class SynchronousProgress(Action<DuplicateReportProgress> callback) : IProgress<DuplicateReportProgress>
    {
        public void Report(DuplicateReportProgress value) => callback(value);
    }
}
