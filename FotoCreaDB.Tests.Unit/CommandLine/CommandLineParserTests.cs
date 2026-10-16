using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.CommandLine
{
    public class CommandLineParserTests
    {
        [Theory]
        [InlineData("analisi", "photos", "--verbos")]
        [InlineData("report", "photos.db", "--verbos")]
        [InlineData("cancella", "photos.db", "--verbos")]
        [InlineData("report", "photos.db", "--path")]
        [InlineData("analisi", "photos", "--config")]
        public void Parse_RejectsUnknownOptionsAndMissingValues(params string[] args) =>
            Assert.Throws<ArgumentException>(() => CommandLineParser.Parse(args));

        [Fact]
        public void Parse_PreservesExplicitConfigurationAndMultipleScopePaths()
        {
            var options = CommandLineParser.Parse(new[] { "cancella", "photos.db", "--config", "custom.json",
                "--path", "/photos/Album", "--path", "/photos/album", "--verbose" })!;
            Assert.Equal("custom.json", options.ConfigurationPath);
            Assert.Equal(new[] { "/photos/Album", "/photos/album" }, options.ScopePaths);
            Assert.True(options.VerboseDuplicates);
        }

        [Fact]
        public void Parse_AnalisiConPath_ReturnsAnalisiAction()
        {
            string[] args = new[]
            {
                "analisi",
                @"C:\Foto"
            };

            CommandLineOptions? result = Foto_CreaDB2.CommandLineParser.Parse(args);

            Assert.NotNull(result);
            Assert.Equal(Foto_CreaDB2.AppAction.Analisi, result.Action);
            Assert.Equal(@"C:\Foto", result.PathInput);
            Assert.Equal(string.Empty, result.NomeDb); // The effective default comes from configuration.
            Assert.False(result.VerboseDuplicates);
        }

        [Fact]
        public void Parse_AnalisiConDbEVerbose_ParsesCorrectly()
        {
            string[] args = new[]
            {
                "analisi",
                @"C:\Foto",
                @"C:\Db\foto.db",
                "--verbose"
            };

            CommandLineOptions? result = Foto_CreaDB2.CommandLineParser.Parse(args);

            Assert.NotNull(result);
            Assert.Equal(Foto_CreaDB2.AppAction.Analisi, result.Action);
            Assert.Equal(@"C:\Foto", result.PathInput);
            Assert.Equal(@"C:\Db\foto.db", result.NomeDb);
            Assert.True(result.VerboseDuplicates);
        }

        [Fact]
        public void Parse_ReportConDb_ReturnsReportAction()
        {
            string[] args = new[]
            {
                "report",
                @"C:\Db\foto.db"
            };

            CommandLineOptions? result = Foto_CreaDB2.CommandLineParser.Parse(args);

            Assert.NotNull(result);
            Assert.Equal(Foto_CreaDB2.AppAction.Report, result.Action);
            Assert.Equal(@"C:\Db\foto.db", result.NomeDb);
        }

        [Fact]
        public void Parse_CancellaConDbEVerbose_ReturnsCancellaAction()
        {
            string[] args = new[]
            {
                "cancella",
                @"C:\Db\foto.db",
                "--verbose"
            };

            CommandLineOptions? result = Foto_CreaDB2.CommandLineParser.Parse(args);

            Assert.NotNull(result);
            Assert.Equal(Foto_CreaDB2.AppAction.Cancella, result.Action);
            Assert.Equal(@"C:\Db\foto.db", result.NomeDb);
            Assert.True(result.VerboseDuplicates);
        }

        [Fact]
        public void Parse_AzioneNonValida_ThrowsArgumentException()
        {
            string[] args = new[]
            {
                "pippo"
            };

            Assert.Throws<System.ArgumentException>(() =>
                Foto_CreaDB2.CommandLineParser.Parse(args));
        }
    }
}
