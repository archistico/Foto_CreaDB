using System.IO;
using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Analysis
{
    public class AnalysisServiceLongPathIntegrationTests
    {
        [Fact]
        public void Run_WithExcessivelyLongInputPath_CompletesWithoutCrashing()
        {
            using TemporaryTestFolder folder = new TemporaryTestFolder();

            string dbPath = folder.GetPath("foto.db");

            string veryLongSegment = new string('a', 280);
            string veryLongPath = Path.Combine(
                folder.FolderPath,
                veryLongSegment,
                veryLongSegment,
                veryLongSegment,
                "file.jpg");

            AnalysisService service = new AnalysisService();

            AnalysisResult result = service.Run(
                new AnalysisRequest
                {
                    Paths = new[] { veryLongPath },
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