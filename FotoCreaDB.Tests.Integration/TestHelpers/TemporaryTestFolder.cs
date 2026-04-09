using System;
using System.IO;

namespace FotoCreaDB.Tests.Integration.TestHelpers
{
    public sealed class TemporaryTestFolder : IDisposable
    {
        public string FolderPath { get; }

        public TemporaryTestFolder()
        {
            FolderPath = Path.Combine(
                Path.GetTempPath(),
                "FotoCreaDB_Integration_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(FolderPath);
        }

        public string GetPath(string fileName)
        {
            return Path.Combine(FolderPath, fileName);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(FolderPath))
                {
                    Directory.Delete(FolderPath, true);
                }
            }
            catch
            {
            }
        }
    }
}