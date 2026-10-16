using Foto_CreaDB2;
using FotoCreaDB.Tests.Integration.TestHelpers;

namespace FotoCreaDB.Tests.Integration.Analysis;

public class PathCaseCompatibilityTests
{
    [Fact]
    public void Repository_DoesNotMergeCaseDistinctPaths()
    {
        using TemporaryTestFolder folder = new();
        using var db = new DatabaseManager(folder.GetPath("photos.db"), false);
        db.Initialize();
        using var repository = new FotoRepository(db.Connection!);
        string lower = folder.GetPath("photo.jpg");
        string upper = folder.GetPath("PHOTO.jpg");
        // Database identities remain exact even when the test host's filesystem is case insensitive.
        repository.Insert(new Foto { percorsoCompleto = lower, hashSha256 = new string('a', 64) });
        repository.Insert(new Foto { percorsoCompleto = upper, hashSha256 = new string('b', 64) });
        var first = repository.GetByPercorso(lower)!;
        var second = repository.GetByPercorso(upper)!;
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(lower, first.PercorsoCompleto);
        Assert.Equal(upper, second.PercorsoCompleto);
        Assert.NotEqual(first.HashSha256, second.HashSha256);
        Assert.Empty(repository.GetBinaryDuplicateDecisions());
    }
}
