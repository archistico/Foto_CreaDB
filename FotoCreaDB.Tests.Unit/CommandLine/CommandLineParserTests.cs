using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.CommandLine
{
    public class CommandLineParserTests
    {
        [Fact]
        public void Parse_AnalisiConPath_ReturnsAnalisiAction()
        {
            string[] args = new[]
            {
                "analisi",
                @"C:\Foto"
            };

            CommandLineOptions result = Foto_CreaDB2.CommandLineParser.Parse(args);

            Assert.NotNull(result);
            Assert.Equal(Foto_CreaDB2.AppAction.Analisi, result.Action);
            Assert.Equal(@"C:\Foto", result.PathInput);
            Assert.Equal("foto.db", result.NomeDb);
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

            CommandLineOptions result = Foto_CreaDB2.CommandLineParser.Parse(args);

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

            CommandLineOptions result = Foto_CreaDB2.CommandLineParser.Parse(args);

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

            CommandLineOptions result = Foto_CreaDB2.CommandLineParser.Parse(args);

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