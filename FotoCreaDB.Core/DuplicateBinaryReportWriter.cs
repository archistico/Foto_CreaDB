using System;
using System.Collections.Generic;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Scrive a console il riepilogo delle decisioni prese
    /// durante l'analisi dei duplicati binari.
    /// </summary>
    public class DuplicateBinaryReportWriter
    {
        /// <summary>
        /// Stampa a console l'elenco dei gruppi di duplicati trovati,
        /// indicando per ciascuno il file da tenere e quelli da eliminare.
        /// </summary>
        /// <param name="decisions">
        /// Elenco delle decisioni costruite dal servizio di analisi.
        /// </param>
        public void Write(List<DuplicateBinaryDecision> decisions)
        {
            Console.WriteLine();
            Console.WriteLine("========== DUPLICATI BINARI ==========");

            if (decisions == null || decisions.Count == 0)
            {
                Console.WriteLine("Nessun duplicato binario trovato.");
                Console.WriteLine("======================================");
                Console.WriteLine();
                return;
            }

            int totaleGruppi = 0;
            int totaleFileDaEliminare = 0;

            foreach (DuplicateBinaryDecision decision in decisions)
            {
                totaleGruppi++;
                totaleFileDaEliminare += decision.FileDaEliminare.Count;

                Console.WriteLine("--------------------------------------");
                Console.WriteLine("HASH            : " + decision.HashSha256);
                Console.WriteLine("DIMENSIONE      : " + decision.Dimensione);
                Console.WriteLine("DATA SCATTO REF : " + (decision.DataScattoRiferimento ?? ""));

                Console.WriteLine("TIENI:");
                WriteCandidate(decision.FileDaTenere);

                Console.WriteLine("ELIMINA:");
                foreach (DuplicateBinaryCandidate item in decision.FileDaEliminare)
                {
                    WriteCandidate(item);
                }
            }

            Console.WriteLine("--------------------------------------");
            Console.WriteLine("Gruppi duplicati        : " + totaleGruppi);
            Console.WriteLine("File candidati elimina  : " + totaleFileDaEliminare);
            Console.WriteLine("======================================");
            Console.WriteLine();
        }

        /// <summary>
        /// Stampa a console una singola riga descrittiva per un file candidato.
        /// </summary>
        /// <param name="item">
        /// File da visualizzare nel report.
        /// </param>
        private void WriteCandidate(DuplicateBinaryCandidate item)
        {
            Console.WriteLine(
                $"  ID={item.Id} | DIM={item.Dimensione} | DATA_FILE={item.DataFileModifica} | DATA_SCATTO={item.DataScatto} | {item.PercorsoCompleto}");
        }
    }
}