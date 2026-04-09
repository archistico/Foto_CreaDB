namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta le informazioni essenziali di una foto già presente nel database.
    /// Viene usata per confrontare lo stato del database con i file realmente presenti su disco.
    /// </summary>
    public class ExistingFotoInfo
    {
        /// <summary>
        /// Identificativo univoco del record nel database.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Percorso completo del file sul disco.
        /// </summary>
        public string PercorsoCompleto { get; set; } = "";

        /// <summary>
        /// Data di ultima modifica del file in formato testuale.
        /// </summary>
        public string DataFileModifica { get; set; } = "";

        /// <summary>
        /// Data di scatto estratta dai metadati, se disponibile.
        /// </summary>
        public string DataScatto { get; set; } = "";

        /// <summary>
        /// Dimensione del file in byte.
        /// </summary>
        public long Dimensione { get; set; }

        /// <summary>
        /// Hash SHA256 del contenuto del file.
        /// </summary>
        public string HashSha256 { get; set; } = "";

        /// <summary>
        /// Indica se il file risulta ancora presente sul disco al momento del controllo.
        /// </summary>
        public bool FileEsiste { get; set; }

        /// <summary>
        /// Data e ora dell'ultima scansione associata al record.
        /// </summary>
        public string DataScansione { get; set; } = "";
    }
}