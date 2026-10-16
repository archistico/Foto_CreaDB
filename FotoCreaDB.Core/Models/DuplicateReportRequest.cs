namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta i dati necessari per generare il report duplicati.
    /// </summary>
    public class DuplicateReportRequest
    {
        public AppConfig? Configuration { get; set; }

        /// <summary>Optional file/directory scope. Empty means the whole database.</summary>
        public string[] Paths { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// Percorso del database SQLite.
        /// </summary>
        public string NomeDb { get; set; } = string.Empty;

        /// <summary>
        /// Indica se includere il dettaglio completo dei duplicati.
        /// </summary>
        public bool VerboseDuplicates { get; set; }
    }
}
