using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite;

namespace Foto_CreaDB2
{
    public class FotoRepository : IDisposable
    {
        private readonly SQLiteConnection _conn;

        public FotoRepository(SQLiteConnection conn)
        {
            _conn = conn ?? throw new ArgumentNullException(nameof(conn));

            if (_conn.State != System.Data.ConnectionState.Open)
            {
                throw new InvalidOperationException("La connessione SQLite non è aperta.");
            }
        }

        public List<DuplicateBinaryDecision> GetBinaryDuplicateDecisions()
        {
            List<DuplicateBinaryCandidate> allFiles = GetAllExistingFilesForBinaryDuplicateAnalysis();

            DuplicateBinaryService service = new DuplicateBinaryService();
            return service.BuildDecisions(allFiles);
        }

        public List<DuplicateBinaryCandidate> GetAllExistingFilesForBinaryDuplicateAnalysis()
        {
            List<DuplicateBinaryCandidate> result = new List<DuplicateBinaryCandidate>();

            using (SQLiteCommand cmd = _conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT "
                    + "  ID"
                    + ", PERCORSO_COMPLETO"
                    + ", HASH_SHA256"
                    + ", DIMENSIONE"
                    + ", DATA_FILE_MODIFICA"
                    + ", IFNULL(DATA_SCATTO, '') AS DATA_SCATTO "
                    + "FROM FOTO "
                    + "WHERE FILE_ESISTE = 1 "
                    + "  AND IFNULL(HASH_SHA256, '') <> ''";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DuplicateBinaryCandidate item = new DuplicateBinaryCandidate
                        {
                            Id = Convert.ToInt64(reader["ID"]),
                            PercorsoCompleto = Convert.ToString(reader["PERCORSO_COMPLETO"]) ?? "",
                            HashSha256 = Convert.ToString(reader["HASH_SHA256"]) ?? "",
                            Dimensione = reader["DIMENSIONE"] == DBNull.Value ? 0 : Convert.ToInt64(reader["DIMENSIONE"]),
                            DataFileModifica = Convert.ToString(reader["DATA_FILE_MODIFICA"]) ?? "",
                            DataScatto = Convert.ToString(reader["DATA_SCATTO"]) ?? ""
                        };

                        result.Add(item);
                    }
                }
            }

            return result;
        }

        public ExistingFotoInfo? GetByPercorso(string percorsoCompleto)
        {
            using (SQLiteCommand cmd = _conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT "
                    + "  ID"
                    + ", PERCORSO_COMPLETO"
                    + ", DATA_FILE_MODIFICA"
                    + ", IFNULL(DATA_SCATTO, '') AS DATA_SCATTO"
                    + ", DIMENSIONE"
                    + ", HASH_SHA256"
                    + ", FILE_ESISTE"
                    + ", IFNULL(DATA_SCANSIONE, '') AS DATA_SCANSIONE "
                    + "FROM FOTO "
                    + "WHERE PERCORSO_COMPLETO = @percorso";

                cmd.Parameters.AddWithValue("@percorso", percorsoCompleto);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    ExistingFotoInfo info = new ExistingFotoInfo
                    {
                        Id = Convert.ToInt64(reader["ID"]),
                        PercorsoCompleto = Convert.ToString(reader["PERCORSO_COMPLETO"]) ?? "",
                        DataFileModifica = Convert.ToString(reader["DATA_FILE_MODIFICA"]) ?? "",
                        DataScatto = Convert.ToString(reader["DATA_SCATTO"]) ?? "",
                        Dimensione = reader["DIMENSIONE"] == DBNull.Value ? 0 : Convert.ToInt64(reader["DIMENSIONE"]),
                        HashSha256 = Convert.ToString(reader["HASH_SHA256"]) ?? "",
                        FileEsiste = reader["FILE_ESISTE"] != DBNull.Value && Convert.ToInt32(reader["FILE_ESISTE"]) == 1,
                        DataScansione = Convert.ToString(reader["DATA_SCANSIONE"]) ?? ""
                    };

                    return info;
                }
            }
        }

        public void Insert(Foto foto)
        {
            using (SQLiteCommand cmd = CreateInsertCommand())
            {
                FillFotoParameters(cmd, foto);
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(Foto foto)
        {
            using (SQLiteCommand cmd = CreateUpdateCommand())
            {
                FillFotoParameters(cmd, foto);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteById(long id)
        {
            using (SQLiteCommand cmd = _conn.CreateCommand())
            {
                cmd.CommandText =
                    "DELETE FROM FOTO "
                    + "WHERE ID = @id";

                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }
        }

        public void TouchAsSeenWithoutRehash(string percorsoCompleto, string dataScansione)
        {
            using (SQLiteCommand cmd = _conn.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE FOTO SET "
                    + "  FILE_ESISTE = 1"
                    + ", DATA_SCANSIONE = @data_scansione "
                    + "WHERE PERCORSO_COMPLETO = @percorso";

                cmd.Parameters.AddWithValue("@data_scansione", Utility.Nz(dataScansione));
                cmd.Parameters.AddWithValue("@percorso", Utility.Nz(percorsoCompleto));

                cmd.ExecuteNonQuery();
            }
        }

        public int MarkMissingFilesForRoot(string rootPath, string currentScanToken)
        {
            string normalizedRoot = NormalizeRootPath(rootPath);

            using (SQLiteCommand cmd = _conn.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE FOTO SET "
                    + "  FILE_ESISTE = 0 "
                    + "WHERE PERCORSO_COMPLETO LIKE @rootPrefix "
                    + "  AND IFNULL(DATA_SCANSIONE, '') <> @data_scansione "
                    + "  AND FILE_ESISTE <> 0";

                cmd.Parameters.AddWithValue("@rootPrefix", normalizedRoot + "%");
                cmd.Parameters.AddWithValue("@data_scansione", Utility.Nz(currentScanToken));

                return cmd.ExecuteNonQuery();
            }
        }

        private string NormalizeRootPath(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return "";
            }

            string fullPath = Path.GetFullPath(rootPath).Trim();

            if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                fullPath += Path.DirectorySeparatorChar;
            }

            return fullPath;
        }

        private SQLiteCommand CreateInsertCommand()
        {
            SQLiteCommand cmd = _conn.CreateCommand();

            cmd.CommandText =
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
                + ", DATA_SCANSIONE"
                + ", FILE_ESISTE"
                + ") VALUES ("
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
                + ", @data_scansione"
                + ", @file_esiste"
                + ")";

            AddFotoParameters(cmd);
            return cmd;
        }

        private SQLiteCommand CreateUpdateCommand()
        {
            SQLiteCommand cmd = _conn.CreateCommand();

            cmd.CommandText =
                "UPDATE FOTO SET "
                + "  CARTELLA = @cartella"
                + ", NOMEFILE = @nomefile"
                + ", NOMEFILE_NORM = @nomefile_norm"
                + ", ESTENSIONE = @estensione"
                + ", MIME_TYPE = @mime_type"
                + ", DIMENSIONE = @dimensione"
                + ", DATA_FILE_MODIFICA = @data_file_modifica"
                + ", DATA_SCATTO = @data_scatto"
                + ", LARGHEZZA = @larghezza"
                + ", ALTEZZA = @altezza"
                + ", MARCA = @marca"
                + ", MODELLO = @modello"
                + ", ESPOSIZIONE = @esposizione"
                + ", APERTURA = @apertura"
                + ", ISO = @iso"
                + ", COMPENSAZIONE = @compensazione"
                + ", ZOOM = @zoom"
                + ", HASH_SHA256 = @hash_sha256"
                + ", CHIAVE_DUP_BINARIO = @chiave_dup_binario"
                + ", CHIAVE_DUP_SCATTO = @chiave_dup_scatto"
                + ", CHIAVE_DUP_PROBABILE = @chiave_dup_probabile"
                + ", NUOVOFILE = @nuovofile"
                + ", METADATI_PRESENTI = @metadati_presenti"
                + ", NOTE_ERRORE = @note_errore"
                + ", DATA_SCANSIONE = @data_scansione"
                + ", FILE_ESISTE = @file_esiste "
                + "WHERE PERCORSO_COMPLETO = @percorso_completo";

            AddFotoParameters(cmd);
            return cmd;
        }

        private void AddFotoParameters(SQLiteCommand cmd)
        {
            cmd.Parameters.Add("@percorso_completo", System.Data.DbType.String);
            cmd.Parameters.Add("@cartella", System.Data.DbType.String);
            cmd.Parameters.Add("@nomefile", System.Data.DbType.String);
            cmd.Parameters.Add("@nomefile_norm", System.Data.DbType.String);
            cmd.Parameters.Add("@estensione", System.Data.DbType.String);
            cmd.Parameters.Add("@mime_type", System.Data.DbType.String);
            cmd.Parameters.Add("@dimensione", System.Data.DbType.Int64);
            cmd.Parameters.Add("@data_file_modifica", System.Data.DbType.String);
            cmd.Parameters.Add("@data_scatto", System.Data.DbType.String);
            cmd.Parameters.Add("@larghezza", System.Data.DbType.Int32);
            cmd.Parameters.Add("@altezza", System.Data.DbType.Int32);
            cmd.Parameters.Add("@marca", System.Data.DbType.String);
            cmd.Parameters.Add("@modello", System.Data.DbType.String);
            cmd.Parameters.Add("@esposizione", System.Data.DbType.String);
            cmd.Parameters.Add("@apertura", System.Data.DbType.String);
            cmd.Parameters.Add("@iso", System.Data.DbType.String);
            cmd.Parameters.Add("@compensazione", System.Data.DbType.String);
            cmd.Parameters.Add("@zoom", System.Data.DbType.String);
            cmd.Parameters.Add("@hash_sha256", System.Data.DbType.String);
            cmd.Parameters.Add("@chiave_dup_binario", System.Data.DbType.String);
            cmd.Parameters.Add("@chiave_dup_scatto", System.Data.DbType.String);
            cmd.Parameters.Add("@chiave_dup_probabile", System.Data.DbType.String);
            cmd.Parameters.Add("@nuovofile", System.Data.DbType.String);
            cmd.Parameters.Add("@metadati_presenti", System.Data.DbType.Int32);
            cmd.Parameters.Add("@note_errore", System.Data.DbType.String);
            cmd.Parameters.Add("@data_scansione", System.Data.DbType.String);
            cmd.Parameters.Add("@file_esiste", System.Data.DbType.Int32);
        }

        private void FillFotoParameters(SQLiteCommand cmd, Foto foto)
        {
            cmd.Parameters["@percorso_completo"].Value = Utility.Nz(foto.percorsoCompleto);
            cmd.Parameters["@cartella"].Value = Utility.Nz(foto.cartella);
            cmd.Parameters["@nomefile"].Value = Utility.Nz(foto.nomefile);
            cmd.Parameters["@nomefile_norm"].Value = Utility.Nz(foto.nomefileNorm);
            cmd.Parameters["@estensione"].Value = Utility.Nz(foto.estensione);
            cmd.Parameters["@mime_type"].Value = Utility.Nz(foto.mimeType);
            cmd.Parameters["@dimensione"].Value = foto.dimensione;
            cmd.Parameters["@data_file_modifica"].Value = Utility.Nz(foto.dataFileModifica);
            cmd.Parameters["@data_scatto"].Value = Utility.Nz(foto.dataScatto);
            cmd.Parameters["@larghezza"].Value = foto.larghezza;
            cmd.Parameters["@altezza"].Value = foto.altezza;
            cmd.Parameters["@marca"].Value = Utility.Nz(foto.marca);
            cmd.Parameters["@modello"].Value = Utility.Nz(foto.modello);
            cmd.Parameters["@esposizione"].Value = Utility.Nz(foto.esposizione);
            cmd.Parameters["@apertura"].Value = Utility.Nz(foto.apertura);
            cmd.Parameters["@iso"].Value = Utility.Nz(foto.iso);
            cmd.Parameters["@compensazione"].Value = Utility.Nz(foto.compensazione);
            cmd.Parameters["@zoom"].Value = Utility.Nz(foto.zoom);
            cmd.Parameters["@hash_sha256"].Value = Utility.Nz(foto.hashSha256);
            cmd.Parameters["@chiave_dup_binario"].Value = Utility.Nz(foto.chiaveDupBinario);
            cmd.Parameters["@chiave_dup_scatto"].Value = Utility.Nz(foto.chiaveDupScatto);
            cmd.Parameters["@chiave_dup_probabile"].Value = Utility.Nz(foto.chiaveDupProbabile);
            cmd.Parameters["@nuovofile"].Value = Utility.Nz(foto.nuovofile);
            cmd.Parameters["@metadati_presenti"].Value = foto.metadatiPresenti ? 1 : 0;
            cmd.Parameters["@note_errore"].Value = Utility.Nz(foto.noteErrore);
            cmd.Parameters["@data_scansione"].Value = Utility.Nz(foto.dataScansione);
            cmd.Parameters["@file_esiste"].Value = foto.fileEsiste ? 1 : 0;
        }

        public void Dispose()
        {
        }
    }
}