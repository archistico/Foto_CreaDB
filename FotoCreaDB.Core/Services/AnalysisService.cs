using System;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Gestisce il processo di analisi dei file e aggiornamento del database.
    /// </summary>
    public class AnalysisService
    {
        /// <summary>
        /// Esegue l'analisi dei file richiesti.
        /// </summary>
        /// <param name="request">Dati necessari per l'analisi.</param>
        /// <param name="progress">Callback di avanzamento.</param>
        /// <param name="log">Callback di log.</param>
        /// <returns>Esito dell'operazione.</returns>
        public AnalysisResult Run(
            AnalysisRequest request,
            IProgress<AnalysisProgress>? progress = null,
            Action<ServiceLogMessage>? log = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Validate before opening/resetting the database.
            if (request.Paths == null || request.Paths.Length == 0)
                throw new ArgumentException("Non è stato specificato alcun path da analizzare.", nameof(request));
            // Own the normalized inputs before callbacks can change them.
            string[] paths = new string[request.Paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(request.Paths[i]))
                    throw new ArgumentException("Il path da analizzare non può essere vuoto.", nameof(request));
                paths[i] = System.IO.Path.GetFullPath(request.Paths[i]);
            }

            ScanStatistics stats = new ScanStatistics();

            // Provare a caricare configurazione da file appsettings.json nella directory corrente
            AppConfig? fileConfig = AppConfigLoader.LoadFromFile("appsettings.json");

            // Costruisci la config finale partendo dal file (se presente) e applicando gli override dalla request
            AppConfig config = fileConfig ?? new AppConfig();
            config.Paths = paths;
            config.NomeDb = System.IO.Path.GetFullPath(string.IsNullOrWhiteSpace(request.NomeDb) ? config.NomeDb : request.NomeDb);
            config.CancellaDbSeEsiste = request.CancellaDbSeEsiste;
            config.LogDettagliato = request.LogDettagliato;
            config.ProgressEvery = request.ProgressEvery > 0 ? request.ProgressEvery : config.ProgressEvery;
            config.VerboseDuplicates = request.VerboseDuplicates;

            if (config.CancellaDbSeEsiste)
            {
                foreach (string full in config.Paths)
                {
                    if (!System.IO.File.Exists(full) && !System.IO.Directory.Exists(full))
                        throw new ArgumentException("Reset database rifiutato: path non valido o non accessibile: " + full, nameof(request));
                    if ((System.IO.File.GetAttributes(full) & System.IO.FileAttributes.ReparsePoint) != 0)
                        throw new ArgumentException("Reset database rifiutato: il path è un collegamento ignorato dalla scansione: " + full, nameof(request));
                    if (System.IO.File.Exists(full) && (!config.EstensioniPermesse.Contains(Utility.GetEstensione(full))
                        || string.Equals(full, config.NomeDb, StringComparison.OrdinalIgnoreCase)))
                        throw new ArgumentException("Reset database rifiutato: il file selezionato non è processabile: " + full, nameof(request));
                }
            }

            MetadataService metadataService = new MetadataService(config);
            HashService hashService = new HashService();

            ServiceCallbackHelper.Info(log, "Avvio analisi.");

            using (DatabaseManager dbManager = new DatabaseManager(config.NomeDb, config.CancellaDbSeEsiste))
            {
                dbManager.Initialize();

                if (dbManager.Connection == null)
                {
                    throw new InvalidOperationException("La connessione SQLite non è disponibile dopo l'inizializzazione.");
                }

                ServiceCallbackHelper.Info(log, "Database inizializzato.");

                using (FotoRepository repository = new FotoRepository(dbManager.Connection))
                {
                    FileScanner scanner = new FileScanner(
                        config,
                        repository,
                        metadataService,
                        hashService,
                        null,
                        stats,
                        progress,
                        log);

                    scanner.Scan();
                }
            }

            bool success = stats.TotaleErroriPath == 0 && stats.TotaleErroriFile == 0
                && stats.TotaleErroriCartelle == 0 && stats.TotaleErroriDb == 0 && stats.TotaleErroriHash == 0;
            string message = success ? "Analisi completata."
                : "Analisi terminata con errori. Verificare il log prima di cancellare duplicati.";
            if (success) ServiceCallbackHelper.Info(log, message);
            else ServiceCallbackHelper.Warning(log, message);

            return new AnalysisResult
            {
                Success = success,
                Message = message,
                Statistics = stats
            };
        }
    }
}
