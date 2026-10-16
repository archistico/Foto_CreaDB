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
            IProgress<DeletionProgress>? progress = null,
            Action<ServiceLogMessage>? log = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            DuplicateReportService reportService = new DuplicateReportService();
            string databasePath = System.IO.Path.GetFullPath(request.NomeDb);
            AppConfig configuration = AppConfigLoader.Snapshot(request.Configuration);
            string[] paths = (string[])request.Paths.Clone();

            DuplicateReportResult reportResult = reportService.Run(
                new DuplicateReportRequest
                {
                    NomeDb = databasePath,
                    Configuration = configuration,
                    Paths = paths,
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
            var statistics = new DeletionStatistics();

            int deletedCount = deletionService.DeleteFiles(
                decisions,
                databasePath,
                null,
                out deletedDbRecordsCount,
                out orphanDbRecordsDeletedCount,
                progress,
                log,
                statistics);

            bool success = statistics.FilesBlockedCount == 0 && statistics.DatabaseErrorsCount == 0;
            string message = (success ? "Cancellazione completata. " : "Cancellazione incompleta. ")
                + "File eliminati: " + deletedCount
                + " - File bloccati: " + statistics.FilesBlockedCount
                + " - File già mancanti: " + statistics.FilesMissingCount
                + " - Record DB eliminati: " + deletedDbRecordsCount
                + " - Record orfani bonificati: " + orphanDbRecordsDeletedCount
                + " - Errori DB: " + statistics.DatabaseErrorsCount + ".";
            if (success) ServiceCallbackHelper.Info(log, message);
            else ServiceCallbackHelper.Warning(log, message);

            return new DeletionResult
            {
                Success = success,
                Message = message,
                FilesBlockedCount = statistics.FilesBlockedCount,
                FilesMissingCount = statistics.FilesMissingCount,
                DatabaseErrorsCount = statistics.DatabaseErrorsCount,
                FilesToDeleteCount = filesToDeleteCount,
                FilesDeletedCount = deletedCount,
                DatabaseRecordsDeletedCount = deletedDbRecordsCount,
                OrphanRecordsDeletedCount = orphanDbRecordsDeletedCount
            };
        }
    }
}
