using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Gestisce il caricamento del report duplicati a partire dai dati presenti nel database.
    /// </summary>
    public class DuplicateReportService
    {
        /// <summary>
        /// Carica dal database le decisioni di deduplicazione.
        /// </summary>
        /// <param name="request">Dati necessari per il report.</param>
        /// <param name="progress">Callback di avanzamento.</param>
        /// <param name="log">Callback di log.</param>
        /// <returns>Esito del caricamento report.</returns>
        public DuplicateReportResult Run(
            DuplicateReportRequest request,
            IProgress<DuplicateReportProgress>? progress = null,
            Action<ServiceLogMessage>? log = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            List<DuplicateBinaryDecision> decisions;
            AppConfig configuration = AppConfigLoader.Snapshot(request.Configuration);
            var scope = new DuplicateScope(configuration, request.Paths);
            string databasePath = System.IO.Path.GetFullPath(request.NomeDb);
            bool verboseDuplicates = request.VerboseDuplicates;
            string[] paths = (string[])request.Paths.Clone();

            ServiceCallbackHelper.Info(log, "Avvio caricamento report duplicati.");
            ServiceCallbackHelper.ReportProgress(
                progress,
                new DuplicateReportProgress
                {
                    CurrentStep = "Apertura database",
                    ProcessedGroups = 0,
                    TotalGroups = 0
                });

            ServiceCallbackHelper.Info(log, "Estensioni del report e della cancellazione: "
                + string.Join(", ", configuration.EstensioniPermesse));
            ServiceCallbackHelper.Info(log, paths.Length == 0 ? "Ambito: tutte le cartelle del database."
                : "Ambito: " + string.Join(", ", paths));

            using (DatabaseManager dbManager = new DatabaseManager(databasePath, false))
            {
                dbManager.OpenExistingReadOnly();

                if (dbManager.Connection == null)
                {
                    throw new InvalidOperationException("La connessione SQLite non è disponibile dopo l'inizializzazione.");
                }

                using (FotoRepository repository = new FotoRepository(dbManager.Connection))
                {
                    ServiceCallbackHelper.ReportProgress(
                        progress,
                        new DuplicateReportProgress
                        {
                            CurrentStep = "Lettura duplicati",
                            ProcessedGroups = 0,
                            TotalGroups = 0
                        });

                    var candidates = repository.GetAllExistingFilesForBinaryDuplicateAnalysis();
                    decisions = new DuplicateBinaryService().BuildDecisions(candidates.Where(scope.Includes).ToList());
                }
            }

            int totalGroups = decisions.Count;
            int filesToDelete = CountFilesToDelete(decisions);
            long bytesToDelete = CountBytesToDelete(decisions);

            ServiceCallbackHelper.ReportProgress(
                progress,
                new DuplicateReportProgress
                {
                    CurrentStep = "Preparazione riepilogo",
                    ProcessedGroups = 0,
                    TotalGroups = 0
                });

            if (decisions.Count > 0)
            {
                ServiceCallbackHelper.Info(log, "Gruppi duplicati: " + totalGroups
                    + " - File candidati alla cancellazione: " + filesToDelete
                    + " - Spazio occupato dai duplicati: " + FormatBytes(bytesToDelete));
            }

            if (decisions.Count > 0 && verboseDuplicates && log != null)
            {
                ServiceCallbackHelper.ReportProgress(progress, new DuplicateReportProgress
                {
                    CurrentStep = "Scrittura dettagli", TotalGroups = totalGroups
                });
                var detailsClock = Stopwatch.StartNew();
                int processedGroups = 0;
                ServiceCallbackHelper.Info(log, "Report duplicati: dettagli gruppi e file:");

                foreach (DuplicateBinaryDecision decision in decisions)
                {
                    ServiceCallbackHelper.Info(log, "Gruppo SHA-256: " + decision.HashSha256);
                    string keepPath = decision.FileDaTenere?.PercorsoCompleto ?? string.Empty;
                    string keepSize = FormatBytes(decision.FileDaTenere?.Dimensione ?? decision.Dimensione);

                    ServiceCallbackHelper.Info(log, "Tenere (" + keepSize + "): " + keepPath);

                    if (decision.FileDaEliminare != null)
                    {
                        foreach (DuplicateBinaryCandidate dup in decision.FileDaEliminare)
                        {
                            string dupPath = dup?.PercorsoCompleto ?? string.Empty;
                            long dupBytes = dup?.Dimensione ?? 0;
                            string dupSize = FormatBytes(dupBytes);

                            ServiceCallbackHelper.Info(log, "    Cancellare (" + dupSize + "): " + dupPath);
                        }
                    }
                    processedGroups++;
                    if (processedGroups < totalGroups && detailsClock.ElapsedMilliseconds >= 250)
                    {
                        ServiceCallbackHelper.ReportProgress(progress, new DuplicateReportProgress
                        {
                            CurrentStep = "Scrittura dettagli", ProcessedGroups = processedGroups, TotalGroups = totalGroups
                        });
                        detailsClock.Restart();
                    }
                }
            }
            else if (decisions.Count == 0)
            {
                ServiceCallbackHelper.Info(log, "Nessun gruppo duplicati trovato.");
            }

            ServiceCallbackHelper.Info(log, "Report duplicati caricato.");
            ServiceCallbackHelper.ReportProgress(progress, new DuplicateReportProgress
            {
                CurrentStep = "Report completato", ProcessedGroups = totalGroups, TotalGroups = totalGroups
            });

            return new DuplicateReportResult
            {
                Success = true,
                Message = "Report duplicati caricato.",
                Decisions = decisions,
                DuplicateGroupsCount = totalGroups,
                FilesToDeleteCount = filesToDelete,
                FilesToDeleteSize = bytesToDelete
            };
        }

        /// <summary>
        /// Conta il numero totale dei file candidati alla cancellazione.
        /// </summary>
        /// <param name="decisions">Decisioni di deduplicazione.</param>
        /// <returns>Numero totale dei file da eliminare.</returns>
        private int CountFilesToDelete(List<DuplicateBinaryDecision> decisions)
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

        private static long CountBytesToDelete(List<DuplicateBinaryDecision> decisions)
        {
            long bytes = 0;
            foreach (var decision in decisions)
            {
                if (decision.FileDaEliminare == null) continue;
                foreach (var candidate in decision.FileDaEliminare)
                    bytes += candidate?.Dimensione ?? 0;
            }
            return bytes;
        }

        private static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
            {
                return (bytes / (double)GB).ToString("0.00", CultureInfo.InvariantCulture) + " GB";
            }

            if (bytes >= MB)
            {
                return (bytes / (double)MB).ToString("0.00", CultureInfo.InvariantCulture) + " MB";
            }

            if (bytes >= KB)
            {
                return (bytes / (double)KB).ToString("0.00", CultureInfo.InvariantCulture) + " KB";
            }

            return bytes + " B";
        }
    }
}
