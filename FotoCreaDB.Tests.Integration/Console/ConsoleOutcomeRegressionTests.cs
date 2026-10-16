using Foto_CreaDB2;
using FotoCreaDB.Tests.Integration.TestHelpers;
using static FotoCreaDB.Tests.Integration.Deletion.ConfiguredDeletionScopeTests;

namespace FotoCreaDB.Tests.Integration.Console;

[Collection("Console streams")]
public class ConsoleOutcomeRegressionTests
{
    [Fact]
    public void InvalidConfiguration_ReturnsErrorWithoutCreatingDatabase()
    {
        using TemporaryTestFolder folder = new();
        string config = folder.GetPath("settings.json");
        File.WriteAllText(config, "{broken");
        var result = Run(new[] { "analisi", folder.FolderPath, folder.GetPath("new.db"), "--config", config });
        Assert.Equal(2, result.Code);
        Assert.Contains("JSON", result.Error);
        Assert.False(File.Exists(folder.GetPath("new.db")));
        Assert.Equal("{broken", File.ReadAllText(config));
    }

    [Fact]
    public void AnalysisFailure_ReturnsNonzeroAndPrintsStatisticsBeforeFinish()
    {
        using TemporaryTestFolder folder = new();
        string config = Configuration(folder);
        var result = Run(new[] { "analisi", folder.GetPath("missing"), folder.GetPath("new.db"), "--config", config });
        Assert.Equal(1, result.Code);
        Assert.Contains("Analisi terminata con errori", result.Output);
        Assert.EndsWith("Fine." + Environment.NewLine, result.Output);
    }

    [Theory]
    [InlineData("")]
    [InlineData("N\n")]
    public void RedirectedInputWithoutConfirmation_CancelsAndPreservesFiles(string input)
    {
        using TemporaryTestFolder folder = new();
        string keeper = Photo(folder, "keeper-with-longest-name.jpg");
        string candidate = Photo(folder, "copy.jpg");
        string db = Scan(folder, Allowed("jpg"));
        var result = Run(new[] { "cancella", db, "--config", Configuration(folder) }, new StringReader(input));
        Assert.Equal(3, result.Code);
        Assert.Contains("annullata", result.Output);
        Assert.True(File.Exists(keeper));
        Assert.True(File.Exists(candidate));
    }

    [Fact]
    public void BlockedDeletion_ReturnsIncompleteCode()
    {
        using TemporaryTestFolder folder = new();
        Photo(folder, "keeper-with-longest-name.jpg");
        string candidate = Photo(folder, "copy.jpg");
        string db = Scan(folder, Allowed("jpg"));
        File.WriteAllText(candidate, "modified after scanning");
        var result = Run(new[] { "cancella", db, "--config", Configuration(folder) }, new StringReader("S\n"));
        Assert.Equal(1, result.Code);
        Assert.Contains("File bloccati: 1", result.Output);
        Assert.DoesNotContain("Cancellazione completata.", result.Output);
        Assert.True(File.Exists(candidate));
    }

    [Fact]
    public void AnalysisUsesConfiguredDatabaseAndSelection()
    {
        using TemporaryTestFolder folder = new();
        Photo(folder, "photo.jpg");
        Photo(folder, "document.pdf");
        string config = Configuration(folder);
        var result = Run(new[] { "analisi", folder.FolderPath, "--config", config });
        Assert.Equal(0, result.Code);
        string db = folder.GetPath("configured.db");
        using var manager = new DatabaseManager(db, false);
        manager.Initialize();
        using var repository = new FotoRepository(manager.Connection!);
        Assert.NotNull(repository.GetByPercorso(folder.GetPath("photo.jpg")));
        Assert.Null(repository.GetByPercorso(folder.GetPath("document.pdf")));
    }

    private static string Configuration(TemporaryTestFolder folder)
    {
        string path = folder.GetPath("settings.json");
        Assert.True(AppConfigLoader.SaveToFile(new AppConfig
        {
            NomeDb = "configured.db", EstensioniPermesse = new() { "jpg" }
        }, path));
        return path;
    }

    private static (int Code, string Output, string Error) Run(string[] args, TextReader? input = null)
    {
        var previousOut = System.Console.Out;
        var previousError = System.Console.Error;
        var previousIn = System.Console.In;
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var empty = new StringReader("");
        try
        {
            System.Console.SetOut(output);
            System.Console.SetError(error);
            System.Console.SetIn(input ?? empty);
            int code = Program.Main(args);
            return (code, output.ToString(), error.ToString());
        }
        finally
        {
            System.Console.SetOut(previousOut);
            System.Console.SetError(previousError);
            System.Console.SetIn(previousIn);
            input?.Dispose();
        }
    }
}

[CollectionDefinition("Console streams", DisableParallelization = true)]
public class ConsoleStreamsCollection;
