using System;
using System.Collections.Generic;
using MetadataExtractor;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Gestisce la lettura dei metadati dei file e il popolamento
    /// delle proprietà corrispondenti dell'oggetto <see cref="Foto"/>.
    /// </summary>
    public class MetadataService
    {
        /// <summary>
        /// Prova a leggere i metadati del file specificato e ad aggiornare l'oggetto foto.
        /// </summary>
        /// <param name="imagePath">
        /// Percorso completo del file di cui leggere i metadati.
        /// </param>
        /// <param name="foto">
        /// Oggetto foto da arricchire con i metadati trovati.
        /// </param>
        /// <param name="logger">
        /// Logger usato per registrare eventuali errori durante la lettura.
        /// </param>
        /// <param name="stats">
        /// Statistiche della scansione da aggiornare in caso di errore.
        /// </param>
        /// <returns>
        /// <c>true</c> se è stato trovato almeno un metadato utile; altrimenti <c>false</c>.
        /// </returns>
        public bool TryPopulateMetadata(string imagePath, Foto foto, Logger logger, ScanStatistics stats)
        {
            bool trovatoAlmenoUnMetadato = false;

            try
            {
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(imagePath);

                string? tagImageWidth = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("JPEG", "Image Width"),
                    new TagSearch("PNG-IHDR", "Image Width"),
                    new TagSearch("Exif IFD0", "Image Width"),
                    new TagSearch("Exif SubIFD", "Exif Image Width"),
                    new TagSearch("Exif SubIFD", "Image Width"),
                    new TagSearch("JFIF", "Image Width"),
                    new TagSearch(null, "Exif Image Width"),
                    new TagSearch(null, "Image Width"),
                    new TagSearch(null, "Width")
                );

                string? tagImageHeight = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("JPEG", "Image Height"),
                    new TagSearch("PNG-IHDR", "Image Height"),
                    new TagSearch("Exif IFD0", "Image Height"),
                    new TagSearch("Exif SubIFD", "Exif Image Height"),
                    new TagSearch("Exif SubIFD", "Image Height"),
                    new TagSearch("JFIF", "Image Height"),
                    new TagSearch(null, "Exif Image Height"),
                    new TagSearch(null, "Image Height"),
                    new TagSearch(null, "Height")
                );

                string? tagFileName = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("File", "File Name"),
                    new TagSearch(null, "File Name")
                );

                string? tagFileSize = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("File", "File Size"),
                    new TagSearch(null, "File Size")
                );

                string? tagMimeType = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("File Type", "Detected MIME Type"),
                    new TagSearch(null, "Detected MIME Type"),
                    new TagSearch(null, "MIME Type")
                );

                string? tagMake = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif IFD0", "Make"),
                    new TagSearch(null, "Make")
                );

                string? tagModel = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif IFD0", "Model"),
                    new TagSearch(null, "Model")
                );

                string? tagExposureTime = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif SubIFD", "Exposure Time"),
                    new TagSearch(null, "Exposure Time")
                );

                string? tagFNumber = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif SubIFD", "F-Number"),
                    new TagSearch(null, "F-Number"),
                    new TagSearch(null, "Aperture")
                );

                string? tagISOSpeedRatings = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif SubIFD", "ISO Speed Ratings"),
                    new TagSearch(null, "ISO Speed Ratings"),
                    new TagSearch(null, "Photographic Sensitivity"),
                    new TagSearch(null, "ISO")
                );

                string? tagDateTimeOriginal = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif SubIFD", "Date/Time Original"),
                    new TagSearch(null, "Date/Time Original"),
                    new TagSearch("Exif SubIFD", "Date/Time Digitized"),
                    new TagSearch(null, "Date/Time Digitized"),
                    new TagSearch("Exif IFD0", "Date/Time"),
                    new TagSearch(null, "Date/Time"),
                    new TagSearch("File", "File Modified Date"),
                    new TagSearch(null, "File Modified Date")
                );

                string? tagExposureBiasValue = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif SubIFD", "Exposure Bias Value"),
                    new TagSearch(null, "Exposure Bias Value")
                );

                string? tagFocalLength = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif SubIFD", "Focal Length"),
                    new TagSearch(null, "Focal Length")
                );

                int imageWidth = Utility.ConvertiString2Int(tagImageWidth, "", "pixels", foto.larghezza);
                int imageHeight = Utility.ConvertiString2Int(tagImageHeight, "", "pixels", foto.altezza);
                long imageSize = Utility.ConvertiString2Long(tagFileSize, "", "bytes", foto.dimensione);

                string dataScatto = Utility.NormalizeMetadataDate(tagDateTimeOriginal, "");

                if (!string.IsNullOrWhiteSpace(tagFileName))
                {
                    foto.nomefile = tagFileName;
                    foto.nomefileNorm = Utility.NormalizeFileNameWithoutExtension(tagFileName);
                    trovatoAlmenoUnMetadato = true;
                }

                foto.mimeType = tagMimeType ?? "";
                foto.dataScatto = dataScatto;
                foto.dimensione = imageSize > 0 ? imageSize : foto.dimensione;
                foto.larghezza = imageWidth;
                foto.altezza = imageHeight;
                foto.marca = tagMake ?? "";
                foto.modello = tagModel ?? "";
                foto.esposizione = tagExposureTime ?? "";
                foto.apertura = tagFNumber ?? "";
                foto.iso = tagISOSpeedRatings ?? "";
                foto.compensazione = tagExposureBiasValue ?? "";
                foto.zoom = tagFocalLength ?? "";

                if (imageWidth > 0) trovatoAlmenoUnMetadato = true;
                if (imageHeight > 0) trovatoAlmenoUnMetadato = true;
                if (!string.IsNullOrWhiteSpace(tagMake)) trovatoAlmenoUnMetadato = true;
                if (!string.IsNullOrWhiteSpace(tagModel)) trovatoAlmenoUnMetadato = true;
                if (!string.IsNullOrWhiteSpace(tagExposureTime)) trovatoAlmenoUnMetadato = true;
                if (!string.IsNullOrWhiteSpace(tagFNumber)) trovatoAlmenoUnMetadato = true;
                if (!string.IsNullOrWhiteSpace(tagISOSpeedRatings)) trovatoAlmenoUnMetadato = true;
                if (!string.IsNullOrWhiteSpace(dataScatto)) trovatoAlmenoUnMetadato = true;
                if (!string.IsNullOrWhiteSpace(tagExposureBiasValue)) trovatoAlmenoUnMetadato = true;
                if (!string.IsNullOrWhiteSpace(tagFocalLength)) trovatoAlmenoUnMetadato = true;
                if (!string.IsNullOrWhiteSpace(tagMimeType)) trovatoAlmenoUnMetadato = true;

                foto.metadatiPresenti = trovatoAlmenoUnMetadato;
                return trovatoAlmenoUnMetadato;
            }
            catch (ImageProcessingException ex)
            {
                stats.TotaleErroriMetadati++;
                foto.metadatiPresenti = false;
                foto.noteErrore = Utility.Truncate("Metadati non leggibili: " + ex.Message, 1000);
                logger.WriteError($"Metadati non leggibili per il file '{imagePath}'", ex);
                return false;
            }
            catch (Exception ex)
            {
                stats.TotaleErroriMetadati++;
                foto.metadatiPresenti = false;
                foto.noteErrore = Utility.Truncate("Errore metadati: " + ex.Message, 1000);
                logger.WriteError($"Errore durante la lettura dei metadati del file '{imagePath}'", ex);
                return false;
            }
        }
    }
}