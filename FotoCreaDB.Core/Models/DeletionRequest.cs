namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta i dati necessari per avviare la cancellazione dei duplicati.
    /// </summary>
    public class DeletionRequest
    {
        public AppConfig? Configuration { get; set; }

        /// <summary>Optional file/directory scope. Empty means the whole database.</summary>
        public string[] Paths { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// Percorso del database SQLite.
        /// </summary>
        public string NomeDb { get; set; } = string.Empty;

        /// <summary>
        /// Indica se mostrare il dettaglio dei duplicati.
        /// </summary>
        public bool VerboseDuplicates { get; set; }
    }
}
