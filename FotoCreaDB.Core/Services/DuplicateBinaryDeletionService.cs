using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Foto_CreaDB2
{
    /// <summary>Deletes verified binary copies while keeping the retained copy locked.</summary>
    public class DuplicateBinaryDeletionService
    {
        public int DeleteFiles(
            List<DuplicateBinaryDecision> decisions,
            string nomeDb,
            Logger? logger,
            out int deletedDbRecordsCount,
            out int orphanDbRecordsDeletedCount,
            IProgress<DeletionProgress>? progress = null,
            Action<ServiceLogMessage>? log = null,
            DeletionStatistics? statistics = null)
        {
            statistics ??= new DeletionStatistics();
            statistics.FilesBlockedCount = 0;
            statistics.FilesMissingCount = 0;
            statistics.DatabaseErrorsCount = 0;
            deletedDbRecordsCount = 0;
            orphanDbRecordsDeletedCount = 0;
            // Callers/callbacks must not redirect a decision during deletion.
            var plan = (decisions ?? new()).Where(d => d != null).Select(d => new DuplicateBinaryDecision
            {
                HashSha256 = d.HashSha256,
                Dimensione = d.Dimensione,
                FileDaTenere = Copy(d.FileDaTenere),
                FileDaEliminare = (d.FileDaEliminare ?? new()).Select(Copy).ToList()
            }).ToList();
            int total = plan.Sum(d => d.FileDaEliminare.Count);
            int processed = 0;
            int deleted = 0;
            void Report(string path) => ServiceCallbackHelper.ReportProgress(progress, new DeletionProgress
            {
                ProcessedFiles = processed, TotalFiles = total, CurrentFile = path
            });
            Report(string.Empty);
            if (total == 0) return 0;

            using var db = new DatabaseManager(nomeDb, false);
            db.Initialize();
            using var repository = new FotoRepository(db.Connection!);
            foreach (var decision in plan)
            {
                FileStream keeper;
                string keepPath = decision.FileDaTenere.PercorsoCompleto;
                try
                {
                    if (!IsValidHash(decision.HashSha256) ||
                        !string.Equals(decision.HashSha256, decision.FileDaTenere.HashSha256, StringComparison.OrdinalIgnoreCase) ||
                        decision.Dimensione != decision.FileDaTenere.Dimensione)
                        throw new IOException("Dati del gruppo incompleti o incoerenti.");
                    keeper = OpenVerified(decision.FileDaTenere, false);
                }
                catch (Exception ex)
                {
                    logger?.WriteError("FILE DA TENERE NON VALIDATO, salto gruppo: " + keepPath, ex);
                    ServiceCallbackHelper.Warning(log, "FILE DA TENERE NON VALIDATO, salto gruppo: " + keepPath + " - " + ex.Message);
                    foreach (var skipped in decision.FileDaEliminare)
                    {
                        statistics.FilesBlockedCount++;
                        processed++;
                        Report(skipped.PercorsoCompleto);
                    }
                    continue;
                }

                // Keep this lock between candidates, including during callbacks.
                using (keeper)
                {
                    foreach (var item in decision.FileDaEliminare)
                    {
                        try
                        {
                            if (!IsValidHash(item.HashSha256) ||
                                !string.Equals(item.HashSha256, decision.HashSha256, StringComparison.OrdinalIgnoreCase) ||
                                item.Dimensione != decision.Dimensione ||
                                string.Equals(Path.GetFullPath(item.PercorsoCompleto), Path.GetFullPath(keepPath), PathIdentity.Comparison))
                                throw new IOException("Il candidato non è una copia distinta e coerente con il gruppo.");

                            // Exclusive access also rejects hard links/aliases of the keeper.
                            using (FileStream candidate = OpenVerified(item, true))
                            {
                                if (!HaveSameBytes(keeper, candidate))
                                    throw new IOException("Contenuti diversi: cancellazione bloccata.");
                                LockedFile.Delete(candidate);
                            }
                            deleted++;
                            deletedDbRecordsCount += RemoveRecord(repository, item, logger, log, statistics);
                            logger?.WriteLine("ELIMINATO: " + item.PercorsoCompleto);
                            ServiceCallbackHelper.Info(log, "ELIMINATO: " + item.PercorsoCompleto);
                        }
                        catch (FileNotFoundException)
                        {
                            statistics.FilesMissingCount++;
                            int removed = RemoveRecord(repository, item, logger, log, statistics);
                            deletedDbRecordsCount += removed;
                            orphanDbRecordsDeletedCount += removed;
                            ServiceCallbackHelper.Warning(log, "NON TROVATO SU DISCO: " + item.PercorsoCompleto);
                        }
                        catch (Exception ex)
                        {
                            statistics.FilesBlockedCount++;
                            logger?.WriteError("Cancellazione bloccata: " + item.PercorsoCompleto, ex);
                            ServiceCallbackHelper.Warning(log, "Cancellazione bloccata: " + item.PercorsoCompleto + " - " + ex.Message);
                        }
                        finally
                        {
                            processed++;
                            Report(item.PercorsoCompleto);
                        }
                    }
                }
            }
            ServiceCallbackHelper.Info(log, "File eliminati con successo: " + deleted);
            ServiceCallbackHelper.Info(log, "Record DB eliminati con successo: " + deletedDbRecordsCount);
            ServiceCallbackHelper.Info(log, "Record orfani bonificati dal DB: " + orphanDbRecordsDeletedCount);
            return deleted;
        }

        private static DuplicateBinaryCandidate Copy(DuplicateBinaryCandidate? item) => new()
        {
            Id = item?.Id ?? 0, PercorsoCompleto = item?.PercorsoCompleto ?? "",
            HashSha256 = item?.HashSha256 ?? "", Dimensione = item?.Dimensione ?? -1
        };

        private static int RemoveRecord(FotoRepository repository, DuplicateBinaryCandidate item,
            Logger? logger, Action<ServiceLogMessage>? log, DeletionStatistics statistics)
        {
            try
            {
                int removed = repository.DeleteByIdAndPath(item.Id, item.PercorsoCompleto);
                if (removed != 1) throw new IOException("Il record del file non corrisponde più al piano di cancellazione.");
                return removed;
            }
            catch (Exception ex)
            {
                statistics.DatabaseErrorsCount++;
                logger?.WriteError("Errore durante la cancellazione del record DB ID=" + item.Id, ex);
                ServiceCallbackHelper.Error(log, "Errore durante la cancellazione del record DB ID=" + item.Id, ex);
                return 0;
            }
        }

        private static bool IsValidHash(string hash) =>
            hash != null && hash.Length == 64 && hash.All(Uri.IsHexDigit);

        private static FileStream OpenVerified(DuplicateBinaryCandidate item, bool forDeletion)
        {
            if (!IsValidHash(item.HashSha256) || item.Dimensione < 0)
                throw new IOException("Hash o dimensione mancanti/non validi.");
            FileStream file = LockedFile.Open(item.PercorsoCompleto, forDeletion);
            try
            {
                if (file.Length != item.Dimensione ||
                    !string.Equals(Convert.ToHexString(SHA256.HashData(file)), item.HashSha256, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("File modificato dopo l'analisi.");
                return file;
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }

        private static bool HaveSameBytes(FileStream keeper, FileStream candidate)
        {
            if (keeper.Length != candidate.Length) return false;
            keeper.Position = candidate.Position = 0;
            byte[] left = new byte[81920];
            byte[] right = new byte[left.Length];
            int count;
            while ((count = keeper.Read(left)) > 0)
            {
                candidate.ReadExactly(right.AsSpan(0, count));
                if (!left.AsSpan(0, count).SequenceEqual(right.AsSpan(0, count))) return false;
            }
            return candidate.ReadByte() == -1;
        }
    }
}
