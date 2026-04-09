namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta i dati necessari per generare il report duplicati.
    /// </summary>
    public class DuplicateReportRequest
    {
        /// <summary>
        /// Percorso del database SQLite.
        /// </summary>
        public string NomeDb { get; set; }

        /// <summary>
        /// Indica se includere il dettaglio completo dei duplicati.
        /// </summary>
        public bool VerboseDuplicates { get; set; }
    }
}