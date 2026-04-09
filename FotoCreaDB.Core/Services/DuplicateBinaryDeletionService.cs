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
        /// Elimina dal disco tutti i file indicati nelle decisioni ricevute e,
        /// se la cancellazione fisica riesce, rimuove anche il relativo record dal database.
        /// Inoltre bonifica i record orfani quando il file non è già più presente sul disco.
        /// </summary>
        /// <param name="decisions">
        /// Elenco delle decisioni contenenti i file da eliminare.
        /// </param>
        /// <param name="nomeDb">
        /// Percorso del database SQLite da aggiornare.
        /// </param>
        /// <param name="logger">
        /// Logger usato per scrivere errori e riepilogo operazioni.
        /// Può essere null se il chiamante gestisce il logging in altro modo.
        /// </param>
        /// <param name="deletedDbRecordsCount">
        /// Restituisce il numero totale di record eliminati dal database.
        /// </param>
        /// <param name="orphanDbRecordsDeletedCount">
        /// Restituisce il numero di record orfani bonificati dal database.
        /// </param>
        /// <param name="progress">
        /// Callback di avanzamento della cancellazione.
        /// </param>
        /// <param name="log">
        /// Callback di log strutturato indipendente dalla console.
        /// </param>
        /// <returns>
        /// Numero totale di file eliminati con successo dal disco.
        /// </returns>
        public int DeleteFiles(
            List<DuplicateBinaryDecision> decisions,
            string nomeDb,
            Logger logger,
            out int deletedDbRecordsCount,
            out int orphanDbRecordsDeletedCount,
            IProgress<DeletionProgress> progress = null,
            Action<ServiceLogMessage> log = null)
        {
            deletedDbRecordsCount = 0;
            orphanDbRecordsDeletedCount = 0;

            if (decisions == null || decisions.Count == 0)
            {
                ServiceCallbackHelper.ReportProgress(
                    progress,
                    new DeletionProgress
                    {
                        ProcessedFiles = 0,
                        TotalFiles = 0,
                        CurrentFile = null
                    });

                return 0;
            }

            int totalFiles = CountFilesToDelete(decisions);
            int processedFiles = 0;
            int deletedCount = 0;

            ServiceCallbackHelper.Info(log, "Avvio cancellazione fisica dei file duplicati.");

            using DatabaseManager dbManager = new DatabaseManager(nomeDb, false);
            dbManager.Initialize();

            using FotoRepository repository = new FotoRepository(dbManager.Connection!);

            ServiceCallbackHelper.ReportProgress(
                progress,
                new DeletionProgress
                {
                    ProcessedFiles = 0,
                    TotalFiles = totalFiles,
                    CurrentFile = null
                });

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

                            logger?.WriteLine("ELIMINATO: " + item.PercorsoCompleto);
                            ServiceCallbackHelper.Info(log, "ELIMINATO: " + item.PercorsoCompleto);

                            try
                            {
                                repository.DeleteById(item.Id);
                                deletedDbRecordsCount++;

                                logger?.WriteLine("RECORD DB ELIMINATO: ID=" + item.Id);
                                ServiceCallbackHelper.Info(log, "RECORD DB ELIMINATO: ID=" + item.Id);
                            }
                            catch (Exception exDb)
                            {
                                logger?.WriteError($"Errore durante la cancellazione del record DB ID={item.Id}", exDb);
                                ServiceCallbackHelper.Error(log, $"Errore durante la cancellazione del record DB ID={item.Id}", exDb);
                            }
                        }
                        else
                        {
                            logger?.WriteLine("NON TROVATO SU DISCO: " + item.PercorsoCompleto);
                            ServiceCallbackHelper.Warning(log, "NON TROVATO SU DISCO: " + item.PercorsoCompleto);

                            try
                            {
                                repository.DeleteById(item.Id);
                                deletedDbRecordsCount++;
                                orphanDbRecordsDeletedCount++;

                                logger?.WriteLine("RECORD ORFANO BONIFICATO DAL DB: ID=" + item.Id);
                                ServiceCallbackHelper.Info(log, "RECORD ORFANO BONIFICATO DAL DB: ID=" + item.Id);
                            }
                            catch (Exception exDb)
                            {
                                logger?.WriteError($"Errore durante la bonifica del record orfano ID={item.Id}", exDb);
                                ServiceCallbackHelper.Error(log, $"Errore durante la bonifica del record orfano ID={item.Id}", exDb);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.WriteError($"Errore durante la cancellazione del file '{item.PercorsoCompleto}'", ex);
                        ServiceCallbackHelper.Error(log, $"Errore durante la cancellazione del file '{item.PercorsoCompleto}'", ex);
                    }
                    finally
                    {
                        processedFiles++;

                        ServiceCallbackHelper.ReportProgress(
                            progress,
                            new DeletionProgress
                            {
                                ProcessedFiles = processedFiles,
                                TotalFiles = totalFiles,
                                CurrentFile = item.PercorsoCompleto
                            });
                    }
                }
            }

            logger?.WriteLine("");
            logger?.WriteLine("File eliminati con successo        : " + deletedCount);
            logger?.WriteLine("Record DB eliminati con successo   : " + deletedDbRecordsCount);
            logger?.WriteLine("Record orfani bonificati dal DB    : " + orphanDbRecordsDeletedCount);

            ServiceCallbackHelper.Info(log, "File eliminati con successo: " + deletedCount);
            ServiceCallbackHelper.Info(log, "Record DB eliminati con successo: " + deletedDbRecordsCount);
            ServiceCallbackHelper.Info(log, "Record orfani bonificati dal DB: " + orphanDbRecordsDeletedCount);

            return deletedCount;
        }

        /// <summary>
        /// Conta il numero totale di file candidati alla cancellazione
        /// presenti nell'elenco delle decisioni.
        /// </summary>
        /// <param name="decisions">
        /// Elenco delle decisioni di deduplicazione.
        /// </param>
        /// <returns>
        /// Numero totale dei file da eliminare.
        /// </returns>
        private int CountFilesToDelete(List<DuplicateBinaryDecision> decisions)
        {
            if (decisions == null || decisions.Count == 0)
            {
                return 0;
            }

            int total = 0;

            foreach (DuplicateBinaryDecision decision in decisions)
            {
                if (decision.FileDaEliminare != null)
                {
                    total += decision.FileDaEliminare.Count;
                }
            }

            return total;
        }
    }
}