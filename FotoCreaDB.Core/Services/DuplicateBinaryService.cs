using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Contiene la logica per individuare gruppi di duplicati binari
    /// e stabilire quale file conservare e quali candidare all'eliminazione.
    /// </summary>
    public class DuplicateBinaryService
    {
        /// <summary>
        /// Costruisce l'elenco delle decisioni di deduplicazione partendo
        /// da tutti i file candidati disponibili.
        /// </summary>
        /// <param name="allFiles">
        /// Elenco completo dei file da analizzare.
        /// </param>
        /// <returns>
        /// Una lista di decisioni, ognuna composta da un file da tenere
        /// e dagli eventuali file duplicati da eliminare.
        /// </returns>
        public List<DuplicateBinaryDecision> BuildDecisions(List<DuplicateBinaryCandidate> allFiles)
        {
            if (allFiles == null)
            {
                return new List<DuplicateBinaryDecision>();
            }

            List<DuplicateBinaryDecision> decisions = new List<DuplicateBinaryDecision>();

            List<IGrouping<string, DuplicateBinaryCandidate>> groups =
                allFiles
                .Where(x => !string.IsNullOrWhiteSpace(x.HashSha256))
                .GroupBy(x => x.HashSha256, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (IGrouping<string, DuplicateBinaryCandidate> hashGroup in groups)
            {
                List<DuplicateBinaryCandidate> candidates = hashGroup.ToList();

                //List<List<DuplicateBinaryCandidate>> compatibleSubgroups =
                //    new List<List<DuplicateBinaryCandidate>>
                //    {
                //        candidates
                //    };

                List<List<DuplicateBinaryCandidate>> compatibleSubgroups =
                    SplitIntoCompatibleSubgroups(candidates);

                foreach (List<DuplicateBinaryCandidate> subgroup in compatibleSubgroups)
                {
                    if (subgroup.Count <= 1)
                    {
                        continue;
                    }

                    List<DuplicateBinaryCandidate> ordered =
                        subgroup
                        .OrderByDescending(x => (x.PercorsoCompleto ?? "").Length)
                        .ThenByDescending(x => ParseSortableDate(x.DataFileModifica))
                        .ThenBy(x => x.Id)
                        .ToList();

                    DuplicateBinaryDecision decision = new DuplicateBinaryDecision
                    {
                        HashSha256 = hashGroup.Key,
                        Dimensione = ordered[0].Dimensione,
                        DataScattoRiferimento = GetReferenceDataScatto(ordered),
                        FileDaTenere = ordered[0],
                        FileDaEliminare = ordered.Skip(1).ToList()
                    };

                    decisions.Add(decision);
                }
            }

            return decisions
                .OrderBy(x => x.FileDaTenere.PercorsoCompleto, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Suddivide i candidati in sottogruppi compatibili tra loro.
        /// Questo evita di considerare nello stesso gruppo file che,
        /// pur avendo lo stesso hash, non superano le altre verifiche previste.
        /// </summary>
        /// <param name="candidates">
        /// Elenco dei file che condividono lo stesso hash.
        /// </param>
        /// <returns>
        /// Una lista di sottogruppi compatibili.
        /// </returns>
        private List<List<DuplicateBinaryCandidate>> SplitIntoCompatibleSubgroups(List<DuplicateBinaryCandidate> candidates)
        {
            List<List<DuplicateBinaryCandidate>> result = new List<List<DuplicateBinaryCandidate>>();

            foreach (DuplicateBinaryCandidate current in candidates)
            {
                bool inserted = false;

                for (int i = 0; i < result.Count; i++)
                {
                    if (CanFitIntoGroup(current, result[i]))
                    {
                        result[i].Add(current);
                        inserted = true;
                        break;
                    }
                }

                if (!inserted)
                {
                    result.Add(new List<DuplicateBinaryCandidate> { current });
                }
            }

            return result;
        }

        /// <summary>
        /// Verifica se un candidato può essere aggiunto a un gruppo già esistente
        /// senza violare le regole di compatibilità.
        /// </summary>
        /// <param name="candidate">
        /// File da valutare.
        /// </param>
        /// <param name="group">
        /// Gruppo nel quale si tenta di inserire il file.
        /// </param>
        /// <returns>
        /// <c>true</c> se il file è compatibile con tutti gli elementi del gruppo;
        /// altrimenti <c>false</c>.
        /// </returns>
        private bool CanFitIntoGroup(DuplicateBinaryCandidate candidate, List<DuplicateBinaryCandidate> group)
        {
            if (group == null || group.Count == 0)
            {
                return true;
            }

            foreach (DuplicateBinaryCandidate existing in group)
            {
                if (!AreCompatible(candidate, existing))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Confronta due file e stabilisce se possono essere considerati
        /// realmente duplicati all'interno dello stesso gruppo.
        /// </summary>
        /// <param name="a">Primo file da confrontare.</param>
        /// <param name="b">Secondo file da confrontare.</param>
        /// <returns>
        /// <c>true</c> se i due file sono compatibili secondo hash, dimensione
        /// e, quando presente su entrambi, data di scatto; altrimenti <c>false</c>.
        /// </returns>
        private bool AreCompatible(DuplicateBinaryCandidate a, DuplicateBinaryCandidate b)
        {
            if (!string.Equals(a.HashSha256, b.HashSha256, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (a.Dimensione != b.Dimensione)
            {
                return false;
            }

            bool aHasDataScatto = !string.IsNullOrWhiteSpace(a.DataScatto);
            bool bHasDataScatto = !string.IsNullOrWhiteSpace(b.DataScatto);

            if (aHasDataScatto && bHasDataScatto)
            {
                return string.Equals(a.DataScatto, b.DataScatto, StringComparison.Ordinal);
            }

            return true;
        }

        /// <summary>
        /// Restituisce la prima data di scatto valorizzata presente
        /// nell'elenco ordinato dei candidati.
        /// </summary>
        /// <param name="ordered">
        /// Elenco ordinato dei file del gruppo.
        /// </param>
        /// <returns>
        /// La data di scatto di riferimento, oppure <c>null</c> se assente.
        /// </returns>
        private string? GetReferenceDataScatto(List<DuplicateBinaryCandidate> ordered)
        {
            if (ordered == null || ordered.Count == 0)
            {
                return null;
            }

            foreach (DuplicateBinaryCandidate item in ordered)
            {
                if (!string.IsNullOrWhiteSpace(item.DataScatto))
                {
                    return item.DataScatto;
                }
            }

            return null;
        }

        /// <summary>
        /// Converte una data in formato testuale in un valore <see cref="DateTime"/>
        /// adatto all'ordinamento cronologico.
        /// </summary>
        /// <param name="value">
        /// Data da convertire nel formato <c>yyyy-MM-dd HH:mm:ss</c>.
        /// </param>
        /// <returns>
        /// La data convertita se valida; in caso contrario <see cref="DateTime.MinValue"/>.
        /// </returns>
        private DateTime ParseSortableDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DateTime.MinValue;
            }

            DateTime dt;
            if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dt))
            {
                return dt;
            }

            return DateTime.MinValue;
        }
    }
}