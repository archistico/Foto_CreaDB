using System.IO;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.Configuration
{
    public class PreferencesSaverTests
    {
        [Fact]
        public void SavePreferences_WritesFotoAndDb_ToConfig()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FotoCreaDB_PrefsTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            string cfgFile = Path.Combine(temp, "appsettings.json");

            try
            {
                bool ok = PreferencesSaver.SavePreferences(Path.Combine(temp, "Immagini"), Path.Combine(temp, "db.db"), cfgFile);
                Assert.True(ok);
                Assert.True(File.Exists(cfgFile));

                string content = File.ReadAllText(cfgFile);
                var cfg = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(content);
                Assert.NotNull(cfg);
                Assert.Equal(Path.GetFullPath(Path.Combine(temp, "Immagini")), cfg.Paths[0]);
                Assert.Equal(Path.GetFullPath(Path.Combine(temp, "db.db")), cfg.NomeDb);
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }

        [Fact]
        public void SavePreferences_MergesWithExistingConfig()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FotoCreaDB_PrefsTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            string cfgFile = Path.Combine(temp, "appsettings.json");

            try
            {
                File.WriteAllText(cfgFile, "{ \"EstensioniPermesse\": [ \"jpg\" ] }");

                bool ok = PreferencesSaver.SavePreferences("/img/path", "/db/path.db", cfgFile);
                Assert.True(ok);

                string content = File.ReadAllText(cfgFile);
                var cfg = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(content);
                Assert.NotNull(cfg);
                Assert.Contains("jpg", cfg.EstensioniPermesse);
                Assert.Equal(Path.GetFullPath("/img/path"), cfg.Paths[0]);
                Assert.Equal(Path.GetFullPath("/db/path.db"), cfg.NomeDb);
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }
    }
}
