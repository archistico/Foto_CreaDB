using System.IO;
using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Deletion
{
    public class DuplicateDeletionKeeperMissingIntegrationTests
    {
        [Fact]
        public void Run_WhenKeeperMissing_SkipsGroupAndDoesNotDeleteDuplicates()
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

            Assert.True(reportResult.FilesToDeleteCount >= 1);
            Assert.NotNull(reportResult.Decisions);
            Assert.NotEmpty(reportResult.Decisions);

            // Prendiamo il primo gruppo e eliminiamo il file che sarebbe stato tenuto
            DuplicateBinaryDecision decision = reportResult.Decisions[0];
            string keeper = decision.FileDaTenere?.PercorsoCompleto ?? string.Empty;

            // Assicuriamoci che esista e poi lo cancelliamo per simulare il caso
            Assert.True(File.Exists(keeper));
            File.Delete(keeper);
            Assert.False(File.Exists(keeper));

            // Prendiamo uno dei duplicati e verifichiamo che esista ancora prima della cancellazione
            string duplicatePath = decision.FileDaEliminare != null && decision.FileDaEliminare.Count > 0
                ? decision.FileDaEliminare[0].PercorsoCompleto
                : string.Empty;

            Assert.True(File.Exists(duplicatePath));

            DuplicateDeletionService deletionService = new DuplicateDeletionService();

            DeletionResult deletionResult = deletionService.Run(
                new DeletionRequest
                {
                    NomeDb = dbPath,
                    VerboseDuplicates = false
                });

            Assert.True(deletionResult.Success);
            // Se la logica salta il gruppo perchè il file da tenere manca, non devono essere cancellati duplicati
            Assert.Equal(0, deletionResult.FilesDeletedCount);
            Assert.Equal(0, deletionResult.DatabaseRecordsDeletedCount);

            // Il duplicato dovrebbe ancora esistere sul disco
            Assert.True(File.Exists(duplicatePath));
        }
    }
}
