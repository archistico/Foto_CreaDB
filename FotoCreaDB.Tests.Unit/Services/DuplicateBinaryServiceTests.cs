using System.Collections.Generic;
using System.Linq;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.Services
{
    public class DuplicateBinaryServiceTests
    {
        [Fact]
        public void BuildDecisions_WithNullInput_ReturnsEmptyList()
        {
            DuplicateBinaryService service = new DuplicateBinaryService();

            List<DuplicateBinaryDecision> result = service.BuildDecisions(null);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void BuildDecisions_WithSingleFile_ReturnsEmptyList()
        {
            DuplicateBinaryService service = new DuplicateBinaryService();

            List<DuplicateBinaryCandidate> files = new List<DuplicateBinaryCandidate>
            {
                CreateCandidate(
                    id: 1,
                    path: @"C:\Foto\a.jpg",
                    hash: "HASH1",
                    size: 100,
                    dataFileModifica: "2026-04-09 10:00:00",
                    dataScatto: "")
            };

            List<DuplicateBinaryDecision> result = service.BuildDecisions(files);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void BuildDecisions_WithTwoDuplicateFiles_ReturnsOneDecision()
        {
            DuplicateBinaryService service = new DuplicateBinaryService();

            List<DuplicateBinaryCandidate> files = new List<DuplicateBinaryCandidate>
            {
                CreateCandidate(1, @"C:\Foto\A\a.jpg", "HASH1", 100, "2026-04-09 10:00:00", ""),
                CreateCandidate(2, @"C:\Foto\B\b.jpg", "HASH1", 100, "2026-04-09 09:00:00", "")
            };

            List<DuplicateBinaryDecision> result = service.BuildDecisions(files);

            Assert.Single(result);
            Assert.Equal("HASH1", result[0].HashSha256);
            Assert.Single(result[0].FileDaEliminare);
        }

        [Fact]
        public void BuildDecisions_ChoosesLongestPathFirst()
        {
            DuplicateBinaryService service = new DuplicateBinaryService();

            DuplicateBinaryCandidate shortPath = CreateCandidate(
                1,
                @"C:\Foto\a.jpg",
                "HASH1",
                100,
                "2026-04-09 10:00:00",
                "");

            DuplicateBinaryCandidate longPath = CreateCandidate(
                2,
                @"C:\Foto\2026\Vacanze al mare\a.jpg",
                "HASH1",
                100,
                "2026-04-09 09:00:00",
                "");

            List<DuplicateBinaryDecision> result = service.BuildDecisions(
                new List<DuplicateBinaryCandidate> { shortPath, longPath });

            Assert.Single(result);
            Assert.Equal(longPath.PercorsoCompleto, result[0].FileDaTenere.PercorsoCompleto);
            Assert.Equal(shortPath.PercorsoCompleto, result[0].FileDaEliminare[0].PercorsoCompleto);
        }

        [Fact]
        public void BuildDecisions_WhenPathLengthIsEqual_ChoosesMostRecentDate()
        {
            DuplicateBinaryService service = new DuplicateBinaryService();

            DuplicateBinaryCandidate older = CreateCandidate(
                1,
                @"C:\Foto\A\a.jpg",
                "HASH1",
                100,
                "2026-04-08 10:00:00",
                "");

            DuplicateBinaryCandidate newer = CreateCandidate(
                2,
                @"D:\Foto\B\b.jpg",
                "HASH1",
                100,
                "2026-04-09 10:00:00",
                "");

            List<DuplicateBinaryDecision> result = service.BuildDecisions(
                new List<DuplicateBinaryCandidate> { older, newer });

            Assert.Single(result);
            Assert.Equal(newer.Id, result[0].FileDaTenere.Id);
            Assert.Equal(older.Id, result[0].FileDaEliminare[0].Id);
        }

        [Fact]
        public void BuildDecisions_WhenPathAndDateAreEqual_ChoosesLowerId()
        {
            DuplicateBinaryService service = new DuplicateBinaryService();

            DuplicateBinaryCandidate lowerId = CreateCandidate(
                1,
                @"C:\Foto\A\a.jpg",
                "HASH1",
                100,
                "2026-04-09 10:00:00",
                "");

            DuplicateBinaryCandidate higherId = CreateCandidate(
                2,
                @"D:\Foto\B\b.jpg",
                "HASH1",
                100,
                "2026-04-09 10:00:00",
                "");

            List<DuplicateBinaryDecision> result = service.BuildDecisions(
                new List<DuplicateBinaryCandidate> { higherId, lowerId });

            Assert.Single(result);
            Assert.Equal(lowerId.Id, result[0].FileDaTenere.Id);
            Assert.Equal(higherId.Id, result[0].FileDaEliminare[0].Id);
        }

        [Fact]
        public void BuildDecisions_WithThreeDuplicates_ReturnsOneToKeepAndTwoToDelete()
        {
            DuplicateBinaryService service = new DuplicateBinaryService();

            List<DuplicateBinaryCandidate> files = new List<DuplicateBinaryCandidate>
            {
                CreateCandidate(1, @"C:\Foto\a.jpg", "HASH1", 100, "2026-04-09 08:00:00", ""),
                CreateCandidate(2, @"C:\Foto\2026\Evento\a.jpg", "HASH1", 100, "2026-04-09 07:00:00", ""),
                CreateCandidate(3, @"C:\Foto\Da sistemare\a.jpg", "HASH1", 100, "2026-04-09 06:00:00", "")
            };

            List<DuplicateBinaryDecision> result = service.BuildDecisions(files);

            Assert.Single(result);
            Assert.Equal(2, result[0].FileDaEliminare.Count);
        }

        [Fact]
        public void BuildDecisions_WithDifferentHashes_ReturnsNoDecision()
        {
            DuplicateBinaryService service = new DuplicateBinaryService();

            List<DuplicateBinaryCandidate> files = new List<DuplicateBinaryCandidate>
            {
                CreateCandidate(1, @"C:\Foto\a.jpg", "HASH1", 100, "2026-04-09 10:00:00", ""),
                CreateCandidate(2, @"C:\Foto\b.jpg", "HASH2", 100, "2026-04-09 10:00:00", "")
            };

            List<DuplicateBinaryDecision> result = service.BuildDecisions(files);

            Assert.Empty(result);
        }

        [Fact]
        public void BuildDecisions_WithSameHashAndDifferentDataScatto_SeparatesGroups()
        {
            DuplicateBinaryService service = new DuplicateBinaryService();

            List<DuplicateBinaryCandidate> files = new List<DuplicateBinaryCandidate>
            {
                CreateCandidate(1, @"C:\Foto\a.jpg", "HASH1", 100, "2026-04-09 10:00:00", "2025-08-10 12:00:00"),
                CreateCandidate(2, @"C:\Foto\b.jpg", "HASH1", 100, "2026-04-09 10:00:00", "2025-08-10 12:00:00"),
                CreateCandidate(3, @"C:\Foto\c.jpg", "HASH1", 100, "2026-04-09 10:00:00", "2024-01-01 09:00:00")
            };

            List<DuplicateBinaryDecision> result = service.BuildDecisions(files);

            Assert.Single(result);
            Assert.Single(result[0].FileDaEliminare);
        }

        private static DuplicateBinaryCandidate CreateCandidate(
            long id,
            string path,
            string hash,
            long size,
            string dataFileModifica,
            string dataScatto)
        {
            return new DuplicateBinaryCandidate
            {
                Id = id,
                PercorsoCompleto = path,
                HashSha256 = hash,
                Dimensione = size,
                DataFileModifica = dataFileModifica,
                DataScatto = dataScatto
            };
        }
    }
}