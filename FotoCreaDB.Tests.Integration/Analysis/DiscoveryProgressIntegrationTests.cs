using Foto_CreaDB2;
using FotoCreaDB.Tests.Integration.TestHelpers;

namespace FotoCreaDB.Tests.Integration.Analysis;

public class DiscoveryProgressIntegrationTests
{
    [Fact]
    public void Scan_ReportsDiscoveryAcrossRootsBeforeAnalyzingAnyFiles()
    {
        using TemporaryTestFolder folder = new();
        string firstRoot = folder.GetPath("first");
        string nested = Path.Combine(firstRoot, "nested");
        string secondRoot = folder.GetPath("second");
        Directory.CreateDirectory(nested);
        Directory.CreateDirectory(secondRoot);
        string[] photos = { Path.Combine(firstRoot, "a.jpg"), Path.Combine(nested, "b.jpg"), Path.Combine(secondRoot, "c.jpg") };
        foreach (string path in photos) File.WriteAllText(path, "photo");
        string excluded = Path.Combine(firstRoot, "ignored.txt");
        File.WriteAllText(excluded, "excluded");
        using var db = new DatabaseManager(folder.GetPath("test.db"), false);
        db.Initialize();
        using var repository = new FotoRepository(db.Connection!);
        var updates = new List<AnalysisProgress>();
        var progress = new SynchronousProgress(value =>
        {
            updates.Add(value);
            if (value.Phase != AnalysisPhase.DiscoveringFiles) return;
            Assert.Equal(0, value.ProcessedFiles);
            Assert.Equal(0, value.TotalFiles);
            Assert.Equal(0, value.Percentage);
            foreach (string photo in photos) Assert.Null(repository.GetByPercorso(photo));
        });

        Scanner(repository, new[] { firstRoot, secondRoot }, progress).Scan();

        Assert.Equal(AnalysisPhase.DiscoveringFiles, updates[0].Phase);
        var discovery = updates.Where(p => p.Phase == AnalysisPhase.DiscoveringFiles).ToList();
        Assert.Equal(0, discovery[0].DiscoveredFiles);
        Assert.Equal(3, discovery[^1].DiscoveredFiles);
        Assert.Equal(secondRoot, discovery[^1].CurrentDirectory);
        Assert.Contains(discovery, p => p.CurrentDirectory == nested);
        Assert.True(discovery.Select(p => p.DiscoveredFiles).SequenceEqual(discovery.Select(p => p.DiscoveredFiles).Order()));
        int firstAnalysis = updates.FindIndex(p => p.Phase == AnalysisPhase.AnalyzingFiles);
        Assert.True(firstAnalysis > 0);
        Assert.All(updates.Skip(firstAnalysis), p => Assert.Equal(AnalysisPhase.AnalyzingFiles, p.Phase));
        Assert.Equal(3, updates[firstAnalysis].TotalFiles);
        Assert.Equal(0, updates[firstAnalysis].ProcessedFiles);
        Assert.Equal(3, updates[^1].ProcessedFiles);
        Assert.Equal(100, updates[^1].Percentage);
        Assert.Null(repository.GetByPercorso(excluded));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Scan_ReportsDiscoveryAndKnownTotalForEmptyFolderOrSingleFile(bool singleFile)
    {
        using TemporaryTestFolder folder = new();
        string input = folder.FolderPath;
        if (singleFile)
        {
            input = folder.GetPath("photo.jpg");
            File.WriteAllText(input, "photo");
        }
        using var db = new DatabaseManager(folder.GetPath("test.db"), false);
        db.Initialize();
        using var repository = new FotoRepository(db.Connection!);
        var updates = new List<AnalysisProgress>();

        Scanner(repository, new[] { input }, new SynchronousProgress(updates.Add)).Scan();

        Assert.Equal(AnalysisPhase.DiscoveringFiles, updates[0].Phase);
        Assert.Equal(singleFile ? 1 : 0, updates.Last(p => p.Phase == AnalysisPhase.DiscoveringFiles).DiscoveredFiles);
        Assert.Equal(AnalysisPhase.AnalyzingFiles, updates[^1].Phase);
        Assert.Equal(singleFile ? 1 : 0, updates[^1].TotalFiles);
        Assert.Equal(singleFile ? 1 : 0, updates[^1].ProcessedFiles);
    }

    private static FileScanner Scanner(FotoRepository repository, string[] paths, IProgress<AnalysisProgress> progress)
    {
        var config = new AppConfig
        {
            Paths = paths,
            EstensioniPermesse = new(StringComparer.OrdinalIgnoreCase) { "jpg" },
            ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        };
        return new FileScanner(config, repository, new MetadataService(config), new HashService(), null, new ScanStatistics(), progress);
    }

    private sealed class SynchronousProgress(Action<AnalysisProgress> callback) : IProgress<AnalysisProgress>
    {
        public void Report(AnalysisProgress value) => callback(value);
    }
}
