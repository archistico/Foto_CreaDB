using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.Utilities
{
    public class DuplicateReportProgressTests
    {
        [Fact]
        public void Percentage_WithZeroTotal_ReturnsZero()
        {
            DuplicateReportProgress progress = new DuplicateReportProgress
            {
                ProcessedGroups = 2,
                TotalGroups = 0
            };

            Assert.Equal(0, progress.Percentage);
        }

        [Fact]
        public void Percentage_WithValidValues_ReturnsExpectedPercentage()
        {
            DuplicateReportProgress progress = new DuplicateReportProgress
            {
                ProcessedGroups = 3,
                TotalGroups = 12
            };

            Assert.Equal(25, progress.Percentage);
        }
    }
}