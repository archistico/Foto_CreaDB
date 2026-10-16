namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta lo stato di avanzamento dell'analisi.
    /// </summary>
    public class AnalysisProgress
    {
        /// <summary>Fase corrente. I chiamanti esistenti descrivono l'analisi.</summary>
        public AnalysisPhase Phase { get; set; } = AnalysisPhase.AnalyzingFiles;

        /// <summary>File processabili individuati durante il conteggio preliminare.</summary>
        public int DiscoveredFiles { get; set; }

        /// <summary>Cartella corrente durante la ricerca dei file.</summary>
        public string? CurrentDirectory { get; set; }

        /// <summary>
        /// Numero di file già analizzati.
        /// </summary>
        public int ProcessedFiles { get; set; }

        /// <summary>
        /// Numero totale di file da analizzare.
        /// Se non noto, può restare a 0.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Percorso del file attualmente in lavorazione.
        /// </summary>
        public string? CurrentFile { get; set; }

        /// <summary>
        /// Percentuale calcolata sull'avanzamento.
        /// </summary>
        public int Percentage
        {
            get
            {
                if (Phase == AnalysisPhase.DiscoveringFiles || TotalFiles <= 0)
                {
                    return 0;
                }

                return (ProcessedFiles * 100) / TotalFiles;
            }
        }
    }
}
