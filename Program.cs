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
        /// esegue la scansione dei file, gestisce i duplicati binari
        /// e, su conferma dell'utente, procede con la cancellazione dei file selezionati.
        /// </summary>
        /// <param name="args">
        /// Argomenti passati da riga di comando.
        /// Il primo rappresenta il percorso da analizzare; il secondo, se presente,
        /// rappresenta il percorso del database SQLite.
        /// I flag opzionali possono includere <c>--verbose</c>.
        /// </param>
        public static void Main(string[] args)
        {
            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                Console.WriteLine("Uso:");
                Console.WriteLine(@"Foto_CreaDB2.exe ""C:\Percorso\Foto""");
                Console.WriteLine(@"Foto_CreaDB2.exe ""C:\Percorso\Foto"" ""C:\Percorso\DB\foto.db""");
                Console.WriteLine(@"Foto_CreaDB2.exe ""C:\Percorso\Foto"" --verbose");
                Console.WriteLine(@"Foto_CreaDB2.exe ""C:\Percorso\Foto"" ""C:\Percorso\DB\foto.db"" --verbose");
                Console.WriteLine("");
                Console.WriteLine("Note:");
                Console.WriteLine("- Se passi una cartella, la scansione è ricorsiva.");
                Console.WriteLine("- Se passi un file, viene analizzato solo quel file.");
                Console.WriteLine("- Il database NON viene cancellato: la scansione è incrementale.");
                Console.WriteLine("- Con --verbose viene mostrato il dettaglio dei duplicati da tenere/eliminare.");
                return;
            }

            string pathInput = args[0].Trim();

            string nomeDb = "foto.db";
            bool verboseDuplicates = false;

            for (int i = 1; i < args.Length; i++)
            {
                string currentArg = (args[i] ?? "").Trim();

                if (string.Equals(currentArg, "--verbose", StringComparison.OrdinalIgnoreCase))
                {
                    verboseDuplicates = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(currentArg))
                {
                    continue;
                }

                if (string.Equals(nomeDb, "foto.db", StringComparison.OrdinalIgnoreCase))
                {
                    nomeDb = currentArg;
                }
            }

            AppConfig config = new AppConfig
            {
                Paths = new string[] { pathInput },
                NomeDb = nomeDb,
                CancellaDbSeEsiste = false,
                LogDettagliato = false,
                ProgressEvery = 1000,
                VerboseDuplicates = verboseDuplicates
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
                        int fileCandidatiAllaCancellazione = CountFilesToDelete(duplicateDecisions);

                        logger.WriteLine("");
                        logger.WriteLine("Duplicati trovati                : " + duplicateDecisions.Count);
                        logger.WriteLine("File candidati alla cancellazione: " + fileCandidatiAllaCancellazione);

                        if (config.VerboseDuplicates)
                        {
                            DuplicateBinaryReportWriter reportWriter = new DuplicateBinaryReportWriter();
                            reportWriter.Write(duplicateDecisions);
                        }

                        if (fileCandidatiAllaCancellazione > 0)
                        {
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
                        else
                        {
                            logger.WriteLine("Nessun file da cancellare.");
                        }
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
    }
}