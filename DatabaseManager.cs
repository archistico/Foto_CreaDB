using System;
using System.IO;
using System.Data.SQLite;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Gestisce la creazione, l'apertura e la preparazione del database SQLite dell'applicazione.
    /// Si occupa dell'inizializzazione della connessione, della tabella principale e degli indici.
    /// </summary>
    public class DatabaseManager : IDisposable
    {
        private readonly string _nomeDb;
        private readonly bool _cancellaDbSeEsiste;

        /// <summary>
        /// Connessione SQLite attualmente aperta e disponibile per le operazioni sul database.
        /// </summary>
        public SQLiteConnection? Connection { get; private set; }

        /// <summary>
        /// Inizializza una nuova istanza del gestore del database.
        /// </summary>
        /// <param name="nomeDb">
        /// Nome o percorso del file SQLite da creare o aprire.
        /// </param>
        /// <param name="cancellaDbSeEsiste">
        /// Indica se un database già esistente deve essere eliminato prima dell'apertura.
        /// </param>
        public DatabaseManager(string nomeDb, bool cancellaDbSeEsiste)
        {
            _nomeDb = nomeDb;
            _cancellaDbSeEsiste = cancellaDbSeEsiste;
        }

        /// <summary>
        /// Inizializza completamente il database:
        /// apre la connessione, crea la tabella se assente, aggiorna lo schema e crea gli indici.
        /// </summary>
        public void Initialize()
        {
            CreateConnection();
            CreateTableIfMissing();
            EnsureSchema();
            CreateIndexes();
        }

        /// <summary>
        /// Crea e apre la connessione SQLite, preparando anche la cartella del database se necessario.
        /// </summary>
        /// <exception cref="Exception">
        /// Generata se si verifica un errore durante la creazione o l'apertura del database.
        /// </exception>
        private void CreateConnection()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_nomeDb))
                {
                    throw new ArgumentException("Il nome del database non può essere vuoto.", nameof(_nomeDb));
                }

                string? directoryName = Path.GetDirectoryName(Path.GetFullPath(_nomeDb));
                if (!string.IsNullOrWhiteSpace(directoryName) && !System.IO.Directory.Exists(directoryName))
                {
                    System.IO.Directory.CreateDirectory(directoryName);
                }

                if (File.Exists(_nomeDb) && _cancellaDbSeEsiste)
                {
                    File.Delete(_nomeDb);
                }

                Connection = new SQLiteConnection(
                    $"Data Source={_nomeDb};Version=3;Foreign Keys=True;");

                Connection.Open();
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore durante la creazione/apertura del database SQLite '{_nomeDb}'.", ex);
            }
        }

        /// <summary>
        /// Crea la tabella principale <c>FOTO</c> se non è già presente nel database.
        /// </summary>
        /// <exception cref="Exception">
        /// Generata se la creazione della tabella fallisce.
        /// </exception>
        private void CreateTableIfMissing()
        {
            SQLiteConnection connection = GetOpenConnection();

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
                + ", DATA_SCANSIONE TEXT"
                + ", FILE_ESISTE INTEGER NOT NULL DEFAULT 1"
                + ")";

            try
            {
                using (SQLiteCommand sqlite_cmd = connection.CreateCommand())
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

        /// <summary>
        /// Verifica che lo schema contenga le colonne necessarie introdotte nelle versioni più recenti
        /// e le aggiunge se mancanti.
        /// </summary>
        private void EnsureSchema()
        {
            SQLiteConnection connection = GetOpenConnection();

            EnsureColumnExists("FOTO", "DATA_SCANSIONE", "TEXT");
            EnsureColumnExists("FOTO", "FILE_ESISTE", "INTEGER NOT NULL DEFAULT 1");
        }

        /// <summary>
        /// Aggiunge una colonna a una tabella se la colonna non esiste già.
        /// </summary>
        /// <param name="tableName">
        /// Nome della tabella da aggiornare.
        /// </param>
        /// <param name="columnName">
        /// Nome della colonna da verificare o creare.
        /// </param>
        /// <param name="columnDefinition">
        /// Definizione SQL della colonna da aggiungere.
        /// </param>
        private void EnsureColumnExists(string tableName, string columnName, string columnDefinition)
        {
            SQLiteConnection connection = GetOpenConnection();

            if (ColumnExists(tableName, columnName))
            {
                return;
            }

            string sql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Verifica se una colonna è già presente nello schema della tabella indicata.
        /// </summary>
        /// <param name="tableName">
        /// Nome della tabella da controllare.
        /// </param>
        /// <param name="columnName">
        /// Nome della colonna da cercare.
        /// </param>
        /// <returns>
        /// <c>true</c> se la colonna esiste; altrimenti <c>false</c>.
        /// </returns>
        private bool ColumnExists(string tableName, string columnName)
        {
            SQLiteConnection connection = GetOpenConnection();

            using (SQLiteCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info({tableName})";
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string currentColumnName = Convert.ToString(reader["name"]) ?? "";
                        if (string.Equals(currentColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Crea gli indici principali usati per velocizzare ricerche, confronti e deduplicazione.
        /// </summary>
        /// <exception cref="Exception">
        /// Generata se la creazione di uno o più indici fallisce.
        /// </exception>
        private void CreateIndexes()
        {
            SQLiteConnection connection = GetOpenConnection();

            string[] indexSql = new string[]
            {
                "CREATE UNIQUE INDEX IF NOT EXISTS UX_FOTO_PERCORSO_COMPLETO ON FOTO(PERCORSO_COMPLETO)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_HASH_SHA256 ON FOTO(HASH_SHA256)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_CHIAVE_DUP_BINARIO ON FOTO(CHIAVE_DUP_BINARIO)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_CHIAVE_DUP_SCATTO ON FOTO(CHIAVE_DUP_SCATTO)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_CHIAVE_DUP_PROBABILE ON FOTO(CHIAVE_DUP_PROBABILE)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_NOMEFILE_NORM ON FOTO(NOMEFILE_NORM)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_DATA_SCATTO ON FOTO(DATA_SCATTO)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_DIMENSIONE ON FOTO(DIMENSIONE)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_DATA_SCANSIONE ON FOTO(DATA_SCANSIONE)",
                "CREATE INDEX IF NOT EXISTS IDX_FOTO_FILE_ESISTE ON FOTO(FILE_ESISTE)"
            };

            try
            {
                using (SQLiteCommand sqlite_cmd = connection.CreateCommand())
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

        /// <summary>
        /// Restituisce la connessione SQLite aperta e pronta all'uso.
        /// </summary>
        /// <returns>
        /// La connessione SQLite attiva.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Generata se la connessione è nulla o non aperta.
        /// </exception>
        private SQLiteConnection GetOpenConnection()
        {
            if (Connection == null)
            {
                throw new InvalidOperationException("La connessione SQLite è nulla.");
            }

            if (Connection.State != System.Data.ConnectionState.Open)
            {
                throw new InvalidOperationException("La connessione SQLite non è aperta.");
            }

            return Connection;
        }

        /// <summary>
        /// Rilascia le risorse usate dal gestore e chiude la connessione al database.
        /// </summary>
        public void Dispose()
        {
            if (Connection != null)
            {
                Connection.Dispose();
                Connection = null;
            }
        }
    }
}