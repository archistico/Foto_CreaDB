using System.IO;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Helper per salvare le preferenze (cartella foto e database) su appsettings.json
    /// senza dipendere dall'interfaccia utente.
    /// </summary>
    public static class PreferencesSaver
    {
        /// <summary>
        /// Salva le preferenze sul file di configurazione specificato.
        /// Ritorna true se il salvataggio è andato a buon fine.
        /// </summary>
        public static bool SavePreferences(string? fotoPath, string? databasePath, string configPath)
        {
            try
            {
                AppConfig cfg = AppConfigLoader.LoadFromFile(configPath) ?? new AppConfig();

                if (!string.IsNullOrWhiteSpace(fotoPath))
                {
                    // store as absolute path
                    cfg.Paths = new[] { Path.GetFullPath(fotoPath) };
                }

                if (!string.IsNullOrWhiteSpace(databasePath))
                {
                    // store DB as absolute path to avoid ambiguities
                    cfg.NomeDb = Path.GetFullPath(databasePath);
                }

                return AppConfigLoader.SaveToFile(cfg, configPath);
            }
            catch
            {
                return false;
            }
        }
    }
}
