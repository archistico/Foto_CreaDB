using System;
using System.Collections.Generic;
using System.Linq;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Fornisce metodi di supporto per cercare descrizioni di tag
    /// all'interno delle directory di metadati lette dai file.
    /// </summary>
    public static class MetadataLookup
    {
        /// <summary>
        /// Cerca e restituisce la prima descrizione valorizzata trovata
        /// tra i tag specificati nell'ordine ricevuto.
        /// </summary>
        /// <param name="directories">
        /// Collezione di directory di metadati in cui eseguire la ricerca.
        /// </param>
        /// <param name="searches">
        /// Elenco ordinato dei criteri di ricerca dei tag.
        /// </param>
        /// <returns>
        /// La prima descrizione trovata, oppure <c>null</c> se nessun tag corrisponde.
        /// </returns>
        public static string? FindFirstDescription(IEnumerable<MetadataExtractor.Directory>? directories, params TagSearch[]? searches)
        {
            if (directories == null)
            {
                return null;
            }

            if (searches == null || searches.Length == 0)
            {
                return null;
            }

            foreach (TagSearch search in searches)
            {
                if (search == null || string.IsNullOrWhiteSpace(search.TagName))
                {
                    continue;
                }

                string? description = FindDescription(directories, search.DirectoryName, search.TagName);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    return description;
                }
            }

            return null;
        }

        /// <summary>
        /// Cerca la descrizione di un tag all'interno di una directory specifica
        /// oppure, se non indicata, in tutte le directory disponibili.
        /// </summary>
        /// <param name="directories">
        /// Directory di metadati in cui cercare.
        /// </param>
        /// <param name="directoryName">
        /// Nome della directory da limitare alla ricerca, oppure <c>null</c> per cercare ovunque.
        /// </param>
        /// <param name="tagName">
        /// Nome del tag da cercare.
        /// </param>
        /// <returns>
        /// Descrizione del tag trovata, oppure <c>null</c> se assente o vuota.
        /// </returns>
        private static string? FindDescription(IEnumerable<MetadataExtractor.Directory> directories, string? directoryName, string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(directoryName))
            {
                var directory = directories.FirstOrDefault(d =>
                    string.Equals(d.Name, directoryName, StringComparison.OrdinalIgnoreCase));

                if (directory != null)
                {
                    var tag = directory.Tags.FirstOrDefault(t =>
                        string.Equals(t.Name, tagName, StringComparison.OrdinalIgnoreCase));

                    if (tag != null && !string.IsNullOrWhiteSpace(tag.Description))
                    {
                        return tag.Description;
                    }
                }
            }
            else
            {
                foreach (var directory in directories)
                {
                    var tag = directory.Tags.FirstOrDefault(t =>
                        string.Equals(t.Name, tagName, StringComparison.OrdinalIgnoreCase));

                    if (tag != null && !string.IsNullOrWhiteSpace(tag.Description))
                    {
                        return tag.Description;
                    }
                }
            }

            return null;
        }
    }
}