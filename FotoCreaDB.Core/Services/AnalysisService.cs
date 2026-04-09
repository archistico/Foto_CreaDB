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
            Action<ServiceLogMessage> log = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ScanStatistics stats = new ScanStatistics();
            MetadataService metadataService = new MetadataService();
            HashService hashService = new HashService();

            AppConfig config = new AppConfig
            {
                Paths = request.Paths,
                NomeDb = request.NomeDb,
                CancellaDbSeEsiste = request.CancellaDbSeEsiste,
                LogDettagliato = request.LogDettagliato,
                ProgressEvery = request.ProgressEvery,
                VerboseDuplicates = request.VerboseDuplicates
            };

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