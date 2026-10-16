using Foto_CreaDB2;
using FotoCreaDB.Tests.Integration.TestHelpers;

namespace FotoCreaDB.Tests.Integration.Deletion;

public class ConfiguredDeletionScopeTests
{
    [Fact]
    public void NarrowingExtensions_ExcludesExistingRecordsFromReportAndDeletion()
    {
        using TemporaryTestFolder folder = new();
        string first = Photo(folder, "first.jpg");
        string second = Photo(folder, "second.jpg");
        string document = Photo(folder, "long-excluded-document.pdf");
        string otherDocument = Photo(folder, "another-excluded-document.pdf");
        string db = Scan(folder, Allowed("jpg", "pdf"));
        var allowed = Allowed("jpg");

        var report = new DuplicateReportService().Run(new() { NomeDb = db, Configuration = allowed });
        Assert.Equal(1, report.FilesToDeleteCount);
        Assert.All(report.Decisions.SelectMany(d => d.FileDaEliminare.Append(d.FileDaTenere)),
            item => Assert.EndsWith(".jpg", item.PercorsoCompleto));
        var result = new DuplicateDeletionService().Run(new() { NomeDb = db, Configuration = allowed });

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesDeletedCount);
        Assert.Equal(0, result.FilesBlockedCount);
        Assert.True(File.Exists(document));
        Assert.True(File.Exists(otherDocument));
        Assert.True(File.Exists(first) || File.Exists(second));
        using var manager = new DatabaseManager(db, false);
        manager.Initialize();
        using var repository = new FotoRepository(manager.Connection!);
        Assert.True(repository.GetByPercorso(document)!.FileEsiste);
    }

    [Fact]
    public void ExcludedFile_CannotBeUsedAsTheOnlyRetainedCopy()
    {
        using TemporaryTestFolder folder = new();
        string photo = Photo(folder, "photo.jpg");
        string document = Photo(folder, "longer-name-document.pdf");
        string db = Scan(folder, Allowed("jpg", "pdf"));
        var result = new DuplicateDeletionService().Run(new() { NomeDb = db, Configuration = Allowed("jpg") });
        Assert.True(result.Success);
        Assert.Equal(0, result.FilesToDeleteCount);
        Assert.True(File.Exists(photo));
        Assert.True(File.Exists(document));
    }

    [Fact]
    public void SelectedDirectory_DoesNotIncludeSiblingWithTheSamePrefix()
    {
        using TemporaryTestFolder folder = new();
        string selected = folder.GetPath("album");
        string sibling = folder.GetPath("album-other");
        Directory.CreateDirectory(selected);
        Directory.CreateDirectory(sibling);
        Photo(folder, Path.Combine("album", "first.jpg"));
        Photo(folder, Path.Combine("album", "second.jpg"));
        string outside = Photo(folder, Path.Combine("album-other", "first.jpg"));
        string outsideSecond = Photo(folder, Path.Combine("album-other", "second.jpg"));
        string db = Scan(folder, Allowed("jpg"));
        var report = new DuplicateReportService().Run(new() { NomeDb = db, Paths = new[] { selected }, Configuration = Allowed("jpg") });
        Assert.Equal(1, report.FilesToDeleteCount);
        var result = new DuplicateDeletionService().Run(new() { NomeDb = db, Paths = new[] { selected }, Configuration = Allowed("jpg") });
        Assert.True(result.Success);
        Assert.Equal(1, result.FilesDeletedCount);
        Assert.True(File.Exists(outside));
        Assert.True(File.Exists(outsideSecond));
    }

    [Fact]
    public void EmptyAllowedExtensions_PreserveAllFilesAndRecords()
    {
        using TemporaryTestFolder folder = new();
        string first = Photo(folder, "first.jpg");
        string second = Photo(folder, "second.jpg");
        string db = Scan(folder, Allowed("jpg"));
        var result = new DuplicateDeletionService().Run(new() { NomeDb = db, Configuration = Allowed() });
        Assert.True(result.Success);
        Assert.Equal(0, result.FilesToDeleteCount);
        Assert.Equal(0, result.DatabaseRecordsDeletedCount);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(second));
    }

    [Fact]
    public void RequestMutationDuringCallbacks_CannotWidenScopeOrChangeDatabase()
    {
        using TemporaryTestFolder folder = new();
        string first = Photo(folder, "first.jpg");
        string document = Photo(folder, "longer-document.pdf");
        string db = Scan(folder, Allowed("jpg", "pdf"));
        var request = new DeletionRequest { NomeDb = db, Configuration = Allowed("jpg"), Paths = new[] { first } };
        var result = new DuplicateDeletionService().Run(request, log: _ =>
        {
            request.Configuration.EstensioniPermesse.Add("pdf");
            request.Paths[0] = folder.FolderPath;
            request.NomeDb = folder.GetPath("other.db");
        });
        Assert.Equal(0, result.FilesDeletedCount);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(document));
        Assert.False(File.Exists(folder.GetPath("other.db")));
    }

    internal static AppConfig Allowed(params string[] extensions) => new()
        { EstensioniPermesse = new(extensions, StringComparer.OrdinalIgnoreCase) };

    internal static string Photo(TemporaryTestFolder folder, string name)
    {
        string path = folder.GetPath(name);
        TestImageFactory.CreateJpegImage(path);
        return path;
    }

    internal static string Scan(TemporaryTestFolder folder, AppConfig config)
    {
        string db = folder.GetPath("photos.db");
        Assert.True(new AnalysisService().Run(new() { NomeDb = db, Paths = new[] { folder.FolderPath }, Configuration = config }).Success);
        return db;
    }
}
