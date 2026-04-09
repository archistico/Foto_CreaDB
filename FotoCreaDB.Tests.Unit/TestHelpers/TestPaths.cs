using System;
using System.IO;

namespace FotoCreaDB.Tests.Unit.TestHelpers
{
    public static class TestPaths
    {
        public static string CreateTemporaryDirectory()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "FotoCreaDB_Tests_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(path);
            return path;
        }
    }
}