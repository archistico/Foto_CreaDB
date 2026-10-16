using Foto_CreaDB2;

namespace FotoCreaDB.Tests.Unit.Configuration;

[Collection("Configuration filesystem")]
public sealed class ConfigurationSafetyTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "FotoCreaDB_Config_" + Guid.NewGuid().ToString("N"));
    public ConfigurationSafetyTests() => Directory.CreateDirectory(_folder);
    public void Dispose() => Directory.Delete(_folder, true);

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("{\"EstensioniPermesse\":[\"jpg\",]}")]
    public void InvalidConfiguration_IsReportedAndNeverOverwritten(string json)
    {
        string path = Path.Combine(_folder, "settings.json");
        File.WriteAllText(path, json);
        Assert.Throws<InvalidDataException>(() => AppConfigLoader.LoadConfiguration(path));
        Assert.False(PreferencesSaver.SavePreferences(_folder, Path.Combine(_folder, "photos.db"), path));
        Assert.Equal(json, File.ReadAllText(path));
    }

    [Fact]
    public void ConfigurationAndRelativePaths_DoNotDependOnLaunchDirectory()
    {
        string config = Path.Combine(_folder, "settings.json");
        File.WriteAllText(config, "{\"Paths\":[\"photos\"],\"NomeDb\":\"data/photos.db\",\"EstensioniPermesse\":[\".JPG\"]}");
        string launch = Path.Combine(_folder, "launch");
        Directory.CreateDirectory(launch);
        string previous = Directory.GetCurrentDirectory();
        try
        {
            string defaultPath = AppConfigLoader.GetDefaultConfigPath();
            Directory.SetCurrentDirectory(launch);
            File.WriteAllText("appsettings.json", "{broken");
            var loaded = AppConfigLoader.LoadConfiguration(config);
            Assert.Equal(defaultPath, AppConfigLoader.GetDefaultConfigPath());
            Assert.Equal(Path.Combine(_folder, "photos"), Assert.Single(loaded.Paths));
            Assert.Equal(Path.Combine(_folder, "data", "photos.db"), loaded.NomeDb);
            Assert.Equal("jpg", Assert.Single(loaded.EstensioniPermesse));
            Assert.DoesNotContain("png", loaded.EstensioniPermesse);
        }
        finally { Directory.SetCurrentDirectory(previous); }
    }

    [Fact]
    public void ClearingPhotoPreference_DoesNotRestoreAnOldScopeOnNextLaunch()
    {
        string path = Path.Combine(_folder, "settings.json");
        Assert.True(PreferencesSaver.SavePreferences(_folder, Path.Combine(_folder, "photos.db"), path));
        Assert.True(PreferencesSaver.SavePreferences("", Path.Combine(_folder, "photos.db"), path));
        Assert.Empty(AppConfigLoader.LoadConfiguration(path).Paths);
    }

    [Fact]
    public void ExplicitMissingConfiguration_DoesNotFallBackToDefaults()
    {
        Assert.Throws<FileNotFoundException>(() => AppConfigLoader.LoadConfiguration(
            Path.Combine(_folder, "missing.json"), requireExists: true));
    }

    [Fact]
    public void Snapshot_OwnsAndNormalizesExtensionsWithoutAddingMetadataFormats()
    {
        var original = new AppConfig { EstensioniPermesse = new() { ".GIF" } };
        var snapshot = AppConfigLoader.Snapshot(original);
        original.EstensioniPermesse.Add("pdf");
        Assert.Contains("gif", snapshot.EstensioniPermesse);
        Assert.DoesNotContain("pdf", snapshot.EstensioniPermesse);
        Assert.DoesNotContain("jpg", snapshot.EstensioniPermesse);
    }

    [Theory]
    [InlineData("pdf")]
    [InlineData("docx")]
    [InlineData("mp3")]
    [InlineData("wav")]
    public void DefaultSelection_DoesNotIncludeDocumentsOrAudio(string extension) =>
        Assert.DoesNotContain(extension, new AppConfig().EstensioniPermesse);
}

[CollectionDefinition("Configuration filesystem", DisableParallelization = true)]
public class ConfigurationFilesystemCollection;
