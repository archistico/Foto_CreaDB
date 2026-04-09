namespace Foto_CreaDB2
{
    /// <summary>
    /// Contiene l'esito dell'operazione di analisi.
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// Indica se l'analisi è terminata correttamente.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Eventuale messaggio finale.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Statistiche raccolte durante la scansione.
        /// </summary>
        public ScanStatistics Statistics { get; set; }
    }
}