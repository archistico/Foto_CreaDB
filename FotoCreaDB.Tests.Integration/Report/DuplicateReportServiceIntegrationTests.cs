using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Report
{
    public class DuplicateReportServiceIntegrationTests
    {
        [Fact]
        public void Run_WithDuplicateFiles_ReturnsCandidatesToDelete()
        {
            using TemporaryTestFolder folder = new TemporaryTestFolder();

            string dbPath = folder.GetPath("foto.db");
            string image1 = folder.GetPath("dup1.jpg");
            string image2 = folder.GetPath("dup2.jpg");

            TestImageFactory.CreateDuplicateJpegImages(image1, image2);

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

            DuplicateReportService reportService = new DuplicateReportService();

            DuplicateReportResult reportResult = reportService.Run(
                new DuplicateReportRequest
                {
                    NomeDb = dbPath,
                    VerboseDuplicates = false
                });

            Assert.NotNull(reportResult);
            Assert.True(reportResult.Success);
            Assert.NotNull(reportResult.Decisions);
            Assert.Equal(1, reportResult.DuplicateGroupsCount);
            Assert.Equal(1, reportResult.FilesToDeleteCount);
            Assert.Single(reportResult.Decisions);
            Assert.Single(reportResult.Decisions[0].FileDaEliminare);
        }
    }
}