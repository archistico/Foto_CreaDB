using System.IO;
using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Deletion
{
    public class DuplicateDeletionOrphanCleanupIntegrationTests
    {
        [Fact]
        public void Run_WhenCandidateFileIsAlreadyMissing_RemovesOrphanRecordFromDatabase()
        {
            using TemporaryTestFolder folder = new TemporaryTestFolder();

            string dbPath = folder.GetPath("foto.db");

            string keepFolder = folder.GetPath("Evento");
            Directory.CreateDirectory(keepFolder);

            string imageToKeep = Path.Combine(keepFolder, "foto_da_tenere.jpg");
            string imageToDelete = folder.GetPath("a.jpg");

            TestImageFactory.CreateDuplicateJpegImages(imageToKeep, imageToDelete);

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

            Assert.True(File.Exists(imageToKeep));
            Assert.True(File.Exists(imageToDelete));

            File.Delete(imageToDelete);

            Assert.False(File.Exists(imageToDelete));

            DuplicateDeletionService deletionService = new DuplicateDeletionService();

            DeletionResult deletionResult = deletionService.Run(
                new DeletionRequest
                {
                    NomeDb = dbPath,
                    VerboseDuplicates = false
                });

            Assert.True(deletionResult.Success);
            Assert.True(deletionResult.OrphanRecordsDeletedCount >= 1);
            Assert.True(deletionResult.DatabaseRecordsDeletedCount >= 1);

            using DatabaseManager dbManager = new DatabaseManager(dbPath, false);
            dbManager.Initialize();

            using FotoRepository repository = new FotoRepository(dbManager.Connection!);

            ExistingFotoInfo? infoKeep = repository.GetByPercorso(imageToKeep);
            ExistingFotoInfo? infoDelete = repository.GetByPercorso(imageToDelete);

            Assert.NotNull(infoKeep);
            Assert.Null(infoDelete);
        }
    }
}