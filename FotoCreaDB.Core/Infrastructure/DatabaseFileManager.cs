using System;
using System.IO;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Helper per operazioni sui file di database (cancellazione sicura di file e file temporanei di SQLite).
    /// </summary>
    public static class DatabaseFileManager
    {
        /// <summary>
        /// Cancella il file del database indicato e i file collegati (-wal, -shm, -journal) se presenti.
        /// Restituisce true se almeno un file è stato cancellato.
        /// I messaggi vengono inviati tramite il callback di log fornito.
        /// </summary>
        public static bool DeleteDatabaseFiles(string databasePath, Action<ServiceLogMessage> log = null)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                ServiceCallbackHelper.Warning(log, "Percorso database vuoto, niente da cancellare.");
                return false;
            }

            bool deletedAny = false;

            try
            {
                string full;
                try
                {
                    full = Path.GetFullPath(databasePath);
                }
                catch
                {
                    full = databasePath;
                }

                string[] candidates = new string[]
                {
                    full,
                    full + "-wal",
                    full + "-shm",
                    full + "-journal"
                };

                foreach (string f in candidates)
                {
                    try
                    {
                        if (File.Exists(f))
                        {
                            File.Delete(f);
                            ServiceCallbackHelper.Info(log, "Database file cancellato: " + f);
                            deletedAny = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ServiceCallbackHelper.Error(log, "Errore durante la cancellazione del file: " + f, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceCallbackHelper.Error(log, "Errore nella cancellazione dei file del database.", ex);
            }

            if (!deletedAny)
            {
                ServiceCallbackHelper.Warning(log, "Nessun file di database trovato per: " + databasePath);
            }

            return deletedAny;
        }
    }
}
