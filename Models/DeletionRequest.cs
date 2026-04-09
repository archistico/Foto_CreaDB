namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta i dati necessari per avviare la cancellazione dei duplicati.
    /// </summary>
    public class DeletionRequest
    {
        /// <summary>
        /// Percorso del database SQLite.
        /// </summary>
        public string NomeDb { get; set; }

        /// <summary>
        /// Indica se mostrare il dettaglio dei duplicati.
        /// </summary>
        public bool VerboseDuplicates { get; set; }
    }
}