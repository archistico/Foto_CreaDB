namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta i dati necessari per avviare un'analisi dei file.
    /// </summary>
    public class AnalysisRequest
    {
        public AppConfig? Configuration { get; set; }

        /// <summary>
        /// Percorsi da analizzare.
        /// </summary>
        public string[] Paths { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// Percorso del database SQLite.
        /// </summary>
        public string NomeDb { get; set; } = string.Empty;

        /// <summary>
        /// Indica se il database va cancellato se già esiste.
        /// </summary>
        public bool CancellaDbSeEsiste { get; set; }

        /// <summary>
        /// Indica se mostrare dettagli aggiuntivi nel log.
        /// </summary>
        public bool LogDettagliato { get; set; }

        /// <summary>
        /// Frequenza di avanzamento per il logger.
        /// </summary>
        public int ProgressEvery { get; set; }

        /// <summary>
        /// Indica se mostrare il dettaglio dei duplicati.
        /// In analisi non è indispensabile, ma resta disponibile.
        /// </summary>
        public bool VerboseDuplicates { get; set; }
    }
}
