using System;
using System.Collections.Generic;

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
            IProgress<DuplicateReportProgress> progress = null,
            Action<ServiceLogMessage> log = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            List<DuplicateBinaryDecision> decisions;

            ServiceCallbackHelper.Info(log, "Avvio caricamento report duplicati.");
            ServiceCallbackHelper.ReportProgress(
                progress,
                new DuplicateReportProgress
                {
                    CurrentStep = "Apertura database",
                    ProcessedGroups = 0,
                    TotalGroups = 0
                });

            using (DatabaseManager dbManager = new DatabaseManager(request.NomeDb, false))
            {
                dbManager.Initialize();

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

                    decisions = repository.GetBinaryDuplicateDecisions();
                }
            }

            int totalGroups = decisions != null ? decisions.Count : 0;
            int filesToDelete = CountFilesToDelete(decisions);

            ServiceCallbackHelper.ReportProgress(
                progress,
                new DuplicateReportProgress
                {
                    CurrentStep = "Report completato",
                    ProcessedGroups = totalGroups,
                    TotalGroups = totalGroups
                });

            ServiceCallbackHelper.Info(log, "Report duplicati caricato.");

            return new DuplicateReportResult
            {
                Success = true,
                Message = "Report duplicati caricato.",
                Decisions = decisions,
                DuplicateGroupsCount = totalGroups,
                FilesToDeleteCount = filesToDelete
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
    }
}