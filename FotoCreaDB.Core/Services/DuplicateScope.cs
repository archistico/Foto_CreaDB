namespace Foto_CreaDB2;

/// <summary>Filter before choosing keepers: excluded files cannot authorize deletion.</summary>
internal sealed class DuplicateScope
{
    private readonly HashSet<string> _extensions;
    private readonly string[] _paths;

    public DuplicateScope(AppConfig config, string[] paths)
    {
        _extensions = new HashSet<string>(config.EstensioniPermesse.Select(e => e.Trim().TrimStart('.')),
            StringComparer.OrdinalIgnoreCase);
        _paths = (paths ?? throw new ArgumentNullException(nameof(paths))).Select(p =>
        {
            if (string.IsNullOrWhiteSpace(p)) throw new ArgumentException("Ambito duplicati: percorso vuoto.");
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(p));
        }).ToArray();
    }

    public bool Includes(DuplicateBinaryCandidate candidate)
    {
        if (!_extensions.Contains(Utility.GetEstensione(candidate.PercorsoCompleto))) return false;
        string full = Path.GetFullPath(candidate.PercorsoCompleto);
        return _paths.Length == 0 || _paths.Any(root =>
            string.Equals(root, full, PathIdentity.Comparison) ||
            full.StartsWith(Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar,
                PathIdentity.Comparison));
    }
}
