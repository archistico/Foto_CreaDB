using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.Utilities
{
    public class AnalysisProgressTests
    {
        [Fact]
        public void Percentage_WithZeroTotal_ReturnsZero()
        {
            AnalysisProgress progress = new AnalysisProgress
            {
                ProcessedFiles = 5,
                TotalFiles = 0
            };

            Assert.Equal(0, progress.Percentage);
        }

        [Fact]
        public void Percentage_WithValidValues_ReturnsExpectedPercentage()
        {
            AnalysisProgress progress = new AnalysisProgress
            {
                ProcessedFiles = 25,
                TotalFiles = 100
            };

            Assert.Equal(25, progress.Percentage);
        }
    }
}