using System;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Gestisce la scrittura dei messaggi di log su console,
    /// inclusi errori, avanzamento elaborazione e statistiche finali.
    /// </summary>
    public class Logger
    {
        private readonly bool _logDettagliato;
        private readonly int _progressEvery;

        /// <summary>
        /// Inizializza una nuova istanza del logger.
        /// </summary>
        /// <param name="logDettagliato">
        /// Indica se ogni file elaborato deve essere scritto in modo dettagliato.
        /// </param>
        /// <param name="progressEvery">
        /// Numero di file elaborati tra un messaggio di avanzamento e il successivo.
        /// Se minore o uguale a zero, viene usato il valore predefinito 1000.
        /// </param>
        public Logger(bool logDettagliato, int progressEvery)
        {
            _logDettagliato = logDettagliato;
            _progressEvery = progressEvery <= 0 ? 1000 : progressEvery;
        }

        /// <summary>
        /// Scrive una riga semplice su console.
        /// </summary>
        /// <param name="message">
        /// Messaggio da visualizzare.
        /// </param>
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Scrive su console un errore con il relativo contesto
        /// e l'eventuale catena delle inner exception.
        /// </summary>
        /// <param name="contesto">
        /// Descrizione sintetica del punto in cui si è verificato l'errore.
        /// </param>
        /// <param name="ex">
        /// Eccezione da registrare.
        /// </param>
        public void WriteError(string contesto, Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine(contesto);
            Console.WriteLine(ex.Message);

            Exception? inner = ex.InnerException;
            while (inner != null)
            {
                Console.WriteLine("INNER: " + inner.Message);
                inner = inner.InnerException;
            }

            Console.WriteLine("==================================================");
            Console.WriteLine();
        }

        /// <summary>
        /// Scrive il risultato dell'elaborazione di un file.
        /// In modalità dettagliata stampa ogni file; altrimenti stampa solo avanzamenti periodici.
        /// </summary>
        /// <param name="action">
        /// Azione eseguita sul file, ad esempio INSERT, UPDATE o SKIP.
        /// </param>
        /// <param name="foto">
        /// File elaborato.
        /// </param>
        /// <param name="stats">
        /// Statistiche correnti della scansione.
        /// </param>
        public void WriteFileProcessed(string action, Foto foto, ScanStatistics stats)
        {
            if (_logDettagliato)
            {
                Console.WriteLine($"{action} | {foto}");
                return;
            }

            if ((stats.TotaleFileElaborati % _progressEvery) == 0)
            {
                Console.WriteLine(
                    $"Elaborati: {stats.TotaleFileElaborati} | Inseriti: {stats.TotaleFileInseriti} | Aggiornati: {stats.TotaleFileAggiornati} | Saltati: {stats.TotaleFileSaltati} | Errori file: {stats.TotaleErroriFile} | Errori DB: {stats.TotaleErroriDb}");
            }
        }

        /// <summary>
        /// Scrive su console il riepilogo finale delle statistiche raccolte durante la scansione.
        /// </summary>
        /// <param name="stats">
        /// Statistiche finali da visualizzare.
        /// </param>
        public void WriteFinalStatistics(ScanStatistics stats)
        {
            Console.WriteLine();
            Console.WriteLine("============== STATISTICHE FINALI ==============");
            Console.WriteLine($"Path iniziali                : {stats.TotalePathIniziali}");
            Console.WriteLine($"Cartelle visitate            : {stats.TotaleCartelleVisitate}");
            Console.WriteLine($"File trovati                 : {stats.TotaleFileTrovati}");
            Console.WriteLine($"File estensione valida       : {stats.TotaleFileConEstensioneValida}");
            Console.WriteLine($"File elaborati               : {stats.TotaleFileElaborati}");
            Console.WriteLine($"File inseriti                : {stats.TotaleFileInseriti}");
            Console.WriteLine($"File aggiornati              : {stats.TotaleFileAggiornati}");
            Console.WriteLine($"File saltati                 : {stats.TotaleFileSaltati}");
            Console.WriteLine($"File marcati mancanti        : {stats.TotaleFileSegnatiComeMancanti}");
            Console.WriteLine($"File senza metadati utili    : {stats.TotaleFileSenzaMetadatiUtili}");
            Console.WriteLine($"Errori file                  : {stats.TotaleErroriFile}");
            Console.WriteLine($"Errori cartelle              : {stats.TotaleErroriCartelle}");
            Console.WriteLine($"Errori metadati              : {stats.TotaleErroriMetadati}");
            Console.WriteLine($"Errori hash                  : {stats.TotaleErroriHash}");
            Console.WriteLine($"Errori DB                    : {stats.TotaleErroriDb}");
            Console.WriteLine("===============================================");
            Console.WriteLine();
        }
    }
}