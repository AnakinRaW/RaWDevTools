using System.Data;
using RepublicAtWar.DevLauncher.Localization;

namespace DevLauncher.Tests;

public class LocalizationParserTest
{
    private LocalizationFile Setup(string text)
    {
        return LocalizationFileReader.ReadFromText(text);
    }


    [Theory]
    [InlineData("")]
    [InlineData("LANGUAGE")]
    [InlineData("LANGUAGE=")]
    public void Test_InvalidFile(string text)
    {
        Assert.Throws<SyntaxErrorException>(() => Setup(text));
    }

    [Fact]
    public void Test_EmptyList()
    {
        var localizationFile = Setup("LANGUAGE = ENGLISH;");
        Assert.Equal("ENGLISH", localizationFile.Language);
    }

    [Fact]
    public void Test_SingleEntry()
    {
        var text = @"
LANGUAGE=ENGLISH;
KEYs=
";
        var localizationFile = Setup(text);
        Assert.Equal("ENGLISH", localizationFile.Language);
    }

    [Fact]
    public void Test_MultiEntry()
    {
        var text = @"
LANGUAGE=ENGLISH;
KEYs=
KEY 123=
";
        var localizationFile = Setup(text);
        Assert.Equal("ENGLISH", localizationFile.Language);
    }
}