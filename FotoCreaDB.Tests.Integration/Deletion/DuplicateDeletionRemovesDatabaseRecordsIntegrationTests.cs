using System.IO;
using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Deletion
{
    public class DuplicateDeletionRemovesDatabaseRecordsIntegrationTests
    {
        [Fact]
        public void Run_WithDuplicateFiles_RemovesDeletedFileRecordsFromDatabase()
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

            DuplicateDeletionService deletionService = new DuplicateDeletionService();

            DeletionResult deletionResult = deletionService.Run(
                new DeletionRequest
                {
                    NomeDb = dbPath,
                    VerboseDuplicates = false
                });

            Assert.True(deletionResult.Success);
            Assert.True(deletionResult.FilesDeletedCount >= 1);
            Assert.Equal(deletionResult.FilesDeletedCount, deletionResult.DatabaseRecordsDeletedCount);

            using DatabaseManager dbManager = new DatabaseManager(dbPath, false);
            dbManager.Initialize();

            using FotoRepository repository = new FotoRepository(dbManager.Connection!);

            ExistingFotoInfo? info1 = repository.GetByPercorso(image1);
            ExistingFotoInfo? info2 = repository.GetByPercorso(image2);

            int existingDbRecords = 0;

            if (info1 != null)
            {
                existingDbRecords++;
            }

            if (info2 != null)
            {
                existingDbRecords++;
            }

            Assert.True(existingDbRecords <= 1);
        }
    }
}