namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta lo stato di avanzamento del caricamento report duplicati.
    /// </summary>
    public class DuplicateReportProgress
    {
        /// <summary>
        /// Descrizione sintetica della fase corrente.
        /// </summary>
        public string CurrentStep { get; set; }

        /// <summary>
        /// Numero dei gruppi duplicati già elaborati.
        /// </summary>
        public int ProcessedGroups { get; set; }

        /// <summary>
        /// Numero totale dei gruppi duplicati da elaborare.
        /// </summary>
        public int TotalGroups { get; set; }

        /// <summary>
        /// Percentuale di avanzamento.
        /// </summary>
        public int Percentage
        {
            get
            {
                if (TotalGroups <= 0)
                {
                    return 0;
                }

                return (ProcessedGroups * 100) / TotalGroups;
            }
        }
    }
}