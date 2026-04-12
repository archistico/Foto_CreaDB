using System.IO;
using System.Text.Json;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.Configuration
{
    public class AppConfigLoaderTests
    {
        [Fact]
        public void LoadFromFile_NormalizesAndIncludesImageExtensions()
        {
            string dir = Path.Combine(Path.GetTempPath(), "FotoCreaDB_AppConfigTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string file = Path.Combine(dir, "appsettings_test.json");

                var sample = new
                {
                    NomeDb = "test.db",
                    EstensioniPermesse = new[] { "JPG", "txt" },
                    ImageExtensions = new[] { "jpg", "NEF", "CR2" }
                };

                string json = JsonSerializer.Serialize(sample, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(file, json);

                AppConfig? cfg = AppConfigLoader.LoadFromFile(file);

                Assert.NotNull(cfg);
                Assert.Equal("test.db", cfg!.NomeDb);

                // Normalized to lower-case
                Assert.Contains("jpg", cfg.EstensioniPermesse, System.StringComparer.OrdinalIgnoreCase);
                Assert.Contains("txt", cfg.EstensioniPermesse, System.StringComparer.OrdinalIgnoreCase);

                // Image extensions normalized and present in ImageExtensions
                Assert.Contains("nef", cfg.ImageExtensions, System.StringComparer.OrdinalIgnoreCase);
                Assert.Contains("cr2", cfg.ImageExtensions, System.StringComparer.OrdinalIgnoreCase);

                // Image extensions also ensured to be in EstensioniPermesse
                Assert.Contains("nef", cfg.EstensioniPermesse, System.StringComparer.OrdinalIgnoreCase);
                Assert.Contains("cr2", cfg.EstensioniPermesse, System.StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void Config_ExcludesFilesNotInAllowedExtensions()
        {
            string dir = Path.Combine(Path.GetTempPath(), "FotoCreaDB_AppConfigTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                // Create sample files
                string fileAllowed = Path.Combine(dir, "a.jpg");
                string fileExcluded = Path.Combine(dir, "b.wmv");
                File.WriteAllText(fileAllowed, "x");
                File.WriteAllText(fileExcluded, "x");

                var sample = new
                {
                    EstensioniPermesse = new[] { "jpg", "png" },
                    ImageExtensions = new[] { "jpg", "png" }
                };

                string cfgFile = Path.Combine(dir, "appsettings_test2.json");
                File.WriteAllText(cfgFile, JsonSerializer.Serialize(sample));

                AppConfig? cfg = AppConfigLoader.LoadFromFile(cfgFile);
                Assert.NotNull(cfg);

                string extAllowed = Path.GetExtension(fileAllowed).TrimStart('.').ToLowerInvariant();
                string extExcluded = Path.GetExtension(fileExcluded).TrimStart('.').ToLowerInvariant();

                Assert.True(cfg.EstensioniPermesse.Contains(extAllowed));
                Assert.False(cfg.EstensioniPermesse.Contains(extExcluded));
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
