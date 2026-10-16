using Foto_CreaDB2;
using FotoCreaDB.Tests.Integration.TestHelpers;

namespace FotoCreaDB.Tests.Integration.Report;

public class DuplicateDetailsOptionIntegrationTests
{
    [Fact]
    public void Run_DetailsOptionChangesOnlyLogAndPreservesCountsAndSize()
    {
        using TemporaryTestFolder folder = new();
        string db = CreateDuplicates(folder);
        var compactLog = new List<ServiceLogMessage>();
        var detailedLog = new List<ServiceLogMessage>();
        var service = new DuplicateReportService();

        var compact = service.Run(new DuplicateReportRequest { NomeDb = db, VerboseDuplicates = false }, log: compactLog.Add);
        var detailed = service.Run(new DuplicateReportRequest { NomeDb = db, VerboseDuplicates = true }, log: detailedLog.Add);

        Assert.DoesNotContain(compactLog, IsFileDetail);
        Assert.Contains(detailedLog, m => m.Message.StartsWith("Tenere (", StringComparison.Ordinal));
        Assert.Equal(2, detailedLog.Count(m => m.Message.TrimStart().StartsWith("Cancellare (", StringComparison.Ordinal)));
        Assert.Equal(1, compact.DuplicateGroupsCount);
        Assert.Equal(2, compact.FilesToDeleteCount);
        Assert.Equal(2 * new FileInfo(folder.GetPath("a.jpg")).Length, compact.FilesToDeleteSize);
        Assert.Equal(compact.FilesToDeleteCount, detailed.FilesToDeleteCount);
        Assert.Equal(compact.DuplicateGroupsCount, detailed.DuplicateGroupsCount);
        Assert.Equal(compact.FilesToDeleteSize, detailed.FilesToDeleteSize);
        Assert.Equal(compact.Decisions[0].FileDaTenere.PercorsoCompleto, detailed.Decisions[0].FileDaTenere.PercorsoCompleto);
        Assert.Contains(compactLog, m => m.Message.Contains("File candidati", StringComparison.Ordinal));
        Assert.True(File.Exists(folder.GetPath("a.jpg")));
        Assert.True(File.Exists(folder.GetPath("b.jpg")));
        Assert.True(File.Exists(folder.GetPath("c.jpg")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Delete_DetailsOptionDoesNotHideWarningsOrAlterDeletion(bool verbose)
    {
        using TemporaryTestFolder folder = new();
        string db = CreateDuplicates(folder);
        var decision = Assert.Single(new DuplicateReportService().Run(new DuplicateReportRequest { NomeDb = db }).Decisions);
        string changed = decision.FileDaEliminare[0].PercorsoCompleto;
        File.WriteAllText(changed, "Unique content created after analysis");
        var messages = new List<ServiceLogMessage>();

        var result = new DuplicateDeletionService().Run(new DeletionRequest
        {
            NomeDb = db, VerboseDuplicates = verbose
        }, log: messages.Add);

        Assert.Equal(verbose, messages.Any(IsFileDetail));
        Assert.Equal(1, result.FilesDeletedCount);
        Assert.Equal(1, result.DatabaseRecordsDeletedCount);
        Assert.Contains(messages, m => m.Level == ServiceLogLevel.Warning && m.Message.Contains("Cancellazione bloccata", StringComparison.Ordinal));
        Assert.Contains(messages, m => m.Message.StartsWith("ELIMINATO:", StringComparison.Ordinal));
        Assert.Equal("Unique content created after analysis", File.ReadAllText(changed));
        Assert.True(File.Exists(decision.FileDaTenere.PercorsoCompleto));
    }

    private static bool IsFileDetail(ServiceLogMessage message) =>
        message.Message.StartsWith("Tenere (", StringComparison.Ordinal)
        || message.Message.TrimStart().StartsWith("Cancellare (", StringComparison.Ordinal);

    private static string CreateDuplicates(TemporaryTestFolder folder)
    {
        string first = folder.GetPath("a.jpg");
        TestImageFactory.CreateDuplicateJpegImages(first, folder.GetPath("b.jpg"));
        File.Copy(first, folder.GetPath("c.jpg"));
        string db = folder.GetPath("test.db");
        Assert.True(new AnalysisService().Run(new AnalysisRequest { NomeDb = db, Paths = new[] { folder.FolderPath } }).Success);
        return db;
    }
}
