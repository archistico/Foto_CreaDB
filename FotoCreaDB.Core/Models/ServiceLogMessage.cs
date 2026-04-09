using System;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta un messaggio prodotto da un servizio applicativo.
    /// </summary>
    public class ServiceLogMessage
    {
        /// <summary>
        /// Data e ora del messaggio.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Livello del messaggio.
        /// </summary>
        public ServiceLogLevel Level { get; set; }

        /// <summary>
        /// Testo del messaggio.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Eventuale eccezione associata.
        /// </summary>
        public Exception Exception { get; set; }
    }
}