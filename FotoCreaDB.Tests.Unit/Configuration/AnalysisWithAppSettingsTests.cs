using System.IO;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.Configuration
{
    public class AnalysisWithAppSettingsTests
    {
        [Fact]
        public void Analysis_Respects_AppSettings_Extensions()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FotoCreaDB_AnalysisTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            string prevCwd = Directory.GetCurrentDirectory();

            try
            {
                // write appsettings.json limiting extensions to gif only
                string cfgFile = Path.Combine(temp, "appsettings.json");
                string json = "{ \"EstensioniPermesse\": [ \"gif\" ], \"ImageExtensions\": [ \"gif\" ] }";
                File.WriteAllText(cfgFile, json);

                // create files
                string fileGif = Path.Combine(temp, "a.gif");
                string fileJpg = Path.Combine(temp, "b.jpg");
                File.WriteAllText(fileGif, "x");
                File.WriteAllText(fileJpg, "x");

                // set current directory so AnalysisService will load appsettings.json
                Directory.SetCurrentDirectory(temp);

                AnalysisService svc = new AnalysisService();

                AnalysisResult result = svc.Run(new AnalysisRequest
                {
                    Paths = new[] { temp },
                    NomeDb = Path.Combine(temp, "foto.db"),
                    CancellaDbSeEsiste = false,
                    LogDettagliato = false,
                    ProgressEvery = 1000,
                    VerboseDuplicates = false
                });

                Assert.NotNull(result);
                Assert.Equal(1, result.Statistics.TotaleFileConEstensioneValida);
            }
            finally
            {
                try { Directory.SetCurrentDirectory(prevCwd); } catch { }
                try { Directory.Delete(temp, true); } catch { }
            }
        }
    }
}
