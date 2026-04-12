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
            IProgress<AnalysisProgress> progress = null,
            Action<ServiceLogMessage>? log = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ScanStatistics stats = new ScanStatistics();

            // Provare a caricare configurazione da file appsettings.json nella directory corrente
            AppConfig fileConfig = AppConfigLoader.LoadFromFile("appsettings.json");

            // Costruisci la config finale partendo dal file (se presente) e applicando gli override dalla request
            AppConfig config = fileConfig ?? new AppConfig();
            config.Paths = request.Paths;
            config.NomeDb = string.IsNullOrWhiteSpace(request.NomeDb) ? config.NomeDb : request.NomeDb;
            config.CancellaDbSeEsiste = request.CancellaDbSeEsiste;
            config.LogDettagliato = request.LogDettagliato;
            config.ProgressEvery = request.ProgressEvery > 0 ? request.ProgressEvery : config.ProgressEvery;
            config.VerboseDuplicates = request.VerboseDuplicates;

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

            ServiceCallbackHelper.Info(log, "Analisi completata.");

            return new AnalysisResult
            {
                Success = true,
                Message = "Analisi completata.",
                Statistics = stats
            };
        }
    }
}