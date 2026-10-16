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
        /// Returns null only if the file does not exist. Invalid configuration is an error.
        /// </summary>
        public static AppConfig? LoadFromFile(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Percorso configurazione vuoto.", nameof(path));
                string json;
                try { json = File.ReadAllText(path); }
                catch (FileNotFoundException) { return null; }
                catch (DirectoryNotFoundException) { return null; }
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                AppConfig? cfg = JsonSerializer.Deserialize<AppConfig>(json, opts);
                if (cfg == null) throw new InvalidDataException("La configurazione non può essere null: " + path);

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

                return cfg;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Configurazione JSON non valida: " + Path.GetFullPath(path), ex);
            }
        }

        /// <summary>A stable, writable location shared by console, services and WPF.</summary>
        public static string GetDefaultConfigPath()
        {
            string? configured = Environment.GetEnvironmentVariable("FOTOCREADB_CONFIG");
            if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FotoCreaDB", "appsettings.json");
        }

        public static AppConfig LoadConfiguration(string? path = null, bool requireExists = false,
            Action<ServiceLogMessage>? log = null)
        {
            string full = Path.GetFullPath(path ?? GetDefaultConfigPath());
            AppConfig? loaded = LoadFromFile(full);
            if (loaded == null && (requireExists || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FOTOCREADB_CONFIG"))))
                throw new FileNotFoundException("Configurazione non trovata: " + full, full);
            AppConfig config = loaded ?? new AppConfig();
            ServiceCallbackHelper.Info(log, "Configurazione: " + full);
            if (loaded == null) ServiceCallbackHelper.Warning(log,
                "Configurazione assente: uso delle estensioni predefinite per immagini e video.");
            string directory = Path.GetDirectoryName(full)!;
            config.Paths = (config.Paths ?? Array.Empty<string>())
                .Select(p => Path.GetFullPath(p, directory)).ToArray();
            config.NomeDb = Path.GetFullPath(config.NomeDb, directory);
            return config;
        }

        /// <summary>Own mutable collections before publishing callbacks or scheduling work.</summary>
        public static AppConfig Snapshot(AppConfig? config)
        {
            AppConfig snapshot = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(config ?? LoadConfiguration()))!;
            snapshot.EstensioniPermesse = NormalizeSet(snapshot.EstensioniPermesse ?? throw new InvalidDataException("EstensioniPermesse non può essere null."));
            snapshot.ImageExtensions = NormalizeSet(snapshot.ImageExtensions ?? throw new InvalidDataException("ImageExtensions non può essere null."));
            return snapshot;
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
                string full = Path.GetFullPath(path);
                string temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    System.IO.File.WriteAllText(temporary, json);
                    File.Move(temporary, full, true);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
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
                string extension = s.Trim().TrimStart('.').ToLowerInvariant();
                if (extension.Length > 0) result.Add(extension);
            }

            return new System.Collections.Generic.HashSet<string>(result, StringComparer.OrdinalIgnoreCase);
        }
    }
}
