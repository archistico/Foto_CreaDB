using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Gestisce il calcolo dell'hash SHA256 dei file e l'aggiornamento
    /// dell'oggetto <see cref="Foto"/> con il valore ottenuto.
    /// </summary>
    public class HashService
    {
        /// <summary>
        /// Prova a calcolare l'hash SHA256 del file indicato e lo assegna
        /// alla proprietà <c>hashSha256</c> dell'oggetto foto.
        /// </summary>
        /// <param name="imagePath">
        /// Percorso completo del file di cui calcolare l'hash.
        /// </param>
        /// <param name="foto">
        /// Oggetto foto da aggiornare con l'hash calcolato o con le note di errore.
        /// </param>
        /// <param name="logger">
        /// Logger usato per registrare eventuali errori durante il calcolo.
        /// </param>
        /// <param name="stats">
        /// Statistiche della scansione da aggiornare in caso di errore.
        /// </param>
        /// <returns>
        /// <c>true</c> se l'hash è stato calcolato correttamente; altrimenti <c>false</c>.
        /// </returns>
        public bool TryCalculateHash(string imagePath, Foto foto, Logger logger, ScanStatistics stats)
        {
            try
            {
                foto.hashSha256 = BytesToString(GetHashSha256(imagePath));
                return true;
            }
            catch (Exception ex)
            {
                stats.TotaleErroriHash++;
                foto.hashSha256 = "";
                foto.noteErrore = Utility.AppendNote(foto.noteErrore, "Errore hash: " + ex.Message, 1000);
                logger.WriteError($"Errore durante il calcolo hash del file '{imagePath}'", ex);
                return false;
            }
        }

        /// <summary>
        /// Calcola l'hash SHA256 del contenuto del file specificato.
        /// </summary>
        /// <param name="filename">
        /// Percorso completo del file da leggere.
        /// </param>
        /// <returns>
        /// Array di byte contenente l'hash SHA256 calcolato.
        /// </returns>
        private byte[] GetHashSha256(string filename)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(filename))
            {
                return sha256.ComputeHash(stream);
            }
        }

        /// <summary>
        /// Converte un array di byte nella rappresentazione esadecimale minuscola.
        /// </summary>
        /// <param name="bytes">
        /// Byte da convertire in stringa.
        /// </param>
        /// <returns>
        /// Stringa esadecimale dell'array ricevuto, oppure stringa vuota se l'input è nullo o vuoto.
        /// </returns>
        private string BytesToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }
}