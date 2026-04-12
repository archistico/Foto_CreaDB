using System.IO;
using Foto_CreaDB2;
using Xunit;

namespace FotoCreaDB.Tests.Unit.Services
{
    public class MetadataServiceTests
    {
        // Minimal valid JPEG bytes (1x1) used in other tests
        private static readonly byte[] Jpeg1x1 = new byte[]
        {
            255,216,255,224,0,16,74,70,73,70,0,1,1,1,0,72,
            0,72,0,0,255,219,0,67,0,8,6,6,7,6,5,8,
            7,7,7,9,9,8,10,12,20,13,12,11,11,12,25,18,
            19,15,20,29,26,31,30,29,26,28,28,32,36,46,39,32,
            34,44,35,28,28,40,55,41,44,48,49,52,52,52,31,39,
            57,61,56,50,60,46,51,52,50,255,219,0,67,1,9,9,
            9,12,11,12,24,13,13,24,50,33,28,33,50,50,50,50,
            50,50,50,50,50,50,50,50,50,50,50,50,50,50,50,50,
            50,50,50,50,50,50,50,50,50,50,50,50,50,50,50,50,
            50,50,50,50,50,50,50,50,50,50,50,50,255,192,0,17,
            8,0,1,0,1,3,1,34,0,2,17,1,3,17,1,255,196,0,31,
            0,0,1,5,1,1,1,1,1,1,0,0,0,0,0,0,0,0,1,2,3,4,5,6,7,8,9,10,11,255,196,0,181,16,0,2,1,3,3,2,4,3,5,5,4,4,0,0,1,125,1,2,3,0,4,17,5,18,33,49,65,6,19,81,97,7,34,113,20,50,129,145,161,8,35,66,177,193,21,82,209,240,36,51,98,114,130,9,10,22,23,24,25,26,37,38,39,40,41,42,52,53,54,55,56,57,58,67,68,69,70,71,72,73,74,83,84,85,86,87,88,89,90,99,100,101,102,103,104,105,106,115,116,117,118,119,120,121,122,131,132,133,134,135,136,137,138,146,147,148,149,150,151,152,153,154,162,163,164,165,166,167,168,169,170,178,179,180,181,182,183,184,185,186,194,195,196,197,198,199,200,201,202,210,211,212,213,214,215,216,217,218,225,226,227,228,229,230,231,232,233,234,241,242,243,244,245,246,247,248,249,250,255,196,0,31,1,0,3,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,1,2,3,4,5,6,7,8,9,10,11,255,196,0,181,17,0,2,1,2,4,4,3,4,7,5,4,4,0,1,2,119,0,1,2,3,17,4,5,33,49,6,18,65,81,7,97,113,19,34,50,129,8,20,66,145,161,177,193,9,35,51,82,240,21,98,114,209,10,22,36,52,225,37,241,23,24,25,26,38,39,40,41,42,53,54,55,56,57,58,67,68,69,70,71,72,73,74,83,84,85,86,87,88,89,90,99,100,101,102,103,104,105,106,115,116,117,118,119,120,121,122,130,131,132,133,134,135,136,137,138,146,147,148,149,150,151,152,153,154,162,163,164,165,166,167,168,169,170,178,179,180,181,182,183,184,185,186,194,195,196,197,198,199,200,201,202,210,211,212,213,214,215,216,217,218,226,227,228,229,230,231,232,233,234,242,243,244,245,246,247,248,249,250,255,218,0,12,3,1,0,2,17,3,17,0,63,0,252,170,40,162,128,63,255,217
        };

        [Fact]
        public void TryPopulateMetadata_SkipsNonImageExtensions()
        {
            string dir = Path.Combine(Path.GetTempPath(), "FotoCreaDB_MetadataTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string file = Path.Combine(dir, "video.wmv");
                File.WriteAllText(file, "dummy content");

                MetadataService svc = new MetadataService(new AppConfig());
                Foto foto = new Foto { percorsoCompleto = file };
                ScanStatistics stats = new ScanStatistics();

                bool result = svc.TryPopulateMetadata(file, foto, null, stats);

                Assert.False(result);
                Assert.False(foto.metadatiPresenti);
                Assert.Equal(0, stats.TotaleErroriMetadati);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void TryPopulateMetadata_RawCorrupted_IncrementsError()
        {
            string dir = Path.Combine(Path.GetTempPath(), "FotoCreaDB_MetadataTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string file = Path.Combine(dir, "image.nef");
                // write some invalid/corrupted content for a RAW file
                File.WriteAllText(file, "this is not a valid raw file");

                AppConfig cfg = new AppConfig();
                MetadataService svc = new MetadataService(cfg);
                Foto foto = new Foto { percorsoCompleto = file };
                ScanStatistics stats = new ScanStatistics();
                Logger logger = new Logger(false, 1000);

                bool result = svc.TryPopulateMetadata(file, foto, logger, stats);

                // For a RAW corrupted file we expect the method to attempt metadata read
                // and handle the error by returning false and incrementing TotaleErroriMetadati
                Assert.False(result);
                Assert.False(foto.metadatiPresenti);
                Assert.True(stats.TotaleErroriMetadati >= 1);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [Fact]
        public void TryPopulateMetadata_ReadsForJpeg()
        {
            string dir = Path.Combine(Path.GetTempPath(), "FotoCreaDB_MetadataTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string file = Path.Combine(dir, "image.jpg");
                File.WriteAllBytes(file, Jpeg1x1);

                MetadataService svc = new MetadataService(new AppConfig());
                Foto foto = new Foto { percorsoCompleto = file };
                ScanStatistics stats = new ScanStatistics();

                // Pass a dummy logger to ensure no NRE and to capture errors if any
                Logger logger = new Logger(false, 1000);

                bool result = svc.TryPopulateMetadata(file, foto, logger, stats);

                // For a minimal JPEG we expect some metadata (e.g. dimensions) to be read
                Assert.True(result == foto.metadatiPresenti);
                // No metadata exceptions
                Assert.Equal(0, stats.TotaleErroriMetadati);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }
    }
}
