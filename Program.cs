using System;
using System.Collections.Generic;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Punto di ingresso dell'applicazione.
    /// Si occupa di interpretare gli argomenti da riga di comando
    /// e di avviare una delle tre modalità disponibili:
    /// analisi, report o cancellazione duplicati.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Avvia l'applicazione console.
        /// </summary>
        /// <param name="args">
        /// Argomenti passati da riga di comando.
        /// </param>
        public static void Main(string[] args)
        {
            Logger logger = null;

            try
            {
                CommandLineOptions options = CommandLineParser.Parse(args);

                if (options == null)
                {
                    WriteUsage();
                    return;
                }

                AppConfig config = BuildConfig(options);

                logger = new Logger(config.LogDettagliato, config.ProgressEvery);

                switch (config.Action)
                {
                    case AppAction.Analisi:
                        RunAnalisi(config, logger);
                        break;

                    case AppAction.Report:
                        RunReport(config, logger);
                        break;

                    case AppAction.Cancella:
                        RunCancella(config, logger);
                        break;

                    default:
                        throw new InvalidOperationException("Azione non gestita.");
                }

                logger.WriteLine("");
                logger.WriteLine("Fine.");
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.WriteError("ERRORE FATALE", ex);
                }
                else
                {
                    Console.WriteLine("ERRORE FATALE");
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine("");
            Console.WriteLine("Premi un tasto per uscire...");
            Console.ReadKey();
        }

        /// <summary>
        /// Costruisce la configurazione applicativa a partire dalle opzioni parse.
        /// </summary>
        /// <param name="options">Opzioni da riga di comando.</param>
        /// <returns>Configurazione pronta all'uso.</returns>
        private static AppConfig BuildConfig(CommandLineOptions options)
        {
            return new AppConfig
            {
                Paths = string.IsNullOrWhiteSpace(options.PathInput)
                    ? new string[0]
                    : new string[] { options.PathInput },
                NomeDb = options.NomeDb,
                CancellaDbSeEsiste = false,
                LogDettagliato = false,
                ProgressEvery = 1000,
                VerboseDuplicates = options.VerboseDuplicates,
                Action = options.Action
            };
        }

        /// <summary>
        /// Esegue l'analisi dei file e aggiorna il database.
        /// Non esegue report né cancellazioni.
        /// </summary>
        /// <param name="config">Configurazione applicativa.</param>
        /// <param name="logger">Logger dell'applicazione.</param>
        private static void RunAnalisi(AppConfig config, Logger logger)
        {
            ScanStatistics stats = new ScanStatistics();
            MetadataService metadataService = new MetadataService();
            HashService hashService = new HashService();

            using (DatabaseManager dbManager = new DatabaseManager(config.NomeDb, config.CancellaDbSeEsiste))
            {
                dbManager.Initialize();

                if (dbManager.Connection == null)
                {
                    throw new InvalidOperationException("La connessione SQLite non è disponibile dopo l'inizializzazione.");
                }

                using (FotoRepository repository = new FotoRepository(dbManager.Connection))
                {
                    FileScanner scanner = new FileScanner(
                        config,
                        repository,
                        metadataService,
                        hashService,
                        logger,
                        stats);

                    scanner.Scan();
                }
            }

            logger.WriteLine("");
            logger.WriteLine("Analisi completata.");
        }

        /// <summary>
        /// Legge dal database le decisioni sui duplicati e mostra il report
        /// senza cancellare alcun file.
        /// </summary>
        /// <param name="config">Configurazione applicativa.</param>
        /// <param name="logger">Logger dell'applicazione.</param>
        private static void RunReport(AppConfig config, Logger logger)
        {
            List<DuplicateBinaryDecision> duplicateDecisions = LoadDuplicateDecisions(config);

            int fileCandidatiAllaCancellazione = CountFilesToDelete(duplicateDecisions);

            logger.WriteLine("");
            logger.WriteLine("Duplicati trovati                : " + duplicateDecisions.Count);
            logger.WriteLine("File candidati alla cancellazione: " + fileCandidatiAllaCancellazione);

            if (config.VerboseDuplicates)
            {
                DuplicateBinaryReportWriter reportWriter = new DuplicateBinaryReportWriter();
                reportWriter.Write(duplicateDecisions);
            }
        }

        /// <summary>
        /// Legge dal database le decisioni sui duplicati e,
        /// dopo conferma dell'utente, cancella i file candidati.
        /// </summary>
        /// <param name="config">Configurazione applicativa.</param>
        /// <param name="logger">Logger dell'applicazione.</param>
        private static void RunCancella(AppConfig config, Logger logger)
        {
            List<DuplicateBinaryDecision> duplicateDecisions = LoadDuplicateDecisions(config);

            int fileCandidatiAllaCancellazione = CountFilesToDelete(duplicateDecisions);

            logger.WriteLine("");
            logger.WriteLine("Duplicati trovati                : " + duplicateDecisions.Count);
            logger.WriteLine("File candidati alla cancellazione: " + fileCandidatiAllaCancellazione);

            if (config.VerboseDuplicates)
            {
                DuplicateBinaryReportWriter reportWriter = new DuplicateBinaryReportWriter();
                reportWriter.Write(duplicateDecisions);
            }

            if (fileCandidatiAllaCancellazione <= 0)
            {
                logger.WriteLine("Nessun file da cancellare.");
                return;
            }

            logger.WriteLine("");
            logger.WriteLine("Procedo alla cancellazione? (S/N)");

            ConsoleKeyInfo keyInfo = Console.ReadKey();

            logger.WriteLine("");
            logger.WriteLine("");

            if (keyInfo.Key == ConsoleKey.S)
            {
                DuplicateBinaryDeletionService deletionService = new DuplicateBinaryDeletionService();
                deletionService.DeleteFiles(duplicateDecisions, logger);
            }
            else
            {
                logger.WriteLine("Cancellazione annullata.");
            }
        }

        /// <summary>
        /// Legge dal database l'elenco delle decisioni relative ai duplicati binari.
        /// </summary>
        /// <param name="config">Configurazione applicativa.</param>
        /// <returns>Elenco delle decisioni di deduplicazione.</returns>
        private static List<DuplicateBinaryDecision> LoadDuplicateDecisions(AppConfig config)
        {
            using (DatabaseManager dbManager = new DatabaseManager(config.NomeDb, false))
            {
                dbManager.Initialize();

                if (dbManager.Connection == null)
                {
                    throw new InvalidOperationException("La connessione SQLite non è disponibile dopo l'inizializzazione.");
                }

                using (FotoRepository repository = new FotoRepository(dbManager.Connection))
                {
                    return repository.GetBinaryDuplicateDecisions();
                }
            }
        }

        /// <summary>
        /// Conta il numero totale di file candidati alla cancellazione
        /// presenti nell'elenco delle decisioni.
        /// </summary>
        /// <param name="decisions">
        /// Elenco delle decisioni di deduplicazione.
        /// </param>
        /// <returns>
        /// Numero totale dei file da eliminare.
        /// </returns>
        private static int CountFilesToDelete(List<DuplicateBinaryDecision> decisions)
        {
            if (decisions == null || decisions.Count == 0)
            {
                return 0;
            }

            int total = 0;

            foreach (DuplicateBinaryDecision decision in decisions)
            {
                if (decision.FileDaEliminare != null)
                {
                    total += decision.FileDaEliminare.Count;
                }
            }

            return total;
        }

        /// <summary>
        /// Mostra le istruzioni di utilizzo da riga di comando.
        /// </summary>
        private static void WriteUsage()
        {
            Console.WriteLine("Uso:");
            Console.WriteLine(@"dotnet run -- analisi ""C:\Percorso\Foto""");
            Console.WriteLine(@"dotnet run -- analisi ""C:\Percorso\Foto"" ""C:\Percorso\DB\foto.db""");
            Console.WriteLine(@"dotnet run -- analisi ""C:\Percorso\Foto"" ""C:\Percorso\DB\foto.db"" --verbose");
            Console.WriteLine("");
            Console.WriteLine(@"dotnet run -- report ""C:\Percorso\DB\foto.db""");
            Console.WriteLine(@"dotnet run -- report ""C:\Percorso\DB\foto.db"" --verbose");
            Console.WriteLine("");
            Console.WriteLine(@"dotnet run -- cancella ""C:\Percorso\DB\foto.db""");
            Console.WriteLine(@"dotnet run -- cancella ""C:\Percorso\DB\foto.db"" --verbose");
            Console.WriteLine("");
            Console.WriteLine("Modalità:");
            Console.WriteLine("- analisi  : analizza file e aggiorna il database");
            Console.WriteLine("- report   : legge il database e mostra i duplicati candidati");
            Console.WriteLine("- cancella : legge il database e cancella i duplicati dopo conferma");
            Console.WriteLine("");
            Console.WriteLine("Note:");
            Console.WriteLine("- In 'analisi' se passi una cartella, la scansione è ricorsiva.");
            Console.WriteLine("- In 'analisi' se passi un file, viene analizzato solo quel file.");
            Console.WriteLine("- Il database NON viene cancellato: la scansione è incrementale.");
            Console.WriteLine("- Con --verbose viene mostrato il dettaglio dei duplicati da tenere/eliminare.");
        }
    }
}