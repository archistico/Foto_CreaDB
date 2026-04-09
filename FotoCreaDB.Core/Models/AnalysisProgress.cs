namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta lo stato di avanzamento dell'analisi.
    /// </summary>
    public class AnalysisProgress
    {
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
        public string CurrentFile { get; set; }

        /// <summary>
        /// Percentuale calcolata sull'avanzamento.
        /// </summary>
        public int Percentage
        {
            get
            {
                if (TotalFiles <= 0)
                {
                    return 0;
                }

                return (ProcessedFiles * 100) / TotalFiles;
            }
        }
    }
}