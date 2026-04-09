using System.IO;
using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Deletion
{
    public class DuplicateDeletionAdditionalIntegrationTests
    {
        [Fact]
        public void Run_WithNoDuplicates_DeletesNothing()
        {
            using TemporaryTestFolder folder = new TemporaryTestFolder();

            string dbPath = folder.GetPath("foto.db");
            string image1 = folder.GetPath("a.jpg");

            TestImageFactory.CreateJpegImage(image1);

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
            Assert.True(File.Exists(image1));
        }
    }
}