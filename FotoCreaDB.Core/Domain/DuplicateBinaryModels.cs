using System;
using System.Collections.Generic;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta un file candidato all'analisi dei duplicati binari.
    /// Contiene i dati minimi necessari per confrontare più copie dello stesso contenuto.
    /// </summary>
    public class DuplicateBinaryCandidate
    {
        /// <summary>
        /// Identificativo univoco del file nel contesto dell'applicazione o del database.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Percorso completo del file sul disco.
        /// </summary>
        public string PercorsoCompleto { get; set; } = "";

        /// <summary>
        /// Hash SHA256 del contenuto del file, usato per individuare file binariamente identici.
        /// </summary>
        public string HashSha256 { get; set; } = "";

        /// <summary>
        /// Dimensione del file in byte.
        /// </summary>
        public long Dimensione { get; set; }

        /// <summary>
        /// Data di modifica del file in formato testuale ordinabile.
        /// </summary>
        public string DataFileModifica { get; set; } = "";

        /// <summary>
        /// Data di scatto del file, se disponibile.
        /// Viene usata come ulteriore verifica di compatibilità tra candidati.
        /// </summary>
        public string DataScatto { get; set; } = "";
    }

    /// <summary>
    /// Rappresenta la decisione finale per un gruppo di duplicati binari:
    /// quale file tenere e quali file eliminare.
    /// </summary>
    public class DuplicateBinaryDecision
    {
        /// <summary>
        /// Hash SHA256 condiviso dai file considerati duplicati.
        /// </summary>
        public string HashSha256 { get; set; } = "";

        /// <summary>
        /// Dimensione del file di riferimento del gruppo.
        /// </summary>
        public long Dimensione { get; set; }

        /// <summary>
        /// Data di scatto di riferimento del gruppo, se presente almeno in uno dei file.
        /// </summary>
        public string? DataScattoRiferimento { get; set; }

        /// <summary>
        /// File scelto da conservare nel gruppo dei duplicati.
        /// </summary>
        public DuplicateBinaryCandidate FileDaTenere { get; set; } = new DuplicateBinaryCandidate();

        /// <summary>
        /// Elenco dei file che risultano duplicati e candidati all'eliminazione.
        /// </summary>
        public List<DuplicateBinaryCandidate> FileDaEliminare { get; set; } = new List<DuplicateBinaryCandidate>();
    }
}