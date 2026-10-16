using System.Security.Cryptography;
using System.Runtime.InteropServices;
using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;

namespace FotoCreaDB.Tests.Integration.Deletion;

public class DuplicateDeletionSafetyIntegrationTests
{
    [Fact]
    public void Run_AfterScanningSamePhotoWithDifferentPathCase_PreservesOnlyCopy()
    {
        using TemporaryTestFolder folder = new();
        string path = folder.GetPath("photo.jpg");
        TestImageFactory.CreateJpegImage(path);
        byte[] original = File.ReadAllBytes(path);
        string db = folder.GetPath("test.db");
        // Separate runs retain coverage of aliases already present in a database;
        // a single scan now visits case variants only once.
        foreach (string input in new[] { path, path.ToUpperInvariant() })
            new AnalysisService().Run(new AnalysisRequest { NomeDb = db, Paths = new[] { input } });
        var report = new DuplicateReportService().Run(new DuplicateReportRequest { NomeDb = db });
        Assert.Equal(1, report.FilesToDeleteCount);

        var result = new DuplicateDeletionService().Run(new DeletionRequest { NomeDb = db });

        Assert.False(result.Success);
        Assert.Equal(1, result.FilesBlockedCount);
        Assert.Equal(0, result.FilesDeletedCount);
        Assert.Equal(0, result.DatabaseRecordsDeletedCount);
        Assert.Equal(original, File.ReadAllBytes(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeleteFiles_WhenCandidateIsKeeperAlias_PreservesOnlyCopy(bool differentCase)
    {
        using TemporaryTestFolder folder = new();
        string path = folder.GetPath("photo.jpg");
        File.WriteAllText(path, "only copy");
        DuplicateBinaryCandidate keeper = Candidate(path, 1);
        DuplicateBinaryCandidate alias = Candidate(path, 2);
        alias.PercorsoCompleto = differentCase ? path.ToUpperInvariant() : folder.GetPath(".\\photo.jpg");

        int deleted = Delete(folder, Decision(keeper, alias));

        Assert.Equal(0, deleted);
        Assert.Equal("only copy", File.ReadAllText(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeleteFiles_WhenContentsDiffer_PreservesCandidate(bool missingHashes)
    {
        using TemporaryTestFolder folder = new();
        string keepPath = folder.GetPath("keep.jpg");
        string otherPath = folder.GetPath("other.jpg");
        File.WriteAllText(keepPath, "AAAA");
        File.WriteAllText(otherPath, "BBBB");
        DuplicateBinaryCandidate keeper = Candidate(keepPath, 1);
        DuplicateBinaryCandidate other = Candidate(otherPath, 2);
        if (missingHashes)
        {
            keeper.HashSha256 = "";
            other.HashSha256 = "";
        }

        int deleted = Delete(folder, Decision(keeper, other));

        Assert.Equal(0, deleted);
        Assert.Equal("BBBB", File.ReadAllText(otherPath));
    }

    [Fact]
    public void DeleteFiles_KeeperCannotBeDeletedOrChangedBetweenCandidates()
    {
        using TemporaryTestFolder folder = new();
        string keeperPath = folder.GetPath("keep.jpg");
        string firstPath = folder.GetPath("first.jpg");
        string secondPath = folder.GetPath("second.jpg");
        foreach (string path in new[] { keeperPath, firstPath, secondPath })
            File.WriteAllText(path, "original photo");
        DuplicateBinaryDecision decision = Decision(Candidate(keeperPath, 1), Candidate(firstPath, 2), Candidate(secondPath, 3));
        bool attempted = false;
        bool deleteBlocked = false;
        bool writeBlocked = false;
        var progress = new SynchronousProgress(value =>
        {
            if (value.ProcessedFiles != 1) return;
            attempted = true;
            try { File.Delete(keeperPath); } catch (IOException) { deleteBlocked = true; }
            try { File.WriteAllText(keeperPath, "changed photo!"); } catch (IOException) { writeBlocked = true; }
        });

        int deleted = Delete(folder, decision, progress);

        Assert.True(attempted);
        Assert.True(deleteBlocked);
        Assert.True(writeBlocked);
        Assert.Equal(2, deleted);
        Assert.Equal("original photo", File.ReadAllText(keeperPath));
    }

    [Fact]
    public void DeleteFiles_WithSameSizeModificationAfterAnalysis_PreservesCandidate()
    {
        using TemporaryTestFolder folder = new();
        string keep = folder.GetPath("keep.jpg");
        string other = folder.GetPath("other.jpg");
        File.WriteAllText(keep, "AAAA");
        File.WriteAllText(other, "AAAA");
        var decision = Decision(Candidate(keep, 1), Candidate(other, 2));
        File.WriteAllText(other, "BBBB");

        Assert.Equal(0, Delete(folder, decision));
        Assert.Equal("BBBB", File.ReadAllText(other));
    }

    [Fact]
    public void DeleteFiles_WhenCandidateIsHardLinkOfKeeper_PreservesBothNames()
    {
        using TemporaryTestFolder folder = new();
        string keep = folder.GetPath("keep.jpg");
        string alias = folder.GetPath("alias.jpg");
        File.WriteAllText(keep, "only physical copy");
        Assert.True(CreateHardLinkW(alias, keep, IntPtr.Zero));

        Assert.Equal(0, Delete(folder, Decision(Candidate(keep, 1), Candidate(alias, 2))));
        Assert.Equal("only physical copy", File.ReadAllText(keep));
        Assert.True(File.Exists(alias));
    }

    [Fact]
    public void DeleteFiles_WithLongPaths_DeletesOnlyDuplicate()
    {
        using TemporaryTestFolder folder = new();
        string directory = folder.FolderPath;
        for (int i = 0; i < 4; i++) directory = Path.Combine(directory, new string('a', 65));
        Directory.CreateDirectory(directory);
        string keep = Path.Combine(directory, "keep.jpg");
        string other = Path.Combine(directory, "other.jpg");
        File.WriteAllText(keep, "photo");
        File.WriteAllText(other, "photo");

        Assert.Equal(1, Delete(folder, Decision(Candidate(keep, 1), Candidate(other, 2))));
        Assert.Equal("photo", File.ReadAllText(keep));
        Assert.False(File.Exists(other));
    }

    [Fact]
    public void DeleteFiles_WhenCandidateIsInUse_PreservesFileAndDatabaseRecord()
    {
        using TemporaryTestFolder folder = new();
        string keep = folder.GetPath("keep.jpg");
        string other = folder.GetPath("other.jpg");
        File.WriteAllText(keep, "photo");
        File.WriteAllText(other, "photo");
        var decision = Decision(Candidate(keep, 1), Candidate(other, 2));
        using var writer = new FileStream(other, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);

        Assert.Equal(0, Delete(folder, decision));
        using var manager = new DatabaseManager(folder.GetPath("test.db"), false);
        manager.Initialize();
        using var repository = new FotoRepository(manager.Connection!);
        Assert.NotNull(repository.GetByPercorso(other));
        Assert.True(File.Exists(other));
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLinkW(string newName, string existingName, IntPtr security);

    private static DuplicateBinaryCandidate Candidate(string path, long id) => new()
    {
        Id = id, PercorsoCompleto = path, Dimensione = new FileInfo(path).Length,
        HashSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))
    };

    private static DuplicateBinaryDecision Decision(DuplicateBinaryCandidate keeper, params DuplicateBinaryCandidate[] others) => new()
    {
        FileDaTenere = keeper, HashSha256 = keeper.HashSha256,
        Dimensione = keeper.Dimensione, FileDaEliminare = others.ToList()
    };

    private static int Delete(TemporaryTestFolder folder, DuplicateBinaryDecision decision, IProgress<DeletionProgress>? progress = null)
    {
        string db = folder.GetPath("test.db");
        using (var manager = new DatabaseManager(db, false))
        {
            manager.Initialize();
            using var repository = new FotoRepository(manager.Connection!);
            foreach (var item in new[] { decision.FileDaTenere }.Concat(decision.FileDaEliminare))
                repository.Insert(new Foto { percorsoCompleto = item.PercorsoCompleto });
        }
        return new DuplicateBinaryDeletionService().DeleteFiles(new() { decision }, db, null, out _, out _, progress!);
    }

    private sealed class SynchronousProgress(Action<DeletionProgress> callback) : IProgress<DeletionProgress>
    {
        public void Report(DeletionProgress value) => callback(value);
    }
}
