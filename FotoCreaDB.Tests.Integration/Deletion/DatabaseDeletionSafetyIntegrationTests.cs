using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;

namespace FotoCreaDB.Tests.Integration.Deletion;

public class DatabaseDeletionSafetyIntegrationTests
{
    [Fact]
    public void DeleteDatabaseFiles_WhenPathPointsToPhoto_LeavesPhotoAndSidecarsUntouched()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("photo.jpg");
        TestImageFactory.CreateJpegImage(photo);
        byte[] original = File.ReadAllBytes(photo);
        File.WriteAllText(photo + "-wal", "unrelated file");

        Assert.False(DatabaseFileManager.DeleteDatabaseFiles(photo));
        Assert.Equal(original, File.ReadAllBytes(photo));
        Assert.Equal("unrelated file", File.ReadAllText(photo + "-wal"));
    }

    [Fact]
    public void Initialize_WhenResetPathPointsToPhoto_RefusesToDeleteIt()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("photo.jpg");
        TestImageFactory.CreateJpegImage(photo);
        byte[] original = File.ReadAllBytes(photo);
        using var manager = new DatabaseManager(photo, true);

        Assert.ThrowsAny<Exception>(() => manager.Initialize());
        Assert.Equal(original, File.ReadAllBytes(photo));
    }

    [Fact]
    public void DeleteDatabaseFiles_WhenDatabaseIsInUse_LeavesDatabaseAndSidecarsUntouched()
    {
        using TemporaryTestFolder folder = new();
        string db = folder.GetPath("test.db");
        using (var manager = new DatabaseManager(db, false)) manager.Initialize();
        File.WriteAllText(db + "-wal", "sidecar");
        using var reader = File.OpenRead(db);

        Assert.False(DatabaseFileManager.DeleteDatabaseFiles(db));
        Assert.True(File.Exists(db));
        Assert.Equal("sidecar", File.ReadAllText(db + "-wal"));
    }

    [Fact]
    public void Initialize_WithSemicolonInFileName_UsesExactPath()
    {
        using TemporaryTestFolder folder = new();
        string db = folder.GetPath("photos;2026.db");
        using var manager = new DatabaseManager(db, false);
        manager.Initialize();

        Assert.True(File.Exists(db));
        Assert.False(File.Exists(folder.GetPath("photos")));
    }

    [Fact]
    public void Initialize_WithReset_RecreatesValidDatabase()
    {
        using TemporaryTestFolder folder = new();
        string db = folder.GetPath("test.db");
        using (var manager = new DatabaseManager(db, false))
        {
            manager.Initialize();
            using var repository = new FotoRepository(manager.Connection!);
            repository.Insert(new Foto { percorsoCompleto = folder.GetPath("photo.jpg") });
        }
        using var reset = new DatabaseManager(db, true);
        reset.Initialize();
        using var command = reset.Connection!.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM FOTO";
        Assert.Equal(0L, (long)command.ExecuteScalar());
    }
}
