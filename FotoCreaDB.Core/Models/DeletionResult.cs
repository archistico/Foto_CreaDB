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
        /// Numero totale dei record eliminati dal database.
        /// Include sia i record dei file cancellati dal disco,
        /// sia i record orfani bonificati.
        /// </summary>
        public int DatabaseRecordsDeletedCount { get; set; }

        /// <summary>
        /// Numero dei record orfani bonificati dal database
        /// perché il file non era già più presente sul disco.
        /// </summary>
        public int OrphanRecordsDeletedCount { get; set; }
    }
}