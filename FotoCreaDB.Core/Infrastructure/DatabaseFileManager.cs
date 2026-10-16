using System;
using System.IO;
using System.Collections.Generic;

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
                deletedAny = DeleteVerifiedDatabase(databasePath);
                if (deletedAny)
                    ServiceCallbackHelper.Info(log, "Database file cancellato: " + Path.GetFullPath(databasePath));
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

        /// <summary>
        /// Checks the SQLite signature before any deletion and holds exclusive
        /// handles to the database and sidecars. Refuses active databases and links.
        /// </summary>
        internal static bool DeleteVerifiedDatabase(string databasePath)
        {
            string full = Path.GetFullPath(databasePath);
            var files = new List<FileStream>();
            try
            {
                FileStream database;
                try { database = LockedFile.Open(full, true); }
                catch (FileNotFoundException) { return false; }
                files.Add(database);
                Span<byte> signature = stackalloc byte[16];
                if (database.ReadAtLeast(signature, signature.Length, false) != signature.Length ||
                    !signature.SequenceEqual("SQLite format 3\0"u8))
                    throw new InvalidDataException("Cancellazione rifiutata: il file non ha una firma SQLite valida.");

                foreach (string suffix in new[] { "-wal", "-shm", "-journal" })
                {
                    try { files.Add(LockedFile.Open(full + suffix, true)); }
                    catch (FileNotFoundException) { }
                }
                for (int i = files.Count - 1; i >= 0; i--)
                    LockedFile.Delete(files[i]);
                return true;
            }
            finally
            {
                // Keep the database locked until all sidecars are closed/deleted.
                for (int i = files.Count - 1; i >= 0; i--)
                    files[i].Dispose();
            }
        }
    }
}
