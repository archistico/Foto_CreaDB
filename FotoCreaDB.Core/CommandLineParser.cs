namespace Foto_CreaDB2;

public static class CommandLineParser
{
    public static CommandLineOptions? Parse(string[]? args)
    {
        if (args == null || args.Length == 0) return null;
        AppAction action = args[0].ToLowerInvariant() switch
        {
            "analisi" => AppAction.Analisi,
            "report" => AppAction.Report,
            "cancella" => AppAction.Cancella,
            _ => throw new ArgumentException("Azione non riconosciuta. Usa: analisi, report, cancella.")
        };
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]) || args[1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException("Specificare il percorso da analizzare o il database.");
        var options = new CommandLineOptions { Action = action };
        if (action == AppAction.Analisi) options.PathInput = args[1];
        else options.NomeDb = args[1];
        var scope = new List<string>();
        for (int i = 2; i < args.Length; i++)
        {
            string argument = args[i];
            switch (argument.ToLowerInvariant())
            {
                case "--verbose": options.VerboseDuplicates = true; break;
                case "--config":
                    if (options.ConfigurationPath != null) throw new ArgumentException("--config specificato più volte.");
                    options.ConfigurationPath = Value(args, ref i);
                    break;
                case "--path" when action != AppAction.Analisi:
                    scope.Add(Value(args, ref i));
                    break;
                default:
                    if (argument.StartsWith("-", StringComparison.Ordinal))
                        throw new ArgumentException("Opzione sconosciuta: " + argument);
                    if (action != AppAction.Analisi || !string.IsNullOrEmpty(options.NomeDb) || string.IsNullOrWhiteSpace(argument))
                        throw new ArgumentException("Argomento inatteso: " + argument);
                    options.NomeDb = argument;
                    break;
            }
        }
        options.ScopePaths = scope.ToArray();
        return options;
    }

    private static string Value(string[] args, ref int index)
    {
        string option = args[index];
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]) || args[index].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException("Valore mancante per " + option);
        return args[index];
    }
}
