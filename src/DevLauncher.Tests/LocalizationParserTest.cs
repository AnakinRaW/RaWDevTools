using System.Data;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RepublicAtWar.DevLauncher.Localization;
using Testably.Abstractions.Testing;

namespace DevLauncher.Tests;

public class LocalizationParserTest
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly LocalizationFileReaderReader _readerReader;

    public LocalizationParserTest()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(_fileSystem);
        _readerReader = new LocalizationFileReaderReader(sc.BuildServiceProvider());
    }

    private void Setup(string text)
    {
        _fileSystem.Initialize().WithFile("textFile.txt").Which(a => a.HasStringContent(text));
    }


    [Theory]
    [InlineData("")]
    [InlineData("LANGUAGE")]
    [InlineData("LANGUAGE=")]
    public void Test_InvalidFile(string text)
    {
        Setup(text);
        Assert.Throws<SyntaxErrorException>(() => _readerReader.ReadFile("textFile.txt"));
    }

    [Fact]
    public void Test_EmptyList()
    {
        Setup("LANGUAGE='ENGLISH';");

        var localizationFile = _readerReader.ReadFile("textFile.txt");

        Assert.Equal("ENGLISH", localizationFile.Language);
    }
}