using System.Globalization;
using System.Security.Cryptography;
using Foto_CreaDB2;
using FotoCreaDB.Tests.Integration.TestHelpers;

namespace FotoCreaDB.Tests.Integration.Analysis;

public class AnalysisSafetyRegressionTests
{
    [Fact]
    public void Run_WhenSameSizeFileChangesWithinOneSecond_RefreshesHash()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("photo.pdf");
        var second = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        File.WriteAllText(photo, "first");
        File.SetLastWriteTimeUtc(photo, second.AddMilliseconds(100));
        var request = Request(folder, photo);
        Assert.True(new AnalysisService().Run(request).Success);
        File.WriteAllText(photo, "other");
        File.SetLastWriteTimeUtc(photo, second.AddMilliseconds(900));

        var result = new AnalysisService().Run(request);

        Assert.True(result.Success);
        Assert.Equal(0, result.Statistics.TotaleFileSaltati);
        Assert.Equal(Hash(photo), ReadPhoto(request.NomeDb, photo)!.HashSha256);
        Assert.Equal(1, new AnalysisService().Run(request).Statistics.TotaleFileSaltati);
    }

    [Fact]
    public void Run_WithLegacyTimestamp_RehashesBeforeTrustingIncrementalCache()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("legacy.pdf");
        File.WriteAllText(photo, "photo");
        var request = Request(folder, photo);
        using (var db = new DatabaseManager(request.NomeDb, false))
        {
            db.Initialize();
            using var repository = new FotoRepository(db.Connection!);
            repository.Insert(new Foto
            {
                percorsoCompleto = photo, dimensione = new FileInfo(photo).Length,
                dataFileModifica = File.GetLastWriteTime(photo).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                hashSha256 = new string('0', 64)
            });
        }

        var result = new AnalysisService().Run(request);

        Assert.Equal(0, result.Statistics.TotaleFileSaltati);
        Assert.Equal(Hash(photo), ReadPhoto(request.NomeDb, photo)!.HashSha256);
    }

    [Fact]
    public void Run_WithPreviousSchema_AddsPreciseTimestampWithoutLosingRecords()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("legacy.pdf");
        File.WriteAllText(photo, "photo");
        var request = Request(folder, photo);
        Assert.True(new AnalysisService().Run(request).Success);
        long originalId = ReadPhoto(request.NomeDb, photo)!.Id;
        using (var db = new DatabaseManager(request.NomeDb, false))
        {
            db.Initialize();
            using var command = db.Connection!.CreateCommand();
            command.CommandText = "ALTER TABLE FOTO DROP COLUMN DATA_FILE_MODIFICA_UTC_TICKS";
            command.ExecuteNonQuery();
        }

        var result = new AnalysisService().Run(request);

        Assert.True(result.Success);
        Assert.Equal(0, result.Statistics.TotaleFileSaltati);
        var after = ReadPhoto(request.NomeDb, photo)!;
        Assert.Equal(originalId, after.Id);
        Assert.Equal(Hash(photo), after.HashSha256);
        Assert.Equal(File.GetLastWriteTimeUtc(photo).Ticks, after.DataFileModificaUtcTicks);
        Assert.Equal(1, new AnalysisService().Run(request).Statistics.TotaleFileSaltati);
    }

    [Fact]
    public void Run_WhenPreviouslyScannedFileHasAnActiveWriter_DoesNotSkipAsUnchanged()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("photo.pdf");
        File.WriteAllText(photo, "photo");
        var request = Request(folder, photo);
        Assert.True(new AnalysisService().Run(request).Success);
        using (var writer = new FileStream(photo, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            var result = new AnalysisService().Run(request);

            Assert.False(result.Success);
            Assert.Equal(0, result.Statistics.TotaleFileSaltati);
        }
        Assert.True(new AnalysisService().Run(request).Success);
    }

    [Fact]
    public void Run_WithMissingCachedHash_RetriesEvenWithMatchingPreciseTimestamp()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("photo.pdf");
        File.WriteAllText(photo, "photo");
        var request = Request(folder, photo);
        Assert.True(new AnalysisService().Run(request).Success);
        using (var db = new DatabaseManager(request.NomeDb, false))
        {
            db.Initialize();
            using var command = db.Connection!.CreateCommand();
            command.CommandText = "UPDATE FOTO SET HASH_SHA256 = ''";
            command.ExecuteNonQuery();
        }

        var result = new AnalysisService().Run(request);

        Assert.True(result.Success);
        Assert.Equal(0, result.Statistics.TotaleFileSaltati);
        Assert.Equal(Hash(photo), ReadPhoto(request.NomeDb, photo)!.HashSha256);
        Assert.Equal(1, new AnalysisService().Run(request).Statistics.TotaleFileSaltati);
    }

    [Fact]
    public void Run_KeepsFileStableUntilItsRecordHasBeenSaved()
    {
        using TemporaryTestFolder folder = new();
        string photo = folder.GetPath("photo.pdf");
        File.WriteAllText(photo, "photo");
        bool? writerWasBlocked = null;

        var result = new AnalysisService().Run(Request(folder, photo), log: message =>
        {
            if (!message.Message.StartsWith("INSERT:", StringComparison.Ordinal)) return;
            try
            {
                using var writer = new FileStream(photo, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                writerWasBlocked = false;
            }
            catch (IOException) { writerWasBlocked = true; }
        });

        Assert.True(result.Success);
        Assert.True(writerWasBlocked);
        using var after = new FileStream(photo, FileMode.Open, FileAccess.Write, FileShare.None);
    }

    [Fact]
    public void Run_WithOverlappingRoots_ProcessesAndCountsEachFileOnce()
    {
        using TemporaryTestFolder folder = new();
        string nested = folder.GetPath("nested");
        Directory.CreateDirectory(nested);
        string photo = Path.Combine(nested, "photo.pdf");
        File.WriteAllText(photo, "photo");
        var updates = new List<AnalysisProgress>();

        var result = new AnalysisService().Run(Request(folder, nested, folder.FolderPath, photo, nested, photo.ToUpperInvariant()),
            new SynchronousProgress(updates.Add));

        Assert.True(result.Success);
        Assert.Equal(1, result.Statistics.TotaleFileElaborati);
        Assert.Equal(2, result.Statistics.TotaleCartelleVisitate);
        Assert.Equal(1, updates[^1].TotalFiles);
        Assert.Equal(1, updates[^1].ProcessedFiles);
        Assert.Equal(1, updates.Last(p => p.Phase == AnalysisPhase.DiscoveringFiles).DiscoveredFiles);
    }

    [Fact]
    public void Run_WithDatabaseUsingAllowedExtension_DoesNotAnalyzeItsOwnDatabase()
    {
        using TemporaryTestFolder folder = new();
        var request = Request(folder, folder.FolderPath);
        request.NomeDb = folder.GetPath("database.jpg");

        var result = new AnalysisService().Run(request);

        Assert.True(result.Success);
        Assert.Equal(0, result.Statistics.TotaleFileElaborati);
        Assert.Null(ReadPhoto(request.NomeDb, request.NomeDb));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Run_WithBlankInput_RejectsBeforeCreatingDatabase(string? input)
    {
        using TemporaryTestFolder folder = new();
        var request = Request(folder, input!);

        Assert.ThrowsAny<ArgumentException>(() => new AnalysisService().Run(request));

        Assert.False(File.Exists(request.NomeDb));
    }

    [Fact]
    public void Run_WithUnprocessableSingleFileAndReset_PreservesDatabase()
    {
        using TemporaryTestFolder folder = new();
        string input = folder.GetPath("excluded.txt");
        File.WriteAllText(input, "excluded");
        var request = Request(folder, input);
        using (var db = new DatabaseManager(request.NomeDb, false)) db.Initialize();
        byte[] before = File.ReadAllBytes(request.NomeDb);
        request.CancellaDbSeEsiste = true;

        Assert.Throws<ArgumentException>(() => new AnalysisService().Run(request));

        Assert.Equal(before, File.ReadAllBytes(request.NomeDb));
    }

    [Fact]
    public void Run_SnapshotsInputPathsBeforeInvokingCallbacks()
    {
        using TemporaryTestFolder folder = new();
        string selected = folder.GetPath("selected.pdf");
        string other = folder.GetPath("other.pdf");
        File.WriteAllText(selected, "selected");
        File.WriteAllText(other, "other");
        var request = Request(folder, selected);

        var result = new AnalysisService().Run(request, log: message =>
        {
            if (message.Message == "Avvio analisi.") request.Paths[0] = other;
        });

        Assert.True(result.Success);
        Assert.NotNull(ReadPhoto(request.NomeDb, selected));
        Assert.Null(ReadPhoto(request.NomeDb, other));
    }

    private static AnalysisRequest Request(TemporaryTestFolder folder, params string[] paths) =>
        new() { Paths = paths, NomeDb = folder.GetPath("test.db") };

    private static ExistingFotoInfo? ReadPhoto(string database, string photo)
    {
        using var db = new DatabaseManager(database, false);
        db.Initialize();
        using var repository = new FotoRepository(db.Connection!);
        return repository.GetByPercorso(photo);
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private sealed class SynchronousProgress(Action<AnalysisProgress> callback) : IProgress<AnalysisProgress>
    {
        public void Report(AnalysisProgress value) => callback(value);
    }
}
