namespace Foto_CreaDB2;

public class Program
{
    // 0: complete/no work; 1: incomplete; 2: invalid input/fatal error; 3: cancelled.
    public static int Main(string[] args)
    {
        try
        {
            CommandLineOptions? options = CommandLineParser.Parse(args);
            if (options == null) { WriteUsage(); return 2; }
            AppConfig config = AppConfigLoader.LoadConfiguration(options.ConfigurationPath,
                requireExists: options.ConfigurationPath != null,
                log: message => Console.WriteLine(message.Message));
            if (options.PathInput != null) config.Paths = new[] { Path.GetFullPath(options.PathInput) };
            if (!string.IsNullOrEmpty(options.NomeDb)) config.NomeDb = Path.GetFullPath(options.NomeDb);
            config.VerboseDuplicates = options.VerboseDuplicates;
            config.CancellaDbSeEsiste = false;
            var logger = new Logger(config.LogDettagliato, config.ProgressEvery);
            var adapter = new ConsoleServiceAdapter(logger);
            int exitCode;
            if (options.Action == AppAction.Analisi)
            {
                AnalysisResult result = new AnalysisService().Run(new AnalysisRequest
                {
                    Configuration = config, Paths = config.Paths, NomeDb = config.NomeDb,
                    LogDettagliato = config.LogDettagliato, ProgressEvery = config.ProgressEvery,
                    VerboseDuplicates = config.VerboseDuplicates
                }, adapter.CreateAnalysisProgress(), adapter.OnLog);
                logger.WriteFinalStatistics(result.Statistics);
                logger.WriteLine(result.Message);
                exitCode = result.Success ? 0 : 1;
            }
            else
            {
                string[] scope = options.ScopePaths.Select(Path.GetFullPath).ToArray();
                DuplicateReportResult report = new DuplicateReportService().Run(new DuplicateReportRequest
                {
                    Configuration = config, NomeDb = config.NomeDb, Paths = scope
                }, adapter.CreateReportProgress(), adapter.OnLog);
                logger.WriteLine("Gruppi duplicati: " + report.DuplicateGroupsCount);
                logger.WriteLine("File candidati alla cancellazione: " + report.FilesToDeleteCount);
                if (config.VerboseDuplicates) new DuplicateBinaryReportWriter().Write(report.Decisions);
                if (!report.Success) return 1;
                exitCode = 0;
                if (options.Action == AppAction.Cancella && report.FilesToDeleteCount > 0)
                {
                    logger.WriteLine("Database: " + config.NomeDb);
                    logger.WriteLine("Ambito: " + (scope.Length == 0 ? "tutte le cartelle del database" : string.Join(", ", scope)));
                    logger.WriteLine("Estensioni: " + string.Join(", ", config.EstensioniPermesse));
                    logger.WriteLine("Cancellazione definitiva; viene conservata una copia verificata per gruppo.");
                    logger.WriteLine("Procedo alla cancellazione? Scrivi S e premi Invio per confermare.");
                    if (!string.Equals(Console.ReadLine()?.Trim(), "S", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.WriteLine("Cancellazione annullata.");
                        return 3;
                    }
                    adapter.Reset();
                    DeletionResult result = new DuplicateDeletionService().Run(new DeletionRequest
                    {
                        Configuration = config, NomeDb = config.NomeDb, Paths = scope
                    }, adapter.CreateDeletionProgress(), adapter.OnLog);
                    logger.WriteLine(result.Message);
                    exitCode = result.Success ? 0 : 1;
                }
            }
            logger.WriteLine("Fine.");
            return exitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Errore: " + ex.Message);
            for (Exception? inner = ex.InnerException; inner != null; inner = inner.InnerException)
                Console.Error.WriteLine(inner.Message);
            return 2;
        }
    }

    private static void WriteUsage()
    {
        Console.WriteLine("Uso: FotoCreaDB.Console analisi <file/cartella> [database] [--config file.json] [--verbose]");
        Console.WriteLine("     FotoCreaDB.Console report <database> [--path file/cartella] [--config file.json] [--verbose]");
        Console.WriteLine("     FotoCreaDB.Console cancella <database> [--path file/cartella] [--config file.json] [--verbose]");
        Console.WriteLine("--path può essere ripetuto. Senza --path, report e cancellazione includono tutte le cartelle del database.");
        Console.WriteLine("Configurazione predefinita: " + AppConfigLoader.GetDefaultConfigPath());
    }
}
