namespace Foto_CreaDB2
{
    /// <summary>
    /// Contiene l'esito dell'operazione di cancellazione.
    /// </summary>
    public class DeletionResult
    {
        /// <summary>
        /// Indica se la cancellazione è terminata correttamente.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Eventuale messaggio finale.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Numero totale dei file candidati alla cancellazione.
        /// </summary>
        public int FilesToDeleteCount { get; set; }

        /// <summary>
        /// Numero dei file eliminati con successo dal disco.
        /// </summary>
        public int FilesDeletedCount { get; set; }

        /// <summary>
        /// Numero dei record eliminati con successo dal database.
        /// </summary>
        public int DatabaseRecordsDeletedCount { get; set; }
    }
}