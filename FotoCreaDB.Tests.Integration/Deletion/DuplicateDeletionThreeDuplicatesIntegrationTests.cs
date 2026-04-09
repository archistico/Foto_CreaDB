using System.IO;
using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Deletion
{
    public class DuplicateDeletionThreeDuplicatesIntegrationTests
    {
        [Fact]
        public void Run_WithThreeDuplicateFiles_DeletesTwoAndKeepsOne()
        {
            using TemporaryTestFolder folder = new TemporaryTestFolder();

            string dbPath = folder.GetPath("foto.db");

            string folder1 = folder.GetPath("DaSistemare");
            string folder2 = folder.GetPath(Path.Combine("2026", "Evento"));
            string folder3 = folder.GetPath(Path.Combine("2026", "Viaggio", "Album finale"));

            Directory.CreateDirectory(folder1);
            Directory.CreateDirectory(folder2);
            Directory.CreateDirectory(folder3);

            string path1 = Path.Combine(folder1, "dup.jpg");
            string path2 = Path.Combine(folder2, "dup.jpg");
            string path3 = Path.Combine(folder3, "dup.jpg");

            TestImageFactory.CreateJpegImage(path1);
            File.Copy(path1, path2, true);
            File.Copy(path1, path3, true);

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
            Assert.Equal(1, reportResult.DuplicateGroupsCount);
            Assert.Equal(2, reportResult.FilesToDeleteCount);
            Assert.Single(reportResult.Decisions);
            Assert.Equal(2, reportResult.Decisions[0].FileDaEliminare.Count);

            DuplicateDeletionService deletionService = new DuplicateDeletionService();

            DeletionResult deletionResult = deletionService.Run(
                new DeletionRequest
                {
                    NomeDb = dbPath,
                    VerboseDuplicates = false
                });

            Assert.True(deletionResult.Success);

            int existingFiles = 0;

            if (File.Exists(path1))
            {
                existingFiles++;
            }

            if (File.Exists(path2))
            {
                existingFiles++;
            }

            if (File.Exists(path3))
            {
                existingFiles++;
            }

            Assert.Equal(1, existingFiles);

            Assert.True(File.Exists(path3));
            Assert.False(File.Exists(path1));
            Assert.False(File.Exists(path2));
        }
    }
}