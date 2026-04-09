using System.IO;
using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Analysis
{
    public class AnalysisServiceIntegrationTests
    {
        [Fact]
        public void Run_WithValidFolder_CompletesSuccessfully_AndCreatesDatabase()
        {
            using TemporaryTestFolder folder = new TemporaryTestFolder();

            string dbPath = folder.GetPath("foto.db");
            string imagePath = folder.GetPath("test.jpg");
            TestImageFactory.CreateJpegImage(imagePath);

            AnalysisService service = new AnalysisService();

            AnalysisResult result = service.Run(
                new AnalysisRequest
                {
                    Paths = new[] { folder.FolderPath },
                    NomeDb = dbPath,
                    CancellaDbSeEsiste = false,
                    LogDettagliato = false,
                    ProgressEvery = 1000,
                    VerboseDuplicates = false
                });

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(File.Exists(dbPath));
        }

        [Fact]
        public void Run_WithSingleValidFile_CompletesSuccessfully()
        {
            using TemporaryTestFolder folder = new TemporaryTestFolder();

            string dbPath = folder.GetPath("foto.db");
            string imagePath = folder.GetPath("single.jpg");

            TestImageFactory.CreateJpegImage(imagePath);

            AnalysisService service = new AnalysisService();

            AnalysisResult result = service.Run(
                new AnalysisRequest
                {
                    Paths = new[] { imagePath },
                    NomeDb = dbPath,
                    CancellaDbSeEsiste = false,
                    LogDettagliato = false,
                    ProgressEvery = 1000,
                    VerboseDuplicates = false
                });

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.True(File.Exists(dbPath));
        }
    }
}