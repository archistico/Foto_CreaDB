using System;
using System.Collections.Generic;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Gestisce la cancellazione dei file duplicati basandosi sui dati del database.
    /// </summary>
    public class DuplicateDeletionService
    {
        /// <summary>
        /// Esegue la cancellazione dei file candidati.
        /// </summary>
        /// <param name="request">Dati necessari per la cancellazione.</param>
        /// <param name="progress">Callback di avanzamento.</param>
        /// <param name="log">Callback di log.</param>
        /// <returns>Esito della cancellazione.</returns>
        public DeletionResult Run(
            DeletionRequest request,
            IProgress<DeletionProgress> progress = null,
            Action<ServiceLogMessage> log = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            DuplicateReportService reportService = new DuplicateReportService();

            DuplicateReportResult reportResult = reportService.Run(
                new DuplicateReportRequest
                {
                    NomeDb = request.NomeDb,
                    VerboseDuplicates = request.VerboseDuplicates
                },
                null,
                log);

            List<DuplicateBinaryDecision> decisions = reportResult.Decisions ?? new List<DuplicateBinaryDecision>();
            int filesToDeleteCount = reportResult.FilesToDeleteCount;

            if (filesToDeleteCount <= 0)
            {
                ServiceCallbackHelper.Info(log, "Nessun file da cancellare.");

                ServiceCallbackHelper.ReportProgress(
                    progress,
                    new DeletionProgress
                    {
                        ProcessedFiles = 0,
                        TotalFiles = 0,
                        CurrentFile = null
                    });

                return new DeletionResult
                {
                    Success = true,
                    Message = "Nessun file da cancellare.",
                    FilesToDeleteCount = 0,
                    FilesDeletedCount = 0,
                    DatabaseRecordsDeletedCount = 0,
                    OrphanRecordsDeletedCount = 0
                };
            }

            ServiceCallbackHelper.Info(log, "Avvio cancellazione duplicati.");

            DuplicateBinaryDeletionService deletionService = new DuplicateBinaryDeletionService();

            int deletedDbRecordsCount;
            int orphanDbRecordsDeletedCount;

            int deletedCount = deletionService.DeleteFiles(
                decisions,
                request.NomeDb,
                null,
                out deletedDbRecordsCount,
                out orphanDbRecordsDeletedCount,
                progress,
                log);

            ServiceCallbackHelper.Info(log, "Cancellazione completata.");

            return new DeletionResult
            {
                Success = true,
                Message =
                    "Cancellazione completata. "
                    + "File eliminati: " + deletedCount
                    + " - Record DB eliminati: " + deletedDbRecordsCount
                    + " - Record orfani bonificati: " + orphanDbRecordsDeletedCount + ".",
                FilesToDeleteCount = filesToDeleteCount,
                FilesDeletedCount = deletedCount,
                DatabaseRecordsDeletedCount = deletedDbRecordsCount,
                OrphanRecordsDeletedCount = orphanDbRecordsDeletedCount
            };
        }
    }
}