using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MetadataExtractor;
using System.Data.SQLite;

#pragma warning disable 8321

namespace Foto_CreaDB2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppConfig config = new AppConfig
            {
                //Paths = new string[] { @"D:\Foto\2004\2004 Gita Thoronet" },   // D:\Foto
                Paths = new string[] { @"C:\Users\rollandine\Pictures\Screenshot" },   // D:\Foto
                NomeDb = "foto.db",
                CancellaDbSeEsiste = true,
                BatchSize = 1000,
                LogDettagliato = false,
                ProgressEvery = 1000
            };

            Logger logger = new Logger(config.LogDettagliato, config.ProgressEvery);
            ScanStatistics stats = new ScanStatistics();
            MetadataService metadataService = new MetadataService();
            HashService hashService = new HashService();

            try
            {
                using (DatabaseManager dbManager = new DatabaseManager(config.NomeDb, config.CancellaDbSeEsiste))
                {
                    dbManager.Initialize();

                    using (InsertManager insertManager = new InsertManager(dbManager.Connection, config.BatchSize))
                    {
                        FileScanner scanner = new FileScanner(
                            config,
                            insertManager,
                            metadataService,
                            hashService,
                            logger,
                            stats);

                        scanner.Scan();
                        insertManager.Flush();
                    }
                }

                logger.WriteLine("");
                logger.WriteLine("Fine.");
            }
            catch (Exception ex)
            {
                logger.WriteError("ERRORE FATALE", ex);
            }

            logger.WriteLine("");
            logger.WriteLine("Premi un tasto per uscire...");
            Console.ReadKey();
        }
    }

    public class AppConfig
    {
        public string[] Paths { get; set; } = Array.Empty<string>();
        public string NomeDb { get; set; } = "foto.db";
        public bool CancellaDbSeEsiste { get; set; } = true;
        public int BatchSize { get; set; } = 1000;
        public bool LogDettagliato { get; set; } = false;
        public int ProgressEvery { get; set; } = 1000;

        public HashSet<string> EstensioniPermesse { get; set; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "jpg", "jpeg", "dcr", "dng", "crw",
                "gif", "png", "ico", "raw", "arw", "cr2",
                "avi", "wmv", "wav", "mp4", "mp3", "mov",
                "bmp", "psd", "3gp"
            };
    }

    public class ScanStatistics
    {
        public long TotalePathIniziali { get; set; }
        public long TotaleCartelleVisitate { get; set; }
        public long TotaleFileTrovati { get; set; }
        public long TotaleFileConEstensioneValida { get; set; }
        public long TotaleFileElaborati { get; set; }
        public long TotaleFileInseriti { get; set; }
        public long TotaleErroriFile { get; set; }
        public long TotaleErroriCartelle { get; set; }
        public long TotaleErroriMetadati { get; set; }
        public long TotaleErroriHash { get; set; }
        public long TotaleErroriDb { get; set; }
        public long TotaleFileSenzaMetadatiUtili { get; set; }
    }

    public class Logger
    {
        private readonly bool _logDettagliato;
        private readonly int _progressEvery;

        public Logger(bool logDettagliato, int progressEvery)
        {
            _logDettagliato = logDettagliato;
            _progressEvery = progressEvery <= 0 ? 1000 : progressEvery;
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteError(string contesto, Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine(contesto);
            Console.WriteLine(ex.Message);

            Exception inner = ex.InnerException;
            while (inner != null)
            {
                Console.WriteLine("INNER: " + inner.Message);
                inner = inner.InnerException;
            }

            Console.WriteLine("==================================================");
            Console.WriteLine();
        }

        public void WriteFileProcessed(Foto foto, ScanStatistics stats)
        {
            if (_logDettagliato)
            {
                Console.WriteLine(foto.ToString());
                return;
            }

            if ((stats.TotaleFileElaborati % _progressEvery) == 0)
            {
                Console.WriteLine(
                    $"Elaborati: {stats.TotaleFileElaborati} | Inseriti: {stats.TotaleFileInseriti} | Errori file: {stats.TotaleErroriFile} | Errori DB: {stats.TotaleErroriDb}");
            }
        }

        public void WriteFinalStatistics(ScanStatistics stats)
        {
            Console.WriteLine();
            Console.WriteLine("============== STATISTICHE FINALI ==============");
            Console.WriteLine($"Path iniziali                : {stats.TotalePathIniziali}");
            Console.WriteLine($"Cartelle visitate            : {stats.TotaleCartelleVisitate}");
            Console.WriteLine($"File trovati                 : {stats.TotaleFileTrovati}");
            Console.WriteLine($"File estensione valida       : {stats.TotaleFileConEstensioneValida}");
            Console.WriteLine($"File elaborati               : {stats.TotaleFileElaborati}");
            Console.WriteLine($"File inseriti                : {stats.TotaleFileInseriti}");
            Console.WriteLine($"File senza metadati utili    : {stats.TotaleFileSenzaMetadatiUtili}");
            Console.WriteLine($"Errori file                  : {stats.TotaleErroriFile}");
            Console.WriteLine($"Errori cartelle              : {stats.TotaleErroriCartelle}");
            Console.WriteLine($"Errori metadati              : {stats.TotaleErroriMetadati}");
            Console.WriteLine($"Errori hash                  : {stats.TotaleErroriHash}");
            Console.WriteLine($"Errori DB                    : {stats.TotaleErroriDb}");
            Console.WriteLine("===============================================");
            Console.WriteLine();
        }
    }

    public class DatabaseManager : IDisposable
    {
        private readonly string _nomeDb;
        private readonly bool _cancellaDbSeEsiste;

        public SQLiteConnection Connection { get; private set; }

        public DatabaseManager(string nomeDb, bool cancellaDbSeEsiste)
        {
            _nomeDb = nomeDb;
            _cancellaDbSeEsiste = cancellaDbSeEsiste;
        }

        public void Initialize()
        {
            CreateConnection();
            CreateTable();
            CreateIndexes();
        }

        private void CreateConnection()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_nomeDb))
                {
                    throw new ArgumentException("Il nome del database non può essere vuoto.", nameof(_nomeDb));
                }

                if (File.Exists(_nomeDb) && _cancellaDbSeEsiste)
                {
                    File.Delete(_nomeDb);
                }

                Connection = new SQLiteConnection(
                    $"Data Source={_nomeDb};Version=3;New=True;Compress=True;");

                Connection.Open();
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore durante la creazione/apertura del database SQLite '{_nomeDb}'.", ex);
            }
        }

        private void CreateTable()
        {
            EnsureConnectionOpen();

            string createSql =
                "CREATE TABLE IF NOT EXISTS FOTO ("
                + "  ID INTEGER PRIMARY KEY AUTOINCREMENT"
                + ", PERCORSO_COMPLETO TEXT NOT NULL"
                + ", CARTELLA TEXT NOT NULL"
                + ", NOMEFILE TEXT NOT NULL"
                + ", NOMEFILE_NORM TEXT"
                + ", ESTENSIONE TEXT"
                + ", MIME_TYPE TEXT"
                + ", DIMENSIONE INTEGER"
                + ", DATA_FILE_MODIFICA TEXT"
                + ", DATA_SCATTO TEXT"
                + ", LARGHEZZA INTEGER"
                + ", ALTEZZA INTEGER"
                + ", MARCA TEXT"
                + ", MODELLO TEXT"
                + ", ESPOSIZIONE TEXT"
                + ", APERTURA TEXT"
                + ", ISO TEXT"
                + ", COMPENSAZIONE TEXT"
                + ", ZOOM TEXT"
                + ", HASH_SHA256 TEXT"
                + ", CHIAVE_DUP_BINARIO TEXT"
                + ", CHIAVE_DUP_SCATTO TEXT"
                + ", CHIAVE_DUP_PROBABILE TEXT"
                + ", NUOVOFILE TEXT"
                + ", METADATI_PRESENTI INTEGER"
                + ", NOTE_ERRORE TEXT"
                + ")";

            try
            {
                using (SQLiteCommand sqlite_cmd = Connection.CreateCommand())
                {
                    sqlite_cmd.CommandText = createSql;
                    sqlite_cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante la creazione della tabella FOTO.", ex);
            }
        }

        private void CreateIndexes()
        {
            EnsureConnectionOpen();

            string[] indexSql = new string[]
            {
                "CREATE UNIQUE INDEX IF NOT EXISTS UX_FOTO_PERCORSO_COMPLETO ON FOTO(PERCORSO_COMPLETO)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_HASH_SHA256 ON FOTO(HASH_SHA256)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_CHIAVE_DUP_BINARIO ON FOTO(CHIAVE_DUP_BINARIO)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_CHIAVE_DUP_SCATTO ON FOTO(CHIAVE_DUP_SCATTO)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_CHIAVE_DUP_PROBABILE ON FOTO(CHIAVE_DUP_PROBABILE)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_NOMEFILE_NORM ON FOTO(NOMEFILE_NORM)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_DATA_SCATTO ON FOTO(DATA_SCATTO)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_DIMENSIONE ON FOTO(DIMENSIONE)"
            };

            try
            {
                using (SQLiteCommand sqlite_cmd = Connection.CreateCommand())
                {
                    foreach (string sql in indexSql)
                    {
                        sqlite_cmd.CommandText = sql;
                        sqlite_cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante la creazione degli indici.", ex);
            }
        }

        private void EnsureConnectionOpen()
        {
            if (Connection == null)
            {
                throw new InvalidOperationException("La connessione SQLite è nulla.");
            }

            if (Connection.State != System.Data.ConnectionState.Open)
            {
                throw new InvalidOperationException("La connessione SQLite non è aperta.");
            }
        }

        public void Dispose()
        {
            if (Connection != null)
            {
                Connection.Dispose();
                Connection = null;
            }
        }
    }

    public class InsertManager : IDisposable
    {
        private readonly SQLiteConnection _conn;
        private readonly int _batchSize;
        private SQLiteTransaction _transaction;
        private SQLiteCommand _insertCommand;
        private int _currentBatchCount;

        public InsertManager(SQLiteConnection conn, int batchSize)
        {
            if (conn == null)
            {
                throw new ArgumentNullException(nameof(conn));
            }

            if (conn.State != System.Data.ConnectionState.Open)
            {
                throw new InvalidOperationException("La connessione SQLite non è aperta.");
            }

            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Il batch size deve essere maggiore di zero.");
            }

            _conn = conn;
            _batchSize = batchSize;

            StartNewBatch();
        }

        public void Insert(Foto foto)
        {
            if (foto == null)
            {
                throw new ArgumentNullException(nameof(foto));
            }

            try
            {
                _insertCommand.Parameters["@percorso_completo"].Value = Utility.Nz(foto.percorsoCompleto);
                _insertCommand.Parameters["@cartella"].Value = Utility.Nz(foto.cartella);
                _insertCommand.Parameters["@nomefile"].Value = Utility.Nz(foto.nomefile);
                _insertCommand.Parameters["@nomefile_norm"].Value = Utility.Nz(foto.nomefileNorm);
                _insertCommand.Parameters["@estensione"].Value = Utility.Nz(foto.estensione);
                _insertCommand.Parameters["@mime_type"].Value = Utility.Nz(foto.mimeType);
                _insertCommand.Parameters["@dimensione"].Value = foto.dimensione;
                _insertCommand.Parameters["@data_file_modifica"].Value = Utility.Nz(foto.dataFileModifica);
                _insertCommand.Parameters["@data_scatto"].Value = Utility.Nz(foto.dataScatto);
                _insertCommand.Parameters["@larghezza"].Value = foto.larghezza;
                _insertCommand.Parameters["@altezza"].Value = foto.altezza;
                _insertCommand.Parameters["@marca"].Value = Utility.Nz(foto.marca);
                _insertCommand.Parameters["@modello"].Value = Utility.Nz(foto.modello);
                _insertCommand.Parameters["@esposizione"].Value = Utility.Nz(foto.esposizione);
                _insertCommand.Parameters["@apertura"].Value = Utility.Nz(foto.apertura);
                _insertCommand.Parameters["@iso"].Value = Utility.Nz(foto.iso);
                _insertCommand.Parameters["@compensazione"].Value = Utility.Nz(foto.compensazione);
                _insertCommand.Parameters["@zoom"].Value = Utility.Nz(foto.zoom);
                _insertCommand.Parameters["@hash_sha256"].Value = Utility.Nz(foto.hashSha256);
                _insertCommand.Parameters["@chiave_dup_binario"].Value = Utility.Nz(foto.chiaveDupBinario);
                _insertCommand.Parameters["@chiave_dup_scatto"].Value = Utility.Nz(foto.chiaveDupScatto);
                _insertCommand.Parameters["@chiave_dup_probabile"].Value = Utility.Nz(foto.chiaveDupProbabile);
                _insertCommand.Parameters["@nuovofile"].Value = Utility.Nz(foto.nuovofile);
                _insertCommand.Parameters["@metadati_presenti"].Value = foto.metadatiPresenti ? 1 : 0;
                _insertCommand.Parameters["@note_errore"].Value = Utility.Nz(foto.noteErrore);

                _insertCommand.ExecuteNonQuery();
                _currentBatchCount++;

                if (_currentBatchCount >= _batchSize)
                {
                    CommitBatch();
                    StartNewBatch();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Errore durante l'inserimento nel database del file '{foto.percorsoCompleto}'.",
                    ex);
            }
        }

        public void Flush()
        {
            if (_transaction != null)
            {
                CommitBatch();
            }
        }

        private void StartNewBatch()
        {
            _transaction = _conn.BeginTransaction();
            _insertCommand = _conn.CreateCommand();
            _insertCommand.Transaction = _transaction;

            _insertCommand.CommandText =
                "INSERT INTO FOTO ("
                + "  PERCORSO_COMPLETO"
                + ", CARTELLA"
                + ", NOMEFILE"
                + ", NOMEFILE_NORM"
                + ", ESTENSIONE"
                + ", MIME_TYPE"
                + ", DIMENSIONE"
                + ", DATA_FILE_MODIFICA"
                + ", DATA_SCATTO"
                + ", LARGHEZZA"
                + ", ALTEZZA"
                + ", MARCA"
                + ", MODELLO"
                + ", ESPOSIZIONE"
                + ", APERTURA"
                + ", ISO"
                + ", COMPENSAZIONE"
                + ", ZOOM"
                + ", HASH_SHA256"
                + ", CHIAVE_DUP_BINARIO"
                + ", CHIAVE_DUP_SCATTO"
                + ", CHIAVE_DUP_PROBABILE"
                + ", NUOVOFILE"
                + ", METADATI_PRESENTI"
                + ", NOTE_ERRORE"
                + ") "
                + " VALUES ("
                + "  @percorso_completo"
                + ", @cartella"
                + ", @nomefile"
                + ", @nomefile_norm"
                + ", @estensione"
                + ", @mime_type"
                + ", @dimensione"
                + ", @data_file_modifica"
                + ", @data_scatto"
                + ", @larghezza"
                + ", @altezza"
                + ", @marca"
                + ", @modello"
                + ", @esposizione"
                + ", @apertura"
                + ", @iso"
                + ", @compensazione"
                + ", @zoom"
                + ", @hash_sha256"
                + ", @chiave_dup_binario"
                + ", @chiave_dup_scatto"
                + ", @chiave_dup_probabile"
                + ", @nuovofile"
                + ", @metadati_presenti"
                + ", @note_errore"
                + ");";

            _insertCommand.Parameters.Add("@percorso_completo", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@cartella", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@nomefile", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@nomefile_norm", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@estensione", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@mime_type", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@dimensione", System.Data.DbType.Int64);
            _insertCommand.Parameters.Add("@data_file_modifica", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@data_scatto", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@larghezza", System.Data.DbType.Int32);
            _insertCommand.Parameters.Add("@altezza", System.Data.DbType.Int32);
            _insertCommand.Parameters.Add("@marca", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@modello", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@esposizione", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@apertura", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@iso", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@compensazione", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@zoom", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@hash_sha256", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@chiave_dup_binario", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@chiave_dup_scatto", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@chiave_dup_probabile", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@nuovofile", System.Data.DbType.String);
            _insertCommand.Parameters.Add("@metadati_presenti", System.Data.DbType.Int32);
            _insertCommand.Parameters.Add("@note_errore", System.Data.DbType.String);

            _currentBatchCount = 0;
        }

        private void CommitBatch()
        {
            try
            {
                _transaction.Commit();
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante il commit della transazione SQLite.", ex);
            }
            finally
            {
                if (_insertCommand != null)
                {
                    _insertCommand.Dispose();
                    _insertCommand = null;
                }

                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
        }

        public void Dispose()
        {
            if (_transaction != null)
            {
                Flush();
            }

            if (_insertCommand != null)
            {
                _insertCommand.Dispose();
                _insertCommand = null;
            }

            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }
    }

    public class FileScanner
    {
        private readonly AppConfig _config;
        private readonly InsertManager _insertManager;
        private readonly MetadataService _metadataService;
        private readonly HashService _hashService;
        private readonly Logger _logger;
        private readonly ScanStatistics _stats;

        public FileScanner(
            AppConfig config,
            InsertManager insertManager,
            MetadataService metadataService,
            HashService hashService,
            Logger logger,
            ScanStatistics stats)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _insertManager = insertManager ?? throw new ArgumentNullException(nameof(insertManager));
            _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        public void Scan()
        {
            if (_config.Paths == null || _config.Paths.Length == 0)
            {
                throw new ArgumentException("Non è stato specificato alcun path da analizzare.");
            }

            _stats.TotalePathIniziali = _config.Paths.Length;

            foreach (string path in _config.Paths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        _stats.TotaleFileTrovati++;

                        string estensione = Utility.GetEstensione(path);
                        if (_config.EstensioniPermesse.Contains(estensione))
                        {
                            _stats.TotaleFileConEstensioneValida++;
                            ProcessFile(path);
                        }
                    }
                    else if (System.IO.Directory.Exists(path))
                    {
                        ProcessDirectory(path);
                    }
                    else
                    {
                        throw new FileNotFoundException($"Path non valido o non trovato: '{path}'.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteError($"Errore durante l'analisi del path iniziale '{path}'", ex);
                }
            }

            _logger.WriteFinalStatistics(_stats);
        }

        private void ProcessDirectory(string targetDirectory)
        {
            try
            {
                _stats.TotaleCartelleVisitate++;

                foreach (string fileName in System.IO.Directory.EnumerateFiles(targetDirectory))
                {
                    try
                    {
                        _stats.TotaleFileTrovati++;

                        string estensione = Utility.GetEstensione(fileName);
                        if (_config.EstensioniPermesse.Contains(estensione))
                        {
                            _stats.TotaleFileConEstensioneValida++;
                            ProcessFile(fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _stats.TotaleErroriFile++;
                        _logger.WriteError($"Errore durante la lavorazione del file '{fileName}'", ex);
                    }
                }

                foreach (string subdirectory in System.IO.Directory.EnumerateDirectories(targetDirectory))
                {
                    ProcessDirectory(subdirectory);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _stats.TotaleErroriCartelle++;
                _logger.WriteError($"Accesso negato alla cartella '{targetDirectory}'", ex);
            }
            catch (PathTooLongException ex)
            {
                _stats.TotaleErroriCartelle++;
                _logger.WriteError($"Percorso troppo lungo '{targetDirectory}'", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                _stats.TotaleErroriCartelle++;
                _logger.WriteError($"Cartella non trovata '{targetDirectory}'", ex);
            }
            catch (Exception ex)
            {
                _stats.TotaleErroriCartelle++;
                _logger.WriteError($"Errore generico nella cartella '{targetDirectory}'", ex);
            }
        }

        private void ProcessFile(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentException("Il path del file è vuoto.", nameof(imagePath));
            }

            Foto foto = new Foto();

            try
            {
                _stats.TotaleFileElaborati++;

                FileInfo fInfo = new FileInfo(imagePath);

                if (!fInfo.Exists)
                {
                    throw new FileNotFoundException($"File non trovato: '{imagePath}'.");
                }

                foto.percorsoCompleto = imagePath;
                foto.cartella = fInfo.DirectoryName ?? "";
                foto.nomefile = fInfo.Name;
                foto.nomefileNorm = Utility.NormalizeFileNameWithoutExtension(fInfo.Name);
                foto.estensione = Utility.GetEstensione(imagePath);
                foto.dimensione = fInfo.Length;
                foto.dataFileModifica = fInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                foto.dataScatto = "";
                foto.larghezza = 0;
                foto.altezza = 0;
                foto.marca = "";
                foto.modello = "";
                foto.esposizione = "";
                foto.apertura = "";
                foto.iso = "";
                foto.compensazione = "";
                foto.zoom = "";
                foto.mimeType = "";
                foto.hashSha256 = "";
                foto.nuovofile = "";
                foto.metadatiPresenti = false;
                foto.noteErrore = "";

                bool metadatiLetti = _metadataService.TryPopulateMetadata(imagePath, foto, _logger, _stats);
                if (!metadatiLetti)
                {
                    _stats.TotaleFileSenzaMetadatiUtili++;
                }

                bool hashCalcolato = _hashService.TryCalculateHash(imagePath, foto, _logger, _stats);
                if (!hashCalcolato)
                {
                    // l'errore è già stato registrato
                }

                foto.CalcolaChiaviDerivate();

                try
                {
                    _insertManager.Insert(foto);
                    _stats.TotaleFileInseriti++;
                }
                catch (Exception ex)
                {
                    _stats.TotaleErroriDb++;
                    throw new Exception($"Errore DB sul file '{imagePath}'.", ex);
                }

                _logger.WriteFileProcessed(foto, _stats);
            }
            catch (UnauthorizedAccessException ex)
            {
                _stats.TotaleErroriFile++;
                _logger.WriteError($"Accesso negato al file '{imagePath}'", ex);
            }
            catch (PathTooLongException ex)
            {
                _stats.TotaleErroriFile++;
                _logger.WriteError($"Percorso troppo lungo per il file '{imagePath}'", ex);
            }
            catch (IOException ex)
            {
                _stats.TotaleErroriFile++;
                _logger.WriteError($"Errore I/O sul file '{imagePath}'", ex);
            }
            catch (Exception ex)
            {
                _stats.TotaleErroriFile++;
                _logger.WriteError($"Errore non gestito sul file '{imagePath}'", ex);
            }
        }
    }

    public class MetadataService
    {
        public bool TryPopulateMetadata(string imagePath, Foto foto, Logger logger, ScanStatistics stats)
        {
            bool trovatoAlmenoUnMetadato = false;

            try
            {
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(imagePath);

                string tagImageWidth = MetadataLookup.FindFirstDescription(
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

                string tagImageHeight = MetadataLookup.FindFirstDescription(
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

                string tagFileName = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("File", "File Name"),
                    new TagSearch(null, "File Name")
                );

                string tagFileSize = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("File", "File Size"),
                    new TagSearch(null, "File Size")
                );

                string tagMimeType = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("File Type", "Detected MIME Type"),
                    new TagSearch(null, "Detected MIME Type"),
                    new TagSearch(null, "MIME Type")
                );

                string tagMake = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif IFD0", "Make"),
                    new TagSearch(null, "Make")
                );

                string tagModel = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif IFD0", "Model"),
                    new TagSearch(null, "Model")
                );

                string tagExposureTime = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif SubIFD", "Exposure Time"),
                    new TagSearch(null, "Exposure Time")
                );

                string tagFNumber = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif SubIFD", "F-Number"),
                    new TagSearch(null, "F-Number"),
                    new TagSearch(null, "Aperture")
                );

                string tagISOSpeedRatings = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif SubIFD", "ISO Speed Ratings"),
                    new TagSearch(null, "ISO Speed Ratings"),
                    new TagSearch(null, "Photographic Sensitivity"),
                    new TagSearch(null, "ISO")
                );

                string tagDateTimeOriginal = MetadataLookup.FindFirstDescription(
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

                string tagExposureBiasValue = MetadataLookup.FindFirstDescription(
                    directories,
                    new TagSearch("Exif SubIFD", "Exposure Bias Value"),
                    new TagSearch(null, "Exposure Bias Value")
                );

                string tagFocalLength = MetadataLookup.FindFirstDescription(
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
                foto.dimensione = imageSize;
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

    public class HashService
    {
        public bool TryCalculateHash(string imagePath, Foto foto, Logger logger, ScanStatistics stats)
        {
            try
            {
                foto.hashSha256 = BytesToString(GetHashSha256(imagePath));
                return true;
            }
            catch (Exception ex)
            {
                stats.TotaleErroriHash++;
                foto.hashSha256 = "";
                foto.noteErrore = Utility.AppendNote(foto.noteErrore, "Errore hash: " + ex.Message, 1000);
                logger.WriteError($"Errore durante il calcolo hash del file '{imagePath}'", ex);
                return false;
            }
        }

        private byte[] GetHashSha256(string filename)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(filename))
            {
                return sha256.ComputeHash(stream);
            }
        }

        private string BytesToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }

    public class Foto
    {
        public string percorsoCompleto { get; set; } = "";
        public string cartella { get; set; } = "";
        public string nomefile { get; set; } = "";
        public string nomefileNorm { get; set; } = "";
        public string estensione { get; set; } = "";
        public string mimeType { get; set; } = "";
        public long dimensione { get; set; } = 0;
        public string dataFileModifica { get; set; } = "";
        public string dataScatto { get; set; } = "";
        public int larghezza { get; set; } = 0;
        public int altezza { get; set; } = 0;
        public string marca { get; set; } = "";
        public string modello { get; set; } = "";
        public string esposizione { get; set; } = "";
        public string apertura { get; set; } = "";
        public string iso { get; set; } = "";
        public string compensazione { get; set; } = "";
        public string zoom { get; set; } = "";
        public string hashSha256 { get; set; } = "";
        public string chiaveDupBinario { get; set; } = "";
        public string chiaveDupScatto { get; set; } = "";
        public string chiaveDupProbabile { get; set; } = "";
        public string nuovofile { get; set; } = "";
        public bool metadatiPresenti { get; set; } = false;
        public string noteErrore { get; set; } = "";

        public override string ToString()
        {
            return $"File: {percorsoCompleto} | {dimensione} bytes";
        }

        public void CalcolaChiaviDerivate()
        {
            nuovofile = CalcNuovofile();

            chiaveDupBinario = hashSha256;

            string camera = Utility.SafeKeyPart(marca) + "|" + Utility.SafeKeyPart(modello);
            string dimensioni = larghezza.ToString() + "x" + altezza.ToString();

            string dataScattoNormalizzata = Utility.NormalizeKeyDate(dataScatto);
            string dataScattoMinuto = Utility.NormalizeKeyDateMinute(dataScatto);

            if (!string.IsNullOrWhiteSpace(dataScattoNormalizzata) && larghezza > 0 && altezza > 0)
            {
                chiaveDupScatto = $"{dataScattoNormalizzata}|{dimensioni}|{camera}";
            }
            else
            {
                chiaveDupScatto = "";
            }

            string dataRiferimentoProbabile = !string.IsNullOrWhiteSpace(dataScattoMinuto)
                ? dataScattoMinuto
                : Utility.NormalizeKeyDateMinute(dataFileModifica);

            if (!string.IsNullOrWhiteSpace(dataRiferimentoProbabile) && larghezza > 0 && altezza > 0)
            {
                chiaveDupProbabile = $"{dataRiferimentoProbabile}|{dimensioni}|{camera}";
            }
            else
            {
                chiaveDupProbabile = "";
            }
        }

        public string CalcNuovofile()
        {
            string dataBase = !string.IsNullOrWhiteSpace(dataScatto) ? dataScatto : dataFileModifica;
            string p1;

            DateTime dateValue;
            if (DateTime.TryParseExact(
                dataBase,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dateValue))
            {
                p1 = dateValue.ToString("yyyy_MM_dd_HH_mm_ss");
            }
            else
            {
                p1 = "0000_01_01_00_00_00";
            }

            return $"{p1}.{this.estensione}";
        }
    }

    public class TagSearch
    {
        public string DirectoryName { get; }
        public string TagName { get; }

        public TagSearch(string directoryName, string tagName)
        {
            DirectoryName = directoryName;
            TagName = tagName;
        }
    }

    public static class MetadataLookup
    {
        public static string FindFirstDescription(IEnumerable<MetadataExtractor.Directory> directories, params TagSearch[] searches)
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

                string description = FindDescription(directories, search.DirectoryName, search.TagName);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    return description;
                }
            }

            return null;
        }

        private static string FindDescription(IEnumerable<MetadataExtractor.Directory> directories, string directoryName, string tagName)
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

    public static class Utility
    {
        public static string Nz(string value)
        {
            return value ?? "";
        }

        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        public static string AppendNote(string currentValue, string newValue, int maxLength)
        {
            string merged;

            if (string.IsNullOrWhiteSpace(currentValue))
            {
                merged = newValue ?? "";
            }
            else if (string.IsNullOrWhiteSpace(newValue))
            {
                merged = currentValue;
            }
            else
            {
                merged = currentValue + " | " + newValue;
            }

            return Truncate(merged, maxLength);
        }

        public static int ConvertiString2Int(string s, string prefisso = "", string postfisso = "", int def = 0)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return def;
            }

            string s1 = TogliPrefisso(s, prefisso);
            string s2 = TogliPostfisso(s1, postfisso);
            string s3 = s2.Trim();

            int ris;
            if (int.TryParse(s3, out ris))
            {
                return ris;
            }

            return def;
        }

        public static long ConvertiString2Long(string s, string prefisso = "", string postfisso = "", long def = 0)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return def;
            }

            string s1 = TogliPrefisso(s, prefisso);
            string s2 = TogliPostfisso(s1, postfisso);
            string s3 = s2.Trim();

            long ris;
            if (long.TryParse(s3, out ris))
            {
                return ris;
            }

            return def;
        }

        public static string TogliPrefisso(string s, string prefisso)
        {
            if (string.IsNullOrEmpty(prefisso))
            {
                return s;
            }

            if (s.StartsWith(prefisso, StringComparison.OrdinalIgnoreCase))
            {
                return s.Substring(prefisso.Length);
            }

            return s;
        }

        public static string TogliPostfisso(string s, string postfisso)
        {
            if (string.IsNullOrEmpty(postfisso))
            {
                return s;
            }

            if (s.EndsWith(postfisso, StringComparison.OrdinalIgnoreCase))
            {
                return s.Substring(0, s.Length - postfisso.Length);
            }

            return s;
        }

        public static string GetEstensione(string path)
        {
            string ext = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(ext))
            {
                return "";
            }

            if (ext.StartsWith("."))
            {
                ext = ext.Substring(1);
            }

            return ext.ToLowerInvariant();
        }

        public static string NormalizeMetadataDate(string rawDate, string fallbackDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate))
            {
                return fallbackDate;
            }

            string[] formats = new string[]
            {
                "yyyy:MM:dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy:MM:dd HH:mm:sszzz",
                "yyyy-MM-dd HH:mm:sszzz"
            };

            DateTime dateValue;
            if (DateTime.TryParseExact(
                rawDate,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out dateValue))
            {
                return dateValue.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateValue))
            {
                return dateValue.ToString("yyyy-MM-dd HH:mm:ss");
            }

            return fallbackDate;
        }

        public static string NormalizeFileNameWithoutExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "";
            }

            string baseName = Path.GetFileNameWithoutExtension(fileName).Trim().ToLowerInvariant();

            baseName = baseName.Replace("_", " ");
            baseName = baseName.Replace("-", " ");

            while (baseName.Contains("  "))
            {
                baseName = baseName.Replace("  ", " ");
            }

            string[] suffixes = new string[]
            {
                " copia",
                " copy",
                " copie"
            };

            foreach (string suffix in suffixes)
            {
                if (baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    baseName = baseName.Substring(0, baseName.Length - suffix.Length).Trim();
                }
            }

            if (baseName.EndsWith(")"))
            {
                int idx = baseName.LastIndexOf('(');
                if (idx > 0)
                {
                    string inside = baseName.Substring(idx + 1, baseName.Length - idx - 2);
                    int n;
                    if (int.TryParse(inside, out n))
                    {
                        baseName = baseName.Substring(0, idx).Trim();
                    }
                }
            }

            return baseName;
        }

        public static string NormalizeKeyDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            DateTime dt;
            if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dt))
            {
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            return "";
        }

        public static string NormalizeKeyDateMinute(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            DateTime dt;
            if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dt))
            {
                return dt.ToString("yyyy-MM-dd HH:mm");
            }

            return "";
        }

        public static string SafeKeyPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            return value.Trim().ToLowerInvariant();
        }
    }
}