using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.Utilities
{
    public class DeletionProgressTests
    {
        [Fact]
        public void Percentage_WithZeroTotal_ReturnsZero()
        {
            DeletionProgress progress = new DeletionProgress
            {
                ProcessedFiles = 3,
                TotalFiles = 0
            };

            Assert.Equal(0, progress.Percentage);
        }

        [Fact]
        public void Percentage_WithValidValues_ReturnsExpectedPercentage()
        {
            DeletionProgress progress = new DeletionProgress
            {
                ProcessedFiles = 40,
                TotalFiles = 80
            };

            Assert.Equal(50, progress.Percentage);
        }
    }
}