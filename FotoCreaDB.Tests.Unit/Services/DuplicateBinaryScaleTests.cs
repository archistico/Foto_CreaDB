using System.Diagnostics;
using System.Globalization;
using Foto_CreaDB2;
using Xunit.Abstractions;

namespace FotoCreaDB.Tests.Unit.Services;

public class DuplicateBinaryScaleTests(ITestOutputHelper output)
{
    [Fact]
    public void BuildDecisions_With150000IdenticalCandidates_AvoidsPairwiseComparisons()
    {
        var candidates = Enumerable.Range(1, 150_000).Select(i => new DuplicateBinaryCandidate
        {
            Id = i, PercorsoCompleto = @"C:\synthetic\" + i.ToString("D6") + ".jpg",
            HashSha256 = new string('a', 64), Dimensione = 12_000_000
        }).ToList();
        var watch = Stopwatch.StartNew();

        var result = new DuplicateBinaryService().BuildDecisions(candidates);

        watch.Stop();
        var decision = Assert.Single(result);
        Assert.Equal(1, decision.FileDaTenere.Id);
        Assert.Equal(149_999, decision.FileDaEliminare.Count);
        output.WriteLine("150,000 candidates grouped in {0} ms", watch.ElapsedMilliseconds);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), "Grouping 150,000 candidates exceeded the generous regression budget.");
    }

    [Fact]
    public void BuildDecisions_PreservesFirstCompatibleGroupingAndKeeperRules()
    {
        var random = new Random(20261016);
        string[] dates = { "", " ", "2024-01-01 10:00:00", "2025-01-01 10:00:00" };
        string[] modified = { "", "invalid", "2024-01-01 10:00:00", "2026-01-01 10:00:00" };
        for (int trial = 0; trial < 300; trial++)
        {
            var candidates = Enumerable.Range(1, 100).Select(i => new DuplicateBinaryCandidate
            {
                Id = i,
                PercorsoCompleto = @"C:\" + new string('x', random.Next(1, 10)) + "\\" + i + ".jpg",
                HashSha256 = random.Next(4) switch { 0 => "hash-a", 1 => "HASH-A", 2 => "hash-b", _ => " " },
                Dimensione = random.Next(3) * 100,
                DataScatto = dates[random.Next(dates.Length)],
                DataFileModifica = modified[random.Next(modified.Length)]
            }).ToList();

            var actual = new DuplicateBinaryService().BuildDecisions(candidates)
                .Select(d => Signature(new[] { d.FileDaTenere }.Concat(d.FileDaEliminare))).Order().ToArray();
            var expected = ReferenceGroups(candidates).Where(g => g.Count > 1)
                .Select(g => Signature(g.OrderByDescending(p => p.PercorsoCompleto.Length)
                    .ThenByDescending(p => DateTime.TryParseExact(p.DataFileModifica, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var date) ? date : DateTime.MinValue).ThenBy(p => p.Id)))
                .Order().ToArray();
            Assert.Equal(expected, actual);
        }
    }

    private static string Signature(IEnumerable<DuplicateBinaryCandidate> candidates) =>
        string.Join(",", candidates.Select(c => c.Id));

    // Deliberately simple pairwise reference for the compatibility rule. It is
    // used only with small sets to verify the optimized algorithm preserves policy.
    private static List<List<DuplicateBinaryCandidate>> ReferenceGroups(List<DuplicateBinaryCandidate> candidates)
    {
        var groups = new List<List<DuplicateBinaryCandidate>>();
        foreach (var item in candidates.Where(c => !string.IsNullOrWhiteSpace(c.HashSha256)))
        {
            var group = groups.FirstOrDefault(g => g.All(other =>
                string.Equals(item.HashSha256, other.HashSha256, StringComparison.OrdinalIgnoreCase)
                && item.Dimensione == other.Dimensione
                && (string.IsNullOrWhiteSpace(item.DataScatto) || string.IsNullOrWhiteSpace(other.DataScatto)
                    || item.DataScatto == other.DataScatto)));
            if (group == null) groups.Add(new() { item });
            else group.Add(item);
        }
        return groups;
    }
}
