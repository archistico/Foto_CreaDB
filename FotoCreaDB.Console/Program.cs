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
            // Carica configurazione da file JSON (se presente) e poi applica override dalle opzioni CLI
            AppConfig baseConfig = AppConfigLoader.LoadFromFile("appsettings.json") ?? new AppConfig();

            // Override con opzioni da linea di comando se fornite
            if (!string.IsNullOrWhiteSpace(options.PathInput))
            {
                baseConfig.Paths = new string[] { options.PathInput };
            }

            if (!string.IsNullOrWhiteSpace(options.NomeDb))
            {
                baseConfig.NomeDb = options.NomeDb;
            }

            baseConfig.CancellaDbSeEsiste = false;
            // LogDettagliato / ProgressEvery possono essere letti dal file di configurazione;
            // non sono esposti come opzioni CLI in questo parser.
            baseConfig.VerboseDuplicates = options.VerboseDuplicates;
            baseConfig.Action = options.Action;

            return baseConfig;
        }

        /// <summary>
        /// Esegue l'analisi dei file e aggiorna il database.
        /// Non esegue report né cancellazioni.
        /// </summary>
        /// <param name="config">Configurazione applicativa.</param>
        /// <param name="logger">Logger dell'applicazione.</param>
        private static void RunAnalisi(AppConfig config, Logger logger)
        {
            AnalysisService service = new AnalysisService();
            ConsoleServiceAdapter adapter = new ConsoleServiceAdapter(logger);

            adapter.Reset();

            AnalysisResult result = service.Run(
                new AnalysisRequest
                {
                    Paths = config.Paths,
                    NomeDb = config.NomeDb,
                    CancellaDbSeEsiste = config.CancellaDbSeEsiste,
                    LogDettagliato = config.LogDettagliato,
                    ProgressEvery = config.ProgressEvery,
                    VerboseDuplicates = config.VerboseDuplicates
                },
                progress: adapter.CreateAnalysisProgress(),
                log: adapter.OnLog);

            logger.WriteLine("");
            logger.WriteLine(result.Message);
        }

        /// <summary>
        /// Legge dal database le decisioni sui duplicati e mostra il report
        /// senza cancellare alcun file.
        /// </summary>
        /// <param name="config">Configurazione applicativa.</param>
        /// <param name="logger">Logger dell'applicazione.</param>
        private static void RunReport(AppConfig config, Logger logger)
        {
            DuplicateReportService service = new DuplicateReportService();
            ConsoleServiceAdapter adapter = new ConsoleServiceAdapter(logger);

            adapter.Reset();

            DuplicateReportResult result = service.Run(
                new DuplicateReportRequest
                {
                    NomeDb = config.NomeDb,
                    VerboseDuplicates = config.VerboseDuplicates
                },
                progress: adapter.CreateReportProgress(),
                log: adapter.OnLog);

            logger.WriteLine("");
            logger.WriteLine("Duplicati trovati                : " + result.DuplicateGroupsCount);
            logger.WriteLine("File candidati alla cancellazione: " + result.FilesToDeleteCount);

            if (config.VerboseDuplicates && result.Decisions != null && result.Decisions.Count > 0)
            {
                DuplicateBinaryReportWriter reportWriter = new DuplicateBinaryReportWriter();
                reportWriter.Write(result.Decisions);
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
            ConsoleServiceAdapter adapter = new ConsoleServiceAdapter(logger);
            DuplicateReportService reportService = new DuplicateReportService();

            adapter.Reset();

            DuplicateReportResult reportResult = reportService.Run(
                new DuplicateReportRequest
                {
                    NomeDb = config.NomeDb,
                    VerboseDuplicates = config.VerboseDuplicates
                },
                progress: adapter.CreateReportProgress(),
                log: adapter.OnLog);

            logger.WriteLine("");
            logger.WriteLine("Duplicati trovati                : " + reportResult.DuplicateGroupsCount);
            logger.WriteLine("File candidati alla cancellazione: " + reportResult.FilesToDeleteCount);

            if (config.VerboseDuplicates && reportResult.Decisions != null && reportResult.Decisions.Count > 0)
            {
                DuplicateBinaryReportWriter reportWriter = new DuplicateBinaryReportWriter();
                reportWriter.Write(reportResult.Decisions);
            }

            if (reportResult.FilesToDeleteCount <= 0)
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
                DuplicateDeletionService deletionService = new DuplicateDeletionService();

                adapter.Reset();

                DeletionResult deletionResult = deletionService.Run(
                    new DeletionRequest
                    {
                        NomeDb = config.NomeDb,
                        VerboseDuplicates = config.VerboseDuplicates
                    },
                    progress: adapter.CreateDeletionProgress(),
                    log: adapter.OnLog);

                logger.WriteLine(deletionResult.Message);
            }
            else
            {
                logger.WriteLine("Cancellazione annullata.");
            }
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