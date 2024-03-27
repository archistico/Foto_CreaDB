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
// Su VSCode extension: sqlite by alexcvzz
// sudo apt install sqlite3

// TODO: Aggiungere HASH?

namespace Foto_CreaDB2
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] paths = { @"/home/emilie/Immagini" };
            //RecursiveFileProcessor.Cerca(paths);

            SQLiteConnection sqlite_conn;
            sqlite_conn = CreateConnection();
            CreateTable(sqlite_conn);

            Foto f = new Foto();
            f.nomefile = "nomefile.jpg";
            f.data = DateTime.Now.ToString();
            f.cartella = "cartella";

            InsertData(sqlite_conn, f);
            //ReadData(sqlite_conn);

            sqlite_conn.Close();

            Console.Write($"Fine... ");
            Console.ReadKey();
        }

        static SQLiteConnection CreateConnection()
        {

            SQLiteConnection sqlite_conn;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection("Data Source=foto.db;Version=3;New=True;Compress=True;");
            // Open the connection:
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {

            }
            return sqlite_conn;
        }

        static void CreateTable(SQLiteConnection conn)
        {
            SQLiteCommand sqlite_cmd;
            string Createsql = "CREATE TABLE FOTO ("
                + "  ID INTEGER PRIMARY KEY AUTOINCREMENT"
                + ", NOMEFILE TEXT NOT NULL"
                + ", CARTELLA TEXT NOT NULL"
                + ", DATA TEXT"
                + ", MIME TEXT"
                + ", DIMENSIONE INTEGER"
                + ", LARGHEZZA INTEGER"
                + ", ALTEZZA INTEGER"
                + ", MARCA TEXT"
                + ", MODELLO TEXT"
                + ", ESPOSIZIONE TEXT"
                + ", APERTURA TEXT"
                + ", ISO TEXT"
                + ", COMPENSAZIONE TEXT"
                + ", ZOOM TEXT"
                + ")"
                ;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = Createsql;
            sqlite_cmd.ExecuteNonQuery();
        }

        static void InsertData(SQLiteConnection conn, Foto foto)
        {
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "INSERT INTO FOTO ("
                + "  NOMEFILE"
                + ", CARTELLA"
                + ", DATA"
                + ", MIME"
                + ", DIMENSIONE"
                + ", LARGHEZZA"
                + ", ALTEZZA"
                + ", MARCA"
                + ", MODELLO"
                + ", ESPOSIZIONE"
                + ", APERTURA"
                + ", ISO"
                + ", COMPENSAZIONE"
                + ", ZOOM"
                + ") " 
                + " VALUES (@nomefile, @cartella, @data, @mime, @dimensione"
                + ", @larghezza, @altezza, @marca, @modello, @esposizione"
                + ", @apertura, @iso, @compensazione, @zoom);";

            sqlite_cmd.Parameters.AddWithValue("nomefile", foto.nomefile);
            sqlite_cmd.Parameters.AddWithValue("cartella", foto.cartella);
            sqlite_cmd.Parameters.AddWithValue("data", foto.data);
            sqlite_cmd.Parameters.AddWithValue("mime", foto.mime);
            sqlite_cmd.Parameters.AddWithValue("dimensione", foto.dimensione);
            sqlite_cmd.Parameters.AddWithValue("larghezza", foto.larghezza);
            sqlite_cmd.Parameters.AddWithValue("altezza", foto.altezza);
            sqlite_cmd.Parameters.AddWithValue("marca", foto.marca);
            sqlite_cmd.Parameters.AddWithValue("modello", foto.modello);
            sqlite_cmd.Parameters.AddWithValue("esposizione", foto.esposizione);
            sqlite_cmd.Parameters.AddWithValue("apertura", foto.apertura);
            sqlite_cmd.Parameters.AddWithValue("iso", foto.iso);
            sqlite_cmd.Parameters.AddWithValue("compensazione", foto.compensazione);
            sqlite_cmd.Parameters.AddWithValue("zoom", foto.zoom);
    
            try {
                sqlite_cmd.ExecuteNonQuery();
            }
            catch (Exception ex) {
                throw new Exception(ex.Message);
            }    
            
        }

        static void ReadData(SQLiteConnection conn)
        {
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT * FROM SampleTable";

            sqlite_datareader = sqlite_cmd.ExecuteReader();
            while (sqlite_datareader.Read())
            {
                string myreader = sqlite_datareader.GetString(0);
                Console.WriteLine(myreader);
            }
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

    public class Foto
    {
        public string nomefile { get; set; } = ""; 
        public string cartella { get; set; } = ""; 
        public string data { get; set; } = ""; 
        public string mime { get; set; } = ""; 
        public int dimensione { get; set; } = 0; 
        public int larghezza { get; set; } = 0; 
        public int altezza { get; set; } = 0; 
        public string marca { get; set; } = ""; 
        public string modello { get; set; } = ""; 
        public string esposizione { get; set; } = ""; 
        public string apertura { get; set; } = ""; 
        public string iso { get; set; } = ""; 
        public string compensazione { get; set; } = ""; 
        public string zoom { get; set; } = ""; 
    }
}