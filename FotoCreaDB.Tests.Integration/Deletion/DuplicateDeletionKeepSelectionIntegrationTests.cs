using System.IO;
using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Deletion
{
    public class DuplicateDeletionKeepSelectionIntegrationTests
    {
        [Fact]
        public void Run_WithTwoDuplicateFiles_KeepsFileWithLongestPath()
        {
            using TemporaryTestFolder folder = new TemporaryTestFolder();

            string dbPath = folder.GetPath("foto.db");

            string shortFolder = folder.GetPath("A");
            string longFolder = folder.GetPath(Path.Combine("2026", "Vacanze al mare", "Album ordinato"));

            Directory.CreateDirectory(shortFolder);
            Directory.CreateDirectory(longFolder);

            string shortPath = Path.Combine(shortFolder, "dup.jpg");
            string longPath = Path.Combine(longFolder, "dup.jpg");

            TestImageFactory.CreateDuplicateJpegImages(shortPath, longPath);

            AnalysisService analysisService = new AnalysisService();

            AnalysisResult analysisResult = analysisService.Run(
                new AnalysisRequest
                {
                    Paths = new[] { folder.FolderPath },
                    NomeDb = dbPath,
                    CancellaDbSeEsiste = false,
                    LogDettagliato = false,
                    ProgressEvery = 1000,
                    VerboseDuplicates = false
                });

            Assert.True(analysisResult.Success);

            DuplicateDeletionService deletionService = new DuplicateDeletionService();

            DeletionResult deletionResult = deletionService.Run(
                new DeletionRequest
                {
                    NomeDb = dbPath,
                    VerboseDuplicates = false
                });

            Assert.True(deletionResult.Success);

            Assert.True(File.Exists(longPath));
            Assert.False(File.Exists(shortPath));
        }
    }
}