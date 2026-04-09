using System;
using System.Collections.Generic;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Contiene i parametri di configurazione principali dell'applicazione.
    /// Definisce i percorsi da analizzare, il database da usare e alcune opzioni operative.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Elenco dei percorsi di input da analizzare.
        /// Ogni elemento può rappresentare una cartella o un file.
        /// </summary>
        public string[] Paths { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Nome o percorso del database SQLite da utilizzare.
        /// </summary>
        public string NomeDb { get; set; } = "foto.db";

        /// <summary>
        /// Indica se il database esistente deve essere eliminato prima della nuova inizializzazione.
        /// </summary>
        public bool CancellaDbSeEsiste { get; set; } = false;

        /// <summary>
        /// Indica se il programma deve scrivere log più dettagliati durante l'esecuzione.
        /// </summary>
        public bool LogDettagliato { get; set; } = false;

        /// <summary>
        /// Numero di elementi elaborati tra un aggiornamento di avanzamento e il successivo.
        /// </summary>
        public int ProgressEvery { get; set; } = 1000;

        /// <summary>
        /// Indica se deve essere stampato il report dettagliato dei duplicati binari.
        /// </summary>
        public bool VerboseDuplicates { get; set; } = false;

        /// <summary>
        /// Azione richiesta dall'utente da riga di comando.
        /// </summary>
        public AppAction Action { get; set; }

        /// <summary>
        /// Insieme delle estensioni file consentite durante la scansione.
        /// Il confronto viene eseguito senza distinzione tra maiuscole e minuscole.
        /// </summary>
        public HashSet<string> EstensioniPermesse { get; set; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "jpg", "jpeg", "dcr", "dng", "crw",
                "gif", "png", "ico", "raw", "arw", "cr2",
                "avi", "wmv", "wav", "mp4", "mp3", "mov",
                "bmp", "psd", "3gp", "webp", "heic", "heif", "tiff", "tif"
            };
    }
}