using System;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Adatta i callback dei servizi applicativi all'output console.
    /// Traduce log strutturati e avanzamenti in messaggi leggibili
    /// senza introdurre dipendenze della logica applicativa dalla console.
    /// </summary>
    public class ConsoleServiceAdapter
    {
        private readonly Logger _logger;

        private int _lastAnalysisPercentage = -1;
        private int _lastDeletionPercentage = -1;
        private int _lastReportPercentage = -1;

        /// <summary>
        /// Inizializza una nuova istanza dell'adapter console.
        /// </summary>
        /// <param name="logger">
        /// Logger console usato per scrivere messaggi e avanzamento.
        /// </param>
        public ConsoleServiceAdapter(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Callback di log strutturato da passare ai servizi.
        /// </summary>
        /// <param name="message">Messaggio prodotto dal servizio.</param>
        public void OnLog(ServiceLogMessage message)
        {
            if (message == null)
            {
                return;
            }

            string prefix = "[" + message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") + "] "
                + "[" + message.Level.ToString().ToUpper() + "] ";

            if (message.Level == ServiceLogLevel.Error && message.Exception != null)
            {
                _logger.WriteLine(prefix + message.Message + " - " + message.Exception.Message);
                return;
            }

            _logger.WriteLine(prefix + message.Message);
        }

        /// <summary>
        /// Callback di avanzamento per l'analisi file-per-file.
        /// </summary>
        /// <param name="progress">Stato corrente dell'analisi.</param>
        public void OnAnalysisProgress(AnalysisProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            if (progress.TotalFiles <= 0)
            {
                return;
            }

            if (progress.Percentage == _lastAnalysisPercentage
                && progress.ProcessedFiles < progress.TotalFiles)
            {
                return;
            }

            _lastAnalysisPercentage = progress.Percentage;

            string currentFile = string.IsNullOrWhiteSpace(progress.CurrentFile)
                ? ""
                : " | " + progress.CurrentFile;

            _logger.WriteLine(
                "[ANALISI] "
                + progress.ProcessedFiles
                + "/"
                + progress.TotalFiles
                + " ("
                + progress.Percentage
                + "%)"
                + currentFile);
        }

        /// <summary>
        /// Callback di avanzamento per il caricamento del report duplicati.
        /// </summary>
        /// <param name="progress">Stato corrente del report.</param>
        public void OnReportProgress(DuplicateReportProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            if (progress.TotalGroups <= 0 && string.IsNullOrWhiteSpace(progress.CurrentStep))
            {
                return;
            }

            if (progress.Percentage == _lastReportPercentage
                && progress.ProcessedGroups < progress.TotalGroups)
            {
                return;
            }

            _lastReportPercentage = progress.Percentage;

            string step = string.IsNullOrWhiteSpace(progress.CurrentStep)
                ? ""
                : " | " + progress.CurrentStep;

            if (progress.TotalGroups > 0)
            {
                _logger.WriteLine(
                    "[REPORT] "
                    + progress.ProcessedGroups
                    + "/"
                    + progress.TotalGroups
                    + " ("
                    + progress.Percentage
                    + "%)"
                    + step);
            }
            else
            {
                _logger.WriteLine("[REPORT]" + step);
            }
        }

        /// <summary>
        /// Callback di avanzamento per la cancellazione file-per-file.
        /// </summary>
        /// <param name="progress">Stato corrente della cancellazione.</param>
        public void OnDeletionProgress(DeletionProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            if (progress.TotalFiles <= 0)
            {
                return;
            }

            if (progress.Percentage == _lastDeletionPercentage
                && progress.ProcessedFiles < progress.TotalFiles)
            {
                return;
            }

            _lastDeletionPercentage = progress.Percentage;

            string currentFile = string.IsNullOrWhiteSpace(progress.CurrentFile)
                ? ""
                : " | " + progress.CurrentFile;

            _logger.WriteLine(
                "[CANCELLA] "
                + progress.ProcessedFiles
                + "/"
                + progress.TotalFiles
                + " ("
                + progress.Percentage
                + "%)"
                + currentFile);
        }

        /// <summary>
        /// Crea un oggetto IProgress per l'analisi.
        /// </summary>
        /// <returns>Progress adapter per AnalysisProgress.</returns>
        public IProgress<AnalysisProgress> CreateAnalysisProgress()
        {
            return new Progress<AnalysisProgress>(OnAnalysisProgress);
        }

        /// <summary>
        /// Crea un oggetto IProgress per il report.
        /// </summary>
        /// <returns>Progress adapter per DuplicateReportProgress.</returns>
        public IProgress<DuplicateReportProgress> CreateReportProgress()
        {
            return new Progress<DuplicateReportProgress>(OnReportProgress);
        }

        /// <summary>
        /// Crea un oggetto IProgress per la cancellazione.
        /// </summary>
        /// <returns>Progress adapter per DeletionProgress.</returns>
        public IProgress<DeletionProgress> CreateDeletionProgress()
        {
            return new Progress<DeletionProgress>(OnDeletionProgress);
        }

        /// <summary>
        /// Reimposta lo stato interno degli ultimi avanzamenti stampati.
        /// Utile prima di avviare una nuova operazione.
        /// </summary>
        public void Reset()
        {
            _lastAnalysisPercentage = -1;
            _lastDeletionPercentage = -1;
            _lastReportPercentage = -1;
        }
    }
}