namespace Foto_CreaDB2
{
    /// <summary>
    /// Contiene le opzioni ricavate dagli argomenti passati da riga di comando.
    /// </summary>
    public class CommandLineOptions
    {
        /// <summary>
        /// Azione richiesta.
        /// </summary>
        public AppAction Action { get; set; }

        /// <summary>
        /// Percorso da analizzare.
        /// Usato solo nella modalità Analisi.
        /// </summary>
        public string PathInput { get; set; }

        /// <summary>
        /// Percorso del database SQLite.
        /// </summary>
        public string NomeDb { get; set; }

        /// <summary>
        /// Indica se mostrare il dettaglio completo dei duplicati.
        /// </summary>
        public bool VerboseDuplicates { get; set; }
    }
}