using System;
using System.IO;
using System.Text.Json;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Utility to load AppConfig from a JSON file.
    /// </summary>
    public static class AppConfigLoader
    {
        /// <summary>
        /// Loads configuration from the specified JSON file.
        /// Returns null if the file does not exist or cannot be parsed.
        /// </summary>
        public static AppConfig? LoadFromFile(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return null;
                }

                string json = File.ReadAllText(path);
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                AppConfig? cfg = JsonSerializer.Deserialize<AppConfig>(json, opts);
                if (cfg == null)
                {
                    return null;
                }

                // Validation / normalization
                if (cfg.EstensioniPermesse == null)
                {
                    cfg.EstensioniPermesse = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                if (cfg.ImageExtensions == null)
                {
                    cfg.ImageExtensions = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                // Normalize all entries to lower-case and trim spaces
                cfg.EstensioniPermesse = NormalizeSet(cfg.EstensioniPermesse);
                cfg.ImageExtensions = NormalizeSet(cfg.ImageExtensions);

                // Ensure that image extensions are included among allowed extensions
                foreach (string imgExt in cfg.ImageExtensions)
                {
                    if (!cfg.EstensioniPermesse.Contains(imgExt))
                    {
                        cfg.EstensioniPermesse.Add(imgExt);
                    }
                }

                return cfg;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Salva la configurazione su file JSON.
        /// Sovrascrive il file esistente.
        /// </summary>
        public static bool SaveToFile(AppConfig cfg, string path)
        {
            try
            {
                if (cfg == null) return false;

                string dir = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(dir) && !System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                var opts = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(cfg, opts);
                System.IO.File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static System.Collections.Generic.HashSet<string> NormalizeSet(System.Collections.Generic.HashSet<string> input)
        {
            var result = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in input)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                result.Add(s.Trim().ToLowerInvariant());
            }

            return new System.Collections.Generic.HashSet<string>(result, StringComparer.OrdinalIgnoreCase);
        }
    }
}
