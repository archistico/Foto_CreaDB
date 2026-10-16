using System.Diagnostics;
using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;

namespace FotoCreaDB.Tests.Integration.Analysis;

public class IncrementalScanRegressionTests
{
    [Theory]
    [InlineData("album_2026", "albumX2026")]
    [InlineData("album%2026", "albumX2026")]
    public void MarkMissing_WithSqlPatternCharacters_DoesNotChangeSibling(string rootName, string siblingName)
    {
        using TemporaryTestFolder folder = new();
        string root = folder.GetPath(rootName);
        Directory.CreateDirectory(root);
        string missing = Path.Combine(root, "missing.jpg");
        string sibling = Path.Combine(folder.GetPath(siblingName), "other.jpg");
        using var db = OpenDatabase(folder);
        using var repository = new FotoRepository(db.Connection!);
        repository.Insert(new Foto { percorsoCompleto = missing, dataScansione = "previous" });
        repository.Insert(new Foto { percorsoCompleto = sibling, dataScansione = "previous" });

        Assert.Equal(1, repository.MarkMissingFilesForRoot(root, "current"));
        Assert.False(repository.GetByPercorso(missing)!.FileEsiste);
        Assert.True(repository.GetByPercorso(sibling)!.FileEsiste);
    }

    [Fact]
    public void MarkMissing_WhenFileWasNotVisitedButStillExists_PreservesItsState()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("excluded.jpg");
        File.WriteAllText(photo, "photo");
        using var db = OpenDatabase(folder);
        using var repository = new FotoRepository(db.Connection!);
        repository.Insert(new Foto { percorsoCompleto = photo, dataScansione = "previous" });

        Assert.Equal(0, repository.MarkMissingFilesForRoot(folder.FolderPath, "current"));
        Assert.True(repository.GetByPercorso(photo)!.FileEsiste);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MarkMissing_WithEmptyRoot_RejectsOperation(string root)
    {
        using TemporaryTestFolder folder = new();
        using var db = OpenDatabase(folder);
        using var repository = new FotoRepository(db.Connection!);
        string photo = folder.GetPath("missing.jpg");
        repository.Insert(new Foto { percorsoCompleto = photo });

        Assert.Throws<ArgumentException>(() => repository.MarkMissingFilesForRoot(root, "current"));
        Assert.True(repository.GetByPercorso(photo)!.FileEsiste);
    }

    [Fact]
    public void Run_AfterTemporaryHashFailure_RetriesUnchangedFile()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("photo.jpg");
        TestImageFactory.CreateJpegImage(photo);
        var request = new AnalysisRequest { Paths = new[] { photo }, NomeDb = folder.GetPath("test.db") };
        using (var writer = new FileStream(photo, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            Assert.False(new AnalysisService().Run(request).Success);
        }
        using (var firstDb = OpenDatabase(folder))
        using (var repository = new FotoRepository(firstDb.Connection!))
            Assert.Equal("", repository.GetByPercorso(photo)!.HashSha256);

        var result = new AnalysisService().Run(request);

        using var db = OpenDatabase(folder);
        using var after = new FotoRepository(db.Connection!);
        Assert.NotEmpty(after.GetByPercorso(photo)!.HashSha256);
        Assert.Equal(0, result.Statistics.TotaleFileSaltati);
        Assert.True(result.Success);
        Assert.Equal(1, new AnalysisService().Run(request).Statistics.TotaleFileSaltati);
    }

    [Fact]
    public void Run_WithMissingRoot_DoesNotReportSuccess()
    {
        using TemporaryTestFolder folder = new();
        var result = new AnalysisService().Run(new AnalysisRequest
        {
            Paths = new[] { folder.GetPath("nonexistent") }, NomeDb = folder.GetPath("test.db")
        });

        Assert.False(result.Success);
        Assert.Contains("error", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("missing")]
    [InlineData("junction")]
    public void Run_WithInvalidInputAndDatabaseReset_PreservesExistingDatabase(string input)
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("photo.jpg");
        using (var db = OpenDatabase(folder))
        using (var repository = new FotoRepository(db.Connection!))
            repository.Insert(new Foto { percorsoCompleto = photo });
        string invalidPath = folder.GetPath(input);
        if (input == "junction") CreateJunction(invalidPath, folder.FolderPath);

        Assert.Throws<ArgumentException>(() => new AnalysisService().Run(new AnalysisRequest
        {
            Paths = input == "empty" ? Array.Empty<string>() : new[] { invalidPath },
            NomeDb = folder.GetPath("test.db"), CancellaDbSeEsiste = true
        }));

        using var after = OpenDatabase(folder);
        using var afterRepository = new FotoRepository(after.Connection!);
        Assert.NotNull(afterRepository.GetByPercorso(photo));
    }

    [Fact]
    public void Run_WithDirectoryJunction_DoesNotScanOutsideSelectedRoot()
    {
        using TemporaryTestFolder root = new();
        using TemporaryTestFolder outside = new();
        string outsidePhoto = outside.GetPath("photo.jpg");
        TestImageFactory.CreateJpegImage(outsidePhoto);
        string link = root.GetPath("linked");
        CreateJunction(link, outside.FolderPath);

        var result = new AnalysisService().Run(new AnalysisRequest
        {
            Paths = new[] { root.FolderPath }, NomeDb = root.GetPath("test.db")
        });

        Assert.Equal(0, result.Statistics.TotaleFileElaborati);
        using var db = OpenDatabase(root);
        using var repository = new FotoRepository(db.Connection!);
        Assert.Null(repository.GetByPercorso(Path.Combine(link, "photo.jpg")));
        Assert.True(File.Exists(outsidePhoto));
    }

    [Fact]
    public void Run_WithJunctionCycle_VisitsRootOnce()
    {
        using TemporaryTestFolder folder = new();
        CreateJunction(folder.GetPath("loop"), folder.FolderPath);

        var result = new AnalysisService().Run(new AnalysisRequest
        {
            Paths = new[] { folder.FolderPath }, NomeDb = folder.GetPath("test.db")
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.Statistics.TotaleCartelleVisitate);
        Assert.Equal(0, result.Statistics.TotaleFileElaborati);
    }

    [Fact]
    public void Scan_WhenScannerIsReused_DetectsFileRemovedBetweenRuns()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("photo.jpg");
        TestImageFactory.CreateJpegImage(photo);
        using var db = OpenDatabase(folder);
        using var repository = new FotoRepository(db.Connection!);
        var config = new AppConfig { Paths = new[] { folder.FolderPath } };
        var scanner = new FileScanner(config, repository, new MetadataService(config), new HashService(), null, new ScanStatistics());
        scanner.Scan();
        Assert.True(repository.GetByPercorso(photo)!.FileEsiste);
        File.Delete(photo);

        scanner.Scan();

        Assert.False(repository.GetByPercorso(photo)!.FileEsiste);
    }

    private static DatabaseManager OpenDatabase(TemporaryTestFolder folder)
    {
        var manager = new DatabaseManager(folder.GetPath("test.db"), false);
        manager.Initialize();
        return manager;
    }

    private static void CreateJunction(string link, string target)
    {
        var start = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (string arg in new[] { "/d", "/c", "mklink", "/J", link, target }) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, process.StandardError.ReadToEnd());
    }
}
