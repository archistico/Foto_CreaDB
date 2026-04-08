using System;
using System.Collections.Generic;
using System.IO;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Gestisce la cancellazione fisica dei file candidati all'eliminazione
    /// dopo l'analisi dei duplicati binari.
    /// </summary>
    public class DuplicateBinaryDeletionService
    {
        /// <summary>
        /// Elimina dal disco tutti i file indicati nelle decisioni ricevute.
        /// </summary>
        /// <param name="decisions">
        /// Elenco delle decisioni contenenti i file da eliminare.
        /// </param>
        /// <param name="logger">
        /// Logger usato per scrivere errori e riepilogo operazioni.
        /// </param>
        /// <returns>
        /// Numero totale di file eliminati con successo.
        /// </returns>
        public int DeleteFiles(List<DuplicateBinaryDecision> decisions, Logger logger)
        {
            if (decisions == null || decisions.Count == 0)
            {
                return 0;
            }

            int deletedCount = 0;

            foreach (DuplicateBinaryDecision decision in decisions)
            {
                if (decision.FileDaEliminare == null || decision.FileDaEliminare.Count == 0)
                {
                    continue;
                }

                foreach (DuplicateBinaryCandidate item in decision.FileDaEliminare)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.PercorsoCompleto))
                    {
                        continue;
                    }

                    try
                    {
                        if (File.Exists(item.PercorsoCompleto))
                        {
                            File.Delete(item.PercorsoCompleto);
                            deletedCount++;
                            logger.WriteLine("ELIMINATO: " + item.PercorsoCompleto);
                        }
                        else
                        {
                            logger.WriteLine("NON TROVATO: " + item.PercorsoCompleto);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.WriteError($"Errore durante la cancellazione del file '{item.PercorsoCompleto}'", ex);
                    }
                }
            }

            logger.WriteLine("");
            logger.WriteLine("File eliminati con successo: " + deletedCount);

            return deletedCount;
        }
    }
}