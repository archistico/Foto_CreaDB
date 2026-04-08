namespace Foto_CreaDB2
{
    /// <summary>
    /// Raccoglie i contatori statistici prodotti durante la scansione dei file.
    /// Viene aggiornata progressivamente dai vari servizi coinvolti nell'elaborazione.
    /// </summary>
    public class ScanStatistics
    {
        /// <summary>
        /// Numero totale di path iniziali passati in input all'applicazione.
        /// </summary>
        public long TotalePathIniziali { get; set; }

        /// <summary>
        /// Numero totale di cartelle visitate durante la scansione ricorsiva.
        /// </summary>
        public long TotaleCartelleVisitate { get; set; }

        /// <summary>
        /// Numero totale di file trovati durante la scansione.
        /// </summary>
        public long TotaleFileTrovati { get; set; }

        /// <summary>
        /// Numero di file con estensione ammessa dalla configurazione.
        /// </summary>
        public long TotaleFileConEstensioneValida { get; set; }

        /// <summary>
        /// Numero totale di file effettivamente elaborati.
        /// </summary>
        public long TotaleFileElaborati { get; set; }

        /// <summary>
        /// Numero di file inseriti come nuovi record nel database.
        /// </summary>
        public long TotaleFileInseriti { get; set; }

        /// <summary>
        /// Numero di file già presenti nel database che sono stati aggiornati.
        /// </summary>
        public long TotaleFileAggiornati { get; set; }

        /// <summary>
        /// Numero di file saltati perché non richiedevano una nuova elaborazione completa.
        /// </summary>
        public long TotaleFileSaltati { get; set; }

        /// <summary>
        /// Numero di file segnati come mancanti rispetto all'ultima scansione.
        /// </summary>
        public long TotaleFileSegnatiComeMancanti { get; set; }

        /// <summary>
        /// Numero totale di errori avvenuti durante l'elaborazione dei file.
        /// </summary>
        public long TotaleErroriFile { get; set; }

        /// <summary>
        /// Numero totale di errori avvenuti durante la scansione delle cartelle.
        /// </summary>
        public long TotaleErroriCartelle { get; set; }

        /// <summary>
        /// Numero totale di errori durante la lettura dei metadati.
        /// </summary>
        public long TotaleErroriMetadati { get; set; }

        /// <summary>
        /// Numero totale di errori durante il calcolo dell'hash dei file.
        /// </summary>
        public long TotaleErroriHash { get; set; }

        /// <summary>
        /// Numero totale di errori avvenuti nelle operazioni verso il database.
        /// </summary>
        public long TotaleErroriDb { get; set; }

        /// <summary>
        /// Numero di file per cui non è stato trovato alcun metadato utile.
        /// </summary>
        public long TotaleFileSenzaMetadatiUtili { get; set; }
    }
}