using FotoCreaDB.Tests.Integration.TestHelpers;
using Foto_CreaDB2;
using System.Data.SQLite;
using Xunit;

namespace FotoCreaDB.Tests.Integration.Analysis
{
    public class AnalysisDatabaseInspectionIntegrationTests
    {
        [Fact]
        public void Run_WithDuplicateFiles_SavesTwoRowsWithSameBinaryKey()
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

            using SQLiteConnection connection = new SQLiteConnection("Data Source=" + dbPath + ";Version=3;");
            connection.Open();

            using SQLiteCommand command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    COUNT(*) AS TotaleRighe,
                    SUM(CASE WHEN FILE_ESISTE = 1 THEN 1 ELSE 0 END) AS FileEsistenti,
                    SUM(CASE WHEN IFNULL(HASH_SHA256, '') <> '' THEN 1 ELSE 0 END) AS RigheConHash,
                    COUNT(DISTINCT HASH_SHA256) AS HashDiversi,
                    COUNT(DISTINCT CHIAVE_DUP_BINARIO) AS ChiaviBinarieDiverse
                FROM FOTO";

            using SQLiteDataReader reader = command.ExecuteReader();

            Assert.True(reader.Read());

            int totaleRighe = reader["TotaleRighe"] == DBNull.Value ? 0 : System.Convert.ToInt32(reader["TotaleRighe"]);
            int fileEsistenti = reader["FileEsistenti"] == DBNull.Value ? 0 : System.Convert.ToInt32(reader["FileEsistenti"]);
            int righeConHash = reader["RigheConHash"] == DBNull.Value ? 0 : System.Convert.ToInt32(reader["RigheConHash"]);
            int hashDiversi = reader["HashDiversi"] == DBNull.Value ? 0 : System.Convert.ToInt32(reader["HashDiversi"]);
            int chiaviBinarieDiverse = reader["ChiaviBinarieDiverse"] == DBNull.Value ? 0 : System.Convert.ToInt32(reader["ChiaviBinarieDiverse"]);

            Assert.Equal(2, totaleRighe);
            Assert.Equal(2, fileEsistenti);
            Assert.Equal(2, righeConHash);
            Assert.Equal(1, hashDiversi);
            Assert.Equal(1, chiaviBinarieDiverse);
        }
    }
}