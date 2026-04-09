using System;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Gestisce il parsing degli argomenti da riga di comando
    /// e li converte in un oggetto CommandLineOptions.
    /// </summary>
    public static class CommandLineParser
    {
        /// <summary>
        /// Analizza gli argomenti passati all'applicazione.
        /// </summary>
        /// <param name="args">Argomenti da riga di comando.</param>
        /// <returns>Opzioni interpretate.</returns>
        public static CommandLineOptions Parse(string[] args)
        {
            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                return null;
            }

            string actionText = args[0].Trim();

            if (string.Equals(actionText, "analisi", StringComparison.OrdinalIgnoreCase))
            {
                return ParseAnalisi(args);
            }

            if (string.Equals(actionText, "report", StringComparison.OrdinalIgnoreCase))
            {
                return ParseReport(args);
            }

            if (string.Equals(actionText, "cancella", StringComparison.OrdinalIgnoreCase))
            {
                return ParseCancella(args);
            }

            throw new ArgumentException("Azione non riconosciuta. Usa: analisi, report, cancella.");
        }

        /// <summary>
        /// Interpreta gli argomenti della modalità analisi.
        /// </summary>
        /// <param name="args">Argomenti completi.</param>
        /// <returns>Opzioni configurate per analisi.</returns>
        private static CommandLineOptions ParseAnalisi(string[] args)
        {
            if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                throw new ArgumentException("Per l'azione 'analisi' devi specificare il percorso file o cartella da analizzare.");
            }

            CommandLineOptions options = new CommandLineOptions
            {
                Action = AppAction.Analisi,
                PathInput = args[1].Trim(),
                NomeDb = "foto.db",
                VerboseDuplicates = false
            };

            for (int i = 2; i < args.Length; i++)
            {
                string currentArg = (args[i] ?? "").Trim();

                if (string.IsNullOrWhiteSpace(currentArg))
                {
                    continue;
                }

                if (string.Equals(currentArg, "--verbose", StringComparison.OrdinalIgnoreCase))
                {
                    options.VerboseDuplicates = true;
                    continue;
                }

                if (string.Equals(options.NomeDb, "foto.db", StringComparison.OrdinalIgnoreCase))
                {
                    options.NomeDb = currentArg;
                }
            }

            return options;
        }

        /// <summary>
        /// Interpreta gli argomenti della modalità report.
        /// </summary>
        /// <param name="args">Argomenti completi.</param>
        /// <returns>Opzioni configurate per report.</returns>
        private static CommandLineOptions ParseReport(string[] args)
        {
            if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                throw new ArgumentException("Per l'azione 'report' devi specificare il percorso del database SQLite.");
            }

            CommandLineOptions options = new CommandLineOptions
            {
                Action = AppAction.Report,
                NomeDb = args[1].Trim(),
                VerboseDuplicates = false
            };

            for (int i = 2; i < args.Length; i++)
            {
                string currentArg = (args[i] ?? "").Trim();

                if (string.Equals(currentArg, "--verbose", StringComparison.OrdinalIgnoreCase))
                {
                    options.VerboseDuplicates = true;
                }
            }

            return options;
        }

        /// <summary>
        /// Interpreta gli argomenti della modalità cancella.
        /// </summary>
        /// <param name="args">Argomenti completi.</param>
        /// <returns>Opzioni configurate per cancellazione.</returns>
        private static CommandLineOptions ParseCancella(string[] args)
        {
            if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                throw new ArgumentException("Per l'azione 'cancella' devi specificare il percorso del database SQLite.");
            }

            CommandLineOptions options = new CommandLineOptions
            {
                Action = AppAction.Cancella,
                NomeDb = args[1].Trim(),
                VerboseDuplicates = false
            };

            for (int i = 2; i < args.Length; i++)
            {
                string currentArg = (args[i] ?? "").Trim();

                if (string.Equals(currentArg, "--verbose", StringComparison.OrdinalIgnoreCase))
                {
                    options.VerboseDuplicates = true;
                }
            }

            return options;
        }
    }
}