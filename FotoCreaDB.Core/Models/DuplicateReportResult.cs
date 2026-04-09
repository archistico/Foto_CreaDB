using System.Collections.Generic;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Contiene l'esito del caricamento del report duplicati.
    /// </summary>
    public class DuplicateReportResult
    {
        /// <summary>
        /// Indica se il caricamento è terminato correttamente.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Eventuale messaggio finale.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Elenco delle decisioni di deduplicazione.
        /// </summary>
        public List<DuplicateBinaryDecision> Decisions { get; set; }

        /// <summary>
        /// Numero dei gruppi duplicati trovati.
        /// </summary>
        public int DuplicateGroupsCount { get; set; }

        /// <summary>
        /// Numero totale dei file candidati alla cancellazione.
        /// </summary>
        public int FilesToDeleteCount { get; set; }
    }
}