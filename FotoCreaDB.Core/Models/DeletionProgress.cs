namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta lo stato di avanzamento della cancellazione.
    /// </summary>
    public class DeletionProgress
    {
        /// <summary>
        /// Numero di file già elaborati.
        /// </summary>
        public int ProcessedFiles { get; set; }

        /// <summary>
        /// Numero totale di file da elaborare.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Percorso del file attualmente in elaborazione.
        /// </summary>
        public string CurrentFile { get; set; }

        /// <summary>
        /// Percentuale di avanzamento.
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