using System;
using System.Globalization;
using System.IO;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Contiene metodi di utilità generici usati nel progetto
    /// per normalizzazione, conversione e manipolazione di stringhe e date.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Restituisce una stringa vuota se il valore è nullo;
        /// altrimenti restituisce il valore originale.
        /// </summary>
        /// <param name="value">
        /// Valore da controllare.
        /// </param>
        /// <returns>
        /// Il valore originale oppure stringa vuota se nullo.
        /// </returns>
        public static string Nz(string? value)
        {
            return value ?? "";
        }

        /// <summary>
        /// Tronca una stringa alla lunghezza massima indicata.
        /// </summary>
        /// <param name="value">
        /// Stringa da troncare.
        /// </param>
        /// <param name="maxLength">
        /// Lunghezza massima consentita.
        /// </param>
        /// <returns>
        /// La stringa originale se già entro il limite, altrimenti la sua parte iniziale.
        /// Se il valore è nullo o vuoto, restituisce stringa vuota.
        /// </returns>
        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        /// <summary>
        /// Unisce una nota esistente con una nuova nota, separandole con un delimitatore,
        /// e applica poi il limite massimo di lunghezza.
        /// </summary>
        /// <param name="currentValue">
        /// Valore attuale della nota.
        /// </param>
        /// <param name="newValue">
        /// Nuova nota da aggiungere.
        /// </param>
        /// <param name="maxLength">
        /// Lunghezza massima del risultato finale.
        /// </param>
        /// <returns>
        /// La nota risultante, eventualmente troncata.
        /// </returns>
        public static string AppendNote(string? currentValue, string? newValue, int maxLength)
        {
            string merged;

            if (string.IsNullOrWhiteSpace(currentValue))
            {
                merged = newValue ?? "";
            }
            else if (string.IsNullOrWhiteSpace(newValue))
            {
                merged = currentValue;
            }
            else
            {
                merged = currentValue + " | " + newValue;
            }

            return Truncate(merged, maxLength);
        }

        /// <summary>
        /// Converte una stringa in intero, rimuovendo opzionalmente prefisso e postfisso.
        /// Se la conversione fallisce, restituisce il valore di default.
        /// </summary>
        /// <param name="s">
        /// Stringa da convertire.
        /// </param>
        /// <param name="prefisso">
        /// Prefisso da rimuovere prima della conversione.
        /// </param>
        /// <param name="postfisso">
        /// Postfisso da rimuovere prima della conversione.
        /// </param>
        /// <param name="def">
        /// Valore di fallback se la conversione non riesce.
        /// </param>
        /// <returns>
        /// Intero convertito oppure valore di default.
        /// </returns>
        public static int ConvertiString2Int(string? s, string? prefisso = "", string? postfisso = "", int def = 0)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return def;
            }

            string s1 = TogliPrefisso(s, prefisso);
            string s2 = TogliPostfisso(s1, postfisso);
            string s3 = s2.Trim();

            int ris;
            if (int.TryParse(s3, out ris))
            {
                return ris;
            }

            return def;
        }

        /// <summary>
        /// Converte una stringa in valore <see cref="long"/>, rimuovendo opzionalmente prefisso e postfisso.
        /// Se la conversione fallisce, restituisce il valore di default.
        /// </summary>
        /// <param name="s">
        /// Stringa da convertire.
        /// </param>
        /// <param name="prefisso">
        /// Prefisso da rimuovere prima della conversione.
        /// </param>
        /// <param name="postfisso">
        /// Postfisso da rimuovere prima della conversione.
        /// </param>
        /// <param name="def">
        /// Valore di fallback se la conversione non riesce.
        /// </param>
        /// <returns>
        /// Valore convertito oppure valore di default.
        /// </returns>
        public static long ConvertiString2Long(string? s, string? prefisso = "", string? postfisso = "", long def = 0)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return def;
            }

            string s1 = TogliPrefisso(s, prefisso);
            string s2 = TogliPostfisso(s1, postfisso);
            string s3 = s2.Trim();

            long ris;
            if (long.TryParse(s3, out ris))
            {
                return ris;
            }

            return def;
        }

        /// <summary>
        /// Rimuove un prefisso da una stringa se presente.
        /// </summary>
        /// <param name="s">
        /// Stringa di input.
        /// </param>
        /// <param name="prefisso">
        /// Prefisso da rimuovere.
        /// </param>
        /// <returns>
        /// Stringa senza il prefisso, se trovato; altrimenti la stringa originale.
        /// </returns>
        public static string TogliPrefisso(string s, string? prefisso)
        {
            if (string.IsNullOrEmpty(prefisso))
            {
                return s;
            }

            if (s.StartsWith(prefisso, StringComparison.OrdinalIgnoreCase))
            {
                return s.Substring(prefisso.Length);
            }

            return s;
        }

        /// <summary>
        /// Rimuove un postfisso da una stringa se presente.
        /// </summary>
        /// <param name="s">
        /// Stringa di input.
        /// </param>
        /// <param name="postfisso">
        /// Postfisso da rimuovere.
        /// </param>
        /// <returns>
        /// Stringa senza il postfisso, se trovato; altrimenti la stringa originale.
        /// </returns>
        public static string TogliPostfisso(string s, string? postfisso)
        {
            if (string.IsNullOrEmpty(postfisso))
            {
                return s;
            }

            if (s.EndsWith(postfisso, StringComparison.OrdinalIgnoreCase))
            {
                return s.Substring(0, s.Length - postfisso.Length);
            }

            return s;
        }

        /// <summary>
        /// Estrae l'estensione di un file senza il punto iniziale
        /// e la restituisce in minuscolo.
        /// </summary>
        /// <param name="path">
        /// Percorso del file.
        /// </param>
        /// <returns>
        /// Estensione del file senza punto, oppure stringa vuota se assente.
        /// </returns>
        public static string GetEstensione(string path)
        {
            string ext = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(ext))
            {
                return "";
            }

            if (ext.StartsWith("."))
            {
                ext = ext.Substring(1);
            }

            return ext.ToLowerInvariant();
        }

        /// <summary>
        /// Normalizza una data proveniente dai metadati in formato
        /// <c>yyyy-MM-dd HH:mm:ss</c>.
        /// Se la conversione non riesce, restituisce il valore di fallback.
        /// </summary>
        /// <param name="rawDate">
        /// Data grezza da normalizzare.
        /// </param>
        /// <param name="fallbackDate">
        /// Valore da restituire se la data non è interpretabile.
        /// </param>
        /// <returns>
        /// Data normalizzata oppure valore di fallback.
        /// </returns>
        public static string NormalizeMetadataDate(string? rawDate, string fallbackDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate))
            {
                return fallbackDate;
            }

            string[] formats = new string[]
            {
                "yyyy:MM:dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy:MM:dd HH:mm:sszzz",
                "yyyy-MM-dd HH:mm:sszzz"
            };

            DateTime dateValue;
            if (DateTime.TryParseExact(
                rawDate,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out dateValue))
            {
                return dateValue.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateValue))
            {
                return dateValue.ToString("yyyy-MM-dd HH:mm:ss");
            }

            return fallbackDate;
        }

        /// <summary>
        /// Normalizza il nome file senza estensione per facilitare confronti tra nomi simili.
        /// Rimuove differenze comuni come maiuscole, underscore, trattini e suffissi di copia.
        /// </summary>
        /// <param name="fileName">
        /// Nome file da normalizzare.
        /// </param>
        /// <returns>
        /// Nome file base normalizzato.
        /// </returns>
        public static string NormalizeFileNameWithoutExtension(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "";
            }

            string baseName = Path.GetFileNameWithoutExtension(fileName).Trim().ToLowerInvariant();

            baseName = baseName.Replace("_", " ");
            baseName = baseName.Replace("-", " ");

            while (baseName.Contains("  "))
            {
                baseName = baseName.Replace("  ", " ");
            }

            string[] suffixes = new string[]
            {
                " copia",
                " copy",
                " copie"
            };

            foreach (string suffix in suffixes)
            {
                if (baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    baseName = baseName.Substring(0, baseName.Length - suffix.Length).Trim();
                }
            }

            if (baseName.EndsWith(")"))
            {
                int idx = baseName.LastIndexOf('(');
                if (idx > 0)
                {
                    string inside = baseName.Substring(idx + 1, baseName.Length - idx - 2);
                    int n;
                    if (int.TryParse(inside, out n))
                    {
                        baseName = baseName.Substring(0, idx).Trim();
                    }
                }
            }

            return baseName;
        }

        /// <summary>
        /// Normalizza una data nel formato chiave completo
        /// <c>yyyy-MM-dd HH:mm:ss</c>.
        /// </summary>
        /// <param name="value">
        /// Data da normalizzare.
        /// </param>
        /// <returns>
        /// Data normalizzata oppure stringa vuota se non valida.
        /// </returns>
        public static string NormalizeKeyDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            DateTime dt;
            if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dt))
            {
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            return "";
        }

        /// <summary>
        /// Normalizza una data nel formato ridotto al minuto
        /// <c>yyyy-MM-dd HH:mm</c>.
        /// </summary>
        /// <param name="value">
        /// Data da normalizzare.
        /// </param>
        /// <returns>
        /// Data normalizzata al minuto oppure stringa vuota se non valida.
        /// </returns>
        public static string NormalizeKeyDateMinute(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            DateTime dt;
            if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dt))
            {
                return dt.ToString("yyyy-MM-dd HH:mm");
            }

            return "";
        }

        /// <summary>
        /// Normalizza una parte di chiave rendendola sicura per confronti:
        /// rimuove spazi esterni e converte in minuscolo.
        /// </summary>
        /// <param name="value">
        /// Valore da normalizzare.
        /// </param>
        /// <returns>
        /// Valore normalizzato oppure stringa vuota se nullo o vuoto.
        /// </returns>
        public static string SafeKeyPart(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            return value.Trim().ToLowerInvariant();
        }
    }
}