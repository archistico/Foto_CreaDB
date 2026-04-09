using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Report
{
    public class DuplicateReportAdditionalIntegrationTests
    {
        [Fact]
        public void Run_WithNoDuplicates_ReturnsZeroCandidates()
        {
            using TemporaryTestFolder folder = new TemporaryTestFolder();

            string dbPath = folder.GetPath("foto.db");
            string image1 = folder.GetPath("a.jpg");
            string image2 = folder.GetPath("b.jpg");

            TestImageFactory.CreateJpegImage(image1);
            TestFileFactory.CreateBinaryFile(image2, new byte[] { 1, 2, 3, 4, 5, 6, 7 });

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

            Assert.True(reportResult.Success);
            Assert.Equal(0, reportResult.DuplicateGroupsCount);
            Assert.Equal(0, reportResult.FilesToDeleteCount);
        }
    }
}