namespace Foto_CreaDB2
{
    /// <summary>
    /// Rappresenta un criterio di ricerca di un tag nei metadati,
    /// composto facoltativamente dal nome della directory e dal nome del tag.
    /// </summary>
    public class TagSearch
    {
        /// <summary>
        /// Nome della directory di metadati in cui cercare il tag.
        /// Se nullo, la ricerca può essere eseguita su tutte le directory disponibili.
        /// </summary>
        public string? DirectoryName { get; }

        /// <summary>
        /// Nome del tag da cercare.
        /// </summary>
        public string TagName { get; }

        /// <summary>
        /// Inizializza un nuovo criterio di ricerca per un tag di metadati.
        /// </summary>
        /// <param name="directoryName">
        /// Nome della directory in cui cercare il tag, oppure <c>null</c> per cercare ovunque.
        /// </param>
        /// <param name="tagName">
        /// Nome del tag da cercare.
        /// </param>
        public TagSearch(string? directoryName, string tagName)
        {
            DirectoryName = directoryName;
            TagName = tagName;
        }
    }
}