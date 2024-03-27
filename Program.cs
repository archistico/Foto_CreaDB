using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MetadataExtractor;
using System.Data.SQLite;

#pragma warning disable 8321

// https://github.com/drewnoakes/metadata-extractor-dotnet
// Install-Package MetadataExtractor
// dotnet add package MetadataExtractor
// dotnet add package System.Data.SQLite --version 1.0.118
// https://www.codeguru.com/dotnet/using-sqlite-in-a-c-application/
// dotnet run --property WarningLevel=0

namespace Foto_CreaDB2
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] paths = { @"/home/emilie/Immagini" };
            RecursiveFileProcessor.Cerca(paths);

            Console.Write($"Fine... ");
            Console.ReadKey();
        }                
    }

    public class RecursiveFileProcessor
    {
        public static void Cerca(string[] paths)
        {
            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    ProcessFile(path);
                }
                else if (System.IO.Directory.Exists(path))
                {
                    ProcessDirectory(path);
                }
                else
                {
                    Console.WriteLine("{0} is not a valid file or directory.", path);
                    throw new Exception();
                }
            }
        }

        public static void ProcessDirectory(string targetDirectory)
        {
            string[] fileEntries = System.IO.Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName);

            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        public static void ProcessFile(string imagePath)
        {
            /*
            File Type - Detected MIME Type = image/jpeg
            File - File Name = 3249781560.jpg
            File - File Size = 129882 bytes
            
            JPEG - Image Height = 900 pixels
            JPEG - Image Width = 600 pixels
            
            Exif IFD0 - Make = SONY
            Exif IFD0 - Model = ILCE-6600
            
            Exif SubIFD - Exposure Time = 1/200 sec
            Exif SubIFD - F-Number = f/2,8
            Exif SubIFD - ISO Speed Ratings = 100
            Exif SubIFD - Date/Time Original = 2019:08:28 15:26:37
            Exif SubIFD - Exposure Bias Value = 0 EV
            Exif SubIFD - Focal Length = 36 mm            
            */

            IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(imagePath);
            var TagImageWidth = GetTag(directories, "JPEG", "Image Width");
            var TagImageHeight = GetTag(directories, "JPEG", "Image Height");
            var TagFileType = GetTag(directories, "File Type", "Detected MIME Type");
            var TagFileName = GetTag(directories, "File", "File Name");
            var TagFileSize = GetTag(directories, "File", "File Size");
            var TagMake = GetTag(directories, "Exif IFD0", "Make");
            var TagModel = GetTag(directories, "Exif IFD0", "Model");
            var TagExposureTime = GetTag(directories, "Exif SubIFD", "Exposure Time");
            var TagFNumber = GetTag(directories, "Exif SubIFD", "F-Number");
            var TagISOSpeedRatings = GetTag(directories, "Exif SubIFD", "ISO Speed Ratings");
            var TagDateTimeOriginal = GetTag(directories, "Exif SubIFD", "Date/Time Original");
            var TagExposureBiasValue = GetTag(directories, "Exif SubIFD", "Exposure Bias Value");
            var TagFocalLength = GetTag(directories, "Exif SubIFD", "Focal Length");

            int imageWidth = ConvertiString2Int(TagImageWidth?.Description, "", "pixels", 0);
            int imageHeight = ConvertiString2Int(TagImageHeight?.Description, "", "pixels", 0);
            int imageSize = ConvertiString2Int(TagFileSize?.Description, "", "bytes", 0);

            Console.WriteLine($"-----------------------------------------------");
            Console.WriteLine($"{TagFileName?.Name} {TagFileName?.Description}");
            Console.WriteLine($"-----------------------------------------------");
            Console.WriteLine($"{imagePath}");
            Console.WriteLine($"{TagFileType?.Name} {TagFileType?.Description}");
            Console.WriteLine($"{TagFileSize?.Name} {TagFileSize?.Description}");

            Console.WriteLine($"W:{imageWidth} H:{imageHeight} S:{imageSize}");
            Console.WriteLine($"{TagImageWidth?.Name} {TagImageWidth?.Description}");
            Console.WriteLine($"{TagImageHeight?.Name} {TagImageHeight?.Description}");

            Console.WriteLine($"{TagMake?.Name} {TagMake?.Description}");
            Console.WriteLine($"{TagModel?.Name} {TagModel?.Description}");

            Console.WriteLine($"{TagExposureTime?.Name} {TagExposureTime?.Description}");
            Console.WriteLine($"{TagFNumber?.Name} {TagFNumber?.Description}");
            Console.WriteLine($"{TagISOSpeedRatings?.Name} {TagISOSpeedRatings?.Description}");
            Console.WriteLine($"{TagDateTimeOriginal?.Name} {TagDateTimeOriginal?.Description}");
            Console.WriteLine($"{TagExposureBiasValue?.Name} {TagExposureBiasValue?.Description}");
            Console.WriteLine($"{TagFocalLength?.Name} {TagFocalLength?.Description}");
        }

        public static Tag GetTag(IEnumerable<MetadataExtractor.Directory> directories, string _directory, string _tag)
        {
            var directory = directories.FirstOrDefault(c => c.Name == _directory);
            var tag = directory?.Tags.FirstOrDefault(c => c.Name == _tag);
            return tag;
        }

        public static int ConvertiString2Int(string s, string prefisso = "", string postfisso = "", int def = 0)
        {
            if (String.IsNullOrEmpty(s))
            {
                return def;
            }

            string s1 = Regex.Replace(s, $"^{prefisso}", "");
            string s2 = Regex.Replace(s1, $"{postfisso}$", "");
            string s3 = s2.Trim();

            int ris = def;

            try
            {
                ris = Convert.ToInt32(s3);
            }
            catch (Exception)
            {
                return def;
            }

            return ris;
        }
    }
}