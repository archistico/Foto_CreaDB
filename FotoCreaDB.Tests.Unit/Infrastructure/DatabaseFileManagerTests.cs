using System.IO;
using System.Collections.Generic;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.Infrastructure
{
    public class DatabaseFileManagerTests
    {
        [Fact]
        public void DeleteDatabaseFiles_RemovesDbAndJournalFiles()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FotoCreaDB_DbTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                string db = Path.Combine(temp, "test.db");
                string wal = db + "-wal";
                string shm = db + "-shm";
                string journal = db + "-journal";

                File.WriteAllText(db, "x");
                File.WriteAllText(wal, "w");
                File.WriteAllText(shm, "s");
                File.WriteAllText(journal, "j");

                List<string> logs = new List<string>();
                void LogCollector(ServiceLogMessage msg)
                {
                    logs.Add(msg.Message);
                }

                bool result = DatabaseFileManager.DeleteDatabaseFiles(db, LogCollector);

                Assert.True(result);
                Assert.False(File.Exists(db));
                Assert.False(File.Exists(wal));
                Assert.False(File.Exists(shm));
                Assert.False(File.Exists(journal));

                // check that at least one info message about deletion was logged
                Assert.Contains(logs, m => m.StartsWith("Database file cancellato"));
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }

        [Fact]
        public void DeleteDatabaseFiles_NoFiles_LogsWarning()
        {
            string temp = Path.Combine(Path.GetTempPath(), "FotoCreaDB_DbTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                string db = Path.Combine(temp, "nonexistent.db");

                List<ServiceLogMessage> collected = new List<ServiceLogMessage>();
                void Collector(ServiceLogMessage msg)
                {
                    collected.Add(msg);
                }

                bool result = DatabaseFileManager.DeleteDatabaseFiles(db, Collector);

                Assert.False(result);
                Assert.Contains(collected, m => m.Message.Contains("Nessun file di database trovato"));
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }
    }
}
