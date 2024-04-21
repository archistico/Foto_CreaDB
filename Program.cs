using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MetadataExtractor;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Globalization;

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
    public class Program
    {
        public static void Main(string[] args)
        {
            string[] paths = { @"D:\Foto\2004\2004 Gita Thoronet" }; //D:\Foto
            string nomedb = "foto.db";

            SQLiteConnection sqlite_conn;
            sqlite_conn = CreateConnection(nomedb, true);
            CreateTable(sqlite_conn);

            RecursiveFileProcessor.Cerca(paths, sqlite_conn);

            sqlite_conn.Close();

            Console.Write($"Fine... ");
            Console.ReadKey();
        }

        public static SQLiteConnection CreateConnection(string nomedb, bool cancella)
        {
            if (File.Exists(nomedb) && cancella)
            {
                File.Delete(nomedb);
            }

            SQLiteConnection sqlite_conn;
            sqlite_conn = new SQLiteConnection($"Data Source={nomedb};Version=3;New=True;Compress=True;");
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {

            }
            return sqlite_conn;
        }

        public static void CreateTable(SQLiteConnection conn)
        {
            SQLiteCommand sqlite_cmd;
            string Createsql = "CREATE TABLE FOTO ("
                + "  ID INTEGER PRIMARY KEY AUTOINCREMENT"
                + ", NOMEFILE TEXT NOT NULL"
                + ", CARTELLA TEXT NOT NULL"
                + ", DATA TEXT"
                + ", ESTENSIONE TEXT"
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
                + ", HASH TEXT"
                + ", NUOVOFILE TEXT"
                + ")"
                ;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = Createsql;
            sqlite_cmd.ExecuteNonQuery();
        }

        public static void InsertData(SQLiteConnection conn, Foto foto)
        {
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "INSERT INTO FOTO ("
                + "  NOMEFILE"
                + ", CARTELLA"
                + ", DATA"
                + ", ESTENSIONE"
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
                + ", HASH"
                + ", NUOVOFILE"
                + ") " 
                + " VALUES (@nomefile, @cartella, @data, @estensione, @dimensione"
                + ", @larghezza, @altezza, @marca, @modello, @esposizione"
                + ", @apertura, @iso, @compensazione, @zoom, @hash, @nuovofile);";

            sqlite_cmd.Parameters.AddWithValue("nomefile", foto.nomefile);
            sqlite_cmd.Parameters.AddWithValue("cartella", foto.cartella);
            sqlite_cmd.Parameters.AddWithValue("data", foto.data);
            sqlite_cmd.Parameters.AddWithValue("estensione", foto.estensione);
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
            sqlite_cmd.Parameters.AddWithValue("hash", foto.hash);
            sqlite_cmd.Parameters.AddWithValue("nuovofile", foto.nuovofile);
    
            try {
                sqlite_cmd.ExecuteNonQuery();
            }
            catch (Exception ex) {
                throw new Exception(ex.Message);
            }    
            
        }
    }

    public class RecursiveFileProcessor
    {
        public static SQLiteConnection _sqlite_conn;
        public static List<string> estensioniPermesse = new List<string> { 
            "jpg", "jpeg", "dcr", "dng", "crw",
            "gif", "png", "ico", "raw", "arw", "cr2",
            "avi", "wmv", "wav", "mp4", "mp3", "mov",
            "bmp", "psd", "3gp"
        };

        public static void Cerca(string[] paths, SQLiteConnection sqlite_conn)
        {
            _sqlite_conn = sqlite_conn;

            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    string estensione = Path.GetExtension(path).Replace(".", "").ToLowerInvariant();
                    if (estensioniPermesse.Contains(estensione))
                    {
                        ProcessFile(path);
                    }
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
            {
                string estensione = Path.GetExtension(fileName).Replace(".", "").ToLowerInvariant();
                if (estensioniPermesse.Contains(estensione))
                {
                    ProcessFile(fileName);
                }
            }

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
            try
            {
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

                Foto f = new Foto();

                string data = TagDateTimeOriginal?.Description;

                if (string.IsNullOrWhiteSpace(data) || data == "0000:00:00 00:00:00")
                {
                    FileInfo fInfo = new FileInfo(imagePath);
                    var dataModifica = fInfo.LastWriteTime;
                    data = dataModifica.ToString("yyyy:MM:dd HH:mm:ss");
                }

                CultureInfo itIT = new CultureInfo("it-IT");
                DateTime dateValue;
                if (DateTime.TryParseExact(data, "yyyy:MM:dd HH:mm:ss", itIT, DateTimeStyles.None, out dateValue))
                    data = dateValue.ToString("yyyy-MM-dd HH:mm:ss");
                else
                    data = "0000-01-01 00:00:00";

                f.nomefile = TagFileName?.Description;
                f.data = data;
                f.cartella = imagePath;
                f.estensione = Path.GetExtension(imagePath).Replace(".", "").ToLowerInvariant();
                f.dimensione = imageSize;
                f.larghezza = imageWidth;
                f.altezza = imageHeight;
                f.marca = TagMake?.Description;
                f.modello = TagModel?.Description;
                f.esposizione = TagExposureTime?.Description;
                f.apertura = TagFNumber?.Description;
                f.iso = TagISOSpeedRatings?.Description;
                f.compensazione = TagExposureBiasValue?.Description;
                f.zoom = TagFocalLength?.Description;
                f.hash = BytesToString(GetHashSha256(imagePath));
                f.nuovofile = f.CalcNuovofile();

                Program.InsertData(_sqlite_conn, f);

                Console.WriteLine(f.ToString());
            }
            catch (Exception)
            {
                Foto f = new Foto();
                FileInfo fInfo = new FileInfo(imagePath);
                
                f.nomefile = Path.GetFileName(imagePath);
                f.data = fInfo.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss");
                f.cartella = imagePath;
                f.estensione = Path.GetExtension(imagePath).Replace(".", "").ToLowerInvariant();
                f.dimensione = Convert.ToInt32(fInfo.Length);
                f.larghezza = 0;
                f.altezza = 0;
                f.marca = "";
                f.modello = "";
                f.esposizione = "";
                f.apertura = "";
                f.iso = "";
                f.compensazione = "";
                f.zoom = "";
                f.hash = BytesToString(GetHashSha256(imagePath));

                Program.InsertData(_sqlite_conn, f);
                Console.WriteLine(f.ToString());
            }
            
        }

        private static byte[] GetHashSha256(string filename)
        {
            SHA256 Sha256 = SHA256.Create();
            using (FileStream stream = File.OpenRead(filename))
            {
                return Sha256.ComputeHash(stream);
            }
        }
        public static string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes) result += b.ToString("x2");
            return result;
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
        public string estensione { get; set; } = ""; 
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
        public string hash { get; set; } = "";
        public string nuovofile { get; set; } = "";

        public override string ToString()
        {
            return $"File: {cartella} | {dimensione.ToString()} bytes";
        }

        public string CalcNuovofile()
        {
            string ris = "";
            string p1 = "";

            CultureInfo itIT = new CultureInfo("it-IT");
            DateTime dateValue;
            if (DateTime.TryParseExact(data, "yyyy-MM-dd HH:mm:ss", itIT, DateTimeStyles.None, out dateValue))
                p1 = dateValue.ToString("yyyy_MM_dd_HH_mm_ss");
            else
                p1 = "0000_01_01_00_00_00";

            ris = $"{p1}.{this.estensione}";
            return ris;
        }
    }
}
