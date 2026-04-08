using System;
using System.Globalization;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta un file multimediale acquisito durante la scansione,
    /// con i relativi dati tecnici, metadati e chiavi usate per il rilevamento dei duplicati.
    /// </summary>
    public class Foto
    {
        /// <summary>
        /// Percorso completo del file sul disco.
        /// </summary>
        public string percorsoCompleto { get; set; } = "";

        /// <summary>
        /// Cartella che contiene il file.
        /// </summary>
        public string cartella { get; set; } = "";

        /// <summary>
        /// Nome del file comprensivo di estensione.
        /// </summary>
        public string nomefile { get; set; } = "";

        /// <summary>
        /// Versione normalizzata del nome file, usata per confronti e ricerche.
        /// </summary>
        public string nomefileNorm { get; set; } = "";

        /// <summary>
        /// Estensione del file senza distinzione di maiuscole e minuscole.
        /// </summary>
        public string estensione { get; set; } = "";

        /// <summary>
        /// MIME type rilevato per il file, se disponibile.
        /// </summary>
        public string mimeType { get; set; } = "";

        /// <summary>
        /// Dimensione del file in byte.
        /// </summary>
        public long dimensione { get; set; } = 0;

        /// <summary>
        /// Data di ultima modifica del file in formato testuale.
        /// </summary>
        public string dataFileModifica { get; set; } = "";

        /// <summary>
        /// Data di scatto estratta dai metadati, se disponibile.
        /// </summary>
        public string dataScatto { get; set; } = "";

        /// <summary>
        /// Larghezza dell'immagine in pixel.
        /// </summary>
        public int larghezza { get; set; } = 0;

        /// <summary>
        /// Altezza dell'immagine in pixel.
        /// </summary>
        public int altezza { get; set; } = 0;

        /// <summary>
        /// Marca del dispositivo di acquisizione, se disponibile.
        /// </summary>
        public string marca { get; set; } = "";

        /// <summary>
        /// Modello del dispositivo di acquisizione, se disponibile.
        /// </summary>
        public string modello { get; set; } = "";

        /// <summary>
        /// Tempo di esposizione rilevato dai metadati.
        /// </summary>
        public string esposizione { get; set; } = "";

        /// <summary>
        /// Valore di apertura rilevato dai metadati.
        /// </summary>
        public string apertura { get; set; } = "";

        /// <summary>
        /// Valore ISO rilevato dai metadati.
        /// </summary>
        public string iso { get; set; } = "";

        /// <summary>
        /// Compensazione dell'esposizione rilevata dai metadati.
        /// </summary>
        public string compensazione { get; set; } = "";

        /// <summary>
        /// Zoom o lunghezza focale rilevati dai metadati.
        /// </summary>
        public string zoom { get; set; } = "";

        /// <summary>
        /// Hash SHA256 del contenuto del file.
        /// </summary>
        public string hashSha256 { get; set; } = "";

        /// <summary>
        /// Chiave usata per il confronto dei duplicati binari esatti.
        /// </summary>
        public string chiaveDupBinario { get; set; } = "";

        /// <summary>
        /// Chiave usata per il confronto dei duplicati basati su data di scatto e caratteristiche tecniche.
        /// </summary>
        public string chiaveDupScatto { get; set; } = "";

        /// <summary>
        /// Chiave usata per individuare duplicati probabili quando la data di scatto non è affidabile o manca.
        /// </summary>
        public string chiaveDupProbabile { get; set; } = "";

        /// <summary>
        /// Nome file derivato dalla data di riferimento, utile per eventuali rinominazioni.
        /// </summary>
        public string nuovofile { get; set; } = "";

        /// <summary>
        /// Indica se sono stati trovati metadati utili nel file.
        /// </summary>
        public bool metadatiPresenti { get; set; } = false;

        /// <summary>
        /// Eventuali note di errore o anomalie rilevate durante la lettura del file.
        /// </summary>
        public string noteErrore { get; set; } = "";

        /// <summary>
        /// Data e ora della scansione che ha elaborato il file.
        /// </summary>
        public string dataScansione { get; set; } = "";

        /// <summary>
        /// Indica se il file risulta esistente sul disco al momento della scansione.
        /// </summary>
        public bool fileEsiste { get; set; } = true;

        /// <summary>
        /// Restituisce una rappresentazione testuale sintetica del file.
        /// </summary>
        /// <returns>
        /// Una stringa con percorso completo e dimensione del file.
        /// </returns>
        public override string ToString()
        {
            return $"File: {percorsoCompleto} | {dimensione} bytes";
        }

        /// <summary>
        /// Calcola e valorizza le chiavi derivate usate per rinomina e deduplicazione.
        /// </summary>
        public void CalcolaChiaviDerivate()
        {
            nuovofile = CalcNuovofile();

            chiaveDupBinario = hashSha256;

            string camera = Utility.SafeKeyPart(marca) + "|" + Utility.SafeKeyPart(modello);
            string dimensioni = larghezza.ToString() + "x" + altezza.ToString();

            string dataScattoNormalizzata = Utility.NormalizeKeyDate(dataScatto);
            string dataScattoMinuto = Utility.NormalizeKeyDateMinute(dataScatto);

            if (!string.IsNullOrWhiteSpace(dataScattoNormalizzata) && larghezza > 0 && altezza > 0)
            {
                chiaveDupScatto = $"{dataScattoNormalizzata}|{dimensioni}|{camera}";
            }
            else
            {
                chiaveDupScatto = "";
            }

            string dataRiferimentoProbabile = !string.IsNullOrWhiteSpace(dataScattoMinuto)
                ? dataScattoMinuto
                : Utility.NormalizeKeyDateMinute(dataFileModifica);

            if (!string.IsNullOrWhiteSpace(dataRiferimentoProbabile) && larghezza > 0 && altezza > 0)
            {
                chiaveDupProbabile = $"{dataRiferimentoProbabile}|{dimensioni}|{camera}";
            }
            else
            {
                chiaveDupProbabile = "";
            }
        }

        /// <summary>
        /// Costruisce il nome file derivato usando la data di scatto oppure,
        /// se assente, la data di modifica del file.
        /// </summary>
        /// <returns>
        /// Un nome file nel formato <c>yyyy_MM_dd_HH_mm_ss.estensione</c>.
        /// Se la data non è valida, usa una data fittizia di fallback.
        /// </returns>
        public string CalcNuovofile()
        {
            string dataBase = !string.IsNullOrWhiteSpace(dataScatto) ? dataScatto : dataFileModifica;
            string p1;

            DateTime dateValue;
            if (DateTime.TryParseExact(
                dataBase,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dateValue))
            {
                p1 = dateValue.ToString("yyyy_MM_dd_HH_mm_ss");
            }
            else
            {
                p1 = "0000_01_01_00_00_00";
            }

            return $"{p1}.{this.estensione}";
        }
    }
}