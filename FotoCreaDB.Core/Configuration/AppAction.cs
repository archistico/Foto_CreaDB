namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta l'azione principale richiesta da riga di comando.
    /// </summary>
    public enum AppAction
    {
        /// <summary>
        /// Analizza file e aggiorna il database.
        /// </summary>
        Analisi,

        /// <summary>
        /// Legge i duplicati dal database e mostra il report senza cancellare file.
        /// </summary>
        Report,

        /// <summary>
        /// Legge i duplicati dal database e cancella i file candidati.
        /// </summary>
        Cancella
    }
}