using System;
using System.Collections.Generic;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Punto di ingresso dell'applicazione.
    /// Si occupa di leggere gli argomenti da riga di comando, inizializzare i servizi
    /// principali e avviare il processo di scansione e analisi dei duplicati.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Avvia l'applicazione console.
        /// Legge i parametri di input, prepara configurazione e dipendenze,
        /// esegue la scansione dei file e stampa il report finale dei duplicati binari.
        /// </summary>
        /// <param name="args">
        /// Argomenti passati da riga di comando.
        /// Il primo rappresenta il percorso da analizzare; il secondo, se presente,
        /// rappresenta il percorso del database SQLite.
        /// </param>
        public static void Main(string[] args)
        {
            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                Console.WriteLine("Uso:");
                Console.WriteLine(@"Foto_CreaDB2.exe ""C:\Percorso\Foto""");
                Console.WriteLine(@"Foto_CreaDB2.exe ""C:\Percorso\Foto"" ""C:\Percorso\DB\foto.db""");
                Console.WriteLine("");
                Console.WriteLine("Note:");
                Console.WriteLine("- Se passi una cartella, la scansione è ricorsiva.");
                Console.WriteLine("- Se passi un file, viene analizzato solo quel file.");
                Console.WriteLine("- Il database NON viene cancellato: la scansione è incrementale.");
                return;
            }

            string pathInput = args[0].Trim();
            string nomeDb = (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
                ? args[1].Trim()
                : "foto.db";

            AppConfig config = new AppConfig
            {
                Paths = new string[] { pathInput },
                NomeDb = nomeDb,
                CancellaDbSeEsiste = false,
                LogDettagliato = false,
                ProgressEvery = 1000
            };

            Logger logger = new Logger(config.LogDettagliato, config.ProgressEvery);
            ScanStatistics stats = new ScanStatistics();
            MetadataService metadataService = new MetadataService();
            HashService hashService = new HashService();

            try
            {
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

                        List<DuplicateBinaryDecision> duplicateDecisions = repository.GetBinaryDuplicateDecisions();

                        DuplicateBinaryReportWriter reportWriter = new DuplicateBinaryReportWriter();
                        reportWriter.Write(duplicateDecisions);
                    }
                }

                logger.WriteLine("");
                logger.WriteLine("Fine.");
            }
            catch (Exception ex)
            {
                logger.WriteError("ERRORE FATALE", ex);
            }

            logger.WriteLine("");
            logger.WriteLine("Premi un tasto per uscire...");
            Console.ReadKey();
        }
    }
}