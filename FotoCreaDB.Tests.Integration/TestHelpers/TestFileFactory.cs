using System.IO;
using System.Text;

namespace FotoCreaDB.Tests.Integration.TestHelpers
{
    public static class TestFileFactory
    {
        public static void CreateTextFile(string path, string content)
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        public static void CreateBinaryFile(string path, byte[] content)
        {
            File.WriteAllBytes(path, content);
        }

        public static void CopyFile(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath, true);
        }
    }
}