using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RepublicAtWar.DevLauncher.Localization;
using Testably.Abstractions.Testing;
using static System.Net.Mime.MediaTypeNames;

namespace DevLauncher.Tests;

public class LocalizationParserTest
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly IServiceProvider _serviceProvider;

    public LocalizationParserTest()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IFileSystem>(_fileSystem);
        _serviceProvider = sc.BuildServiceProvider();
    }

    private LocalizationFileReader Setup(string text)
    {
        _fileSystem.Initialize().WithFile("textFile.txt")
            .Which(a => a.HasStringContent(text));
        return new LocalizationFileReader("textFile.txt", false, _serviceProvider);
    }


    [Theory]
    [InlineData("")]
    [InlineData("LANGUAGE")]
    [InlineData("LANGUAGE=")]
    [InlineData("LANGUAGE=ENGLISH")]
    public void Test_InvalidText_Language(string text)
    {
        Assert.Throws<SyntaxErrorException>(Setup(text).Read);
    }

    [Fact]
    public void Test_ReadFile_Invalid_EmptyValue1()
    {
        var text = @"
LANGUAGE='LANG';
key=
";
        Assert.Throws<SyntaxErrorException>(Setup(text).Read);
    }

    [Fact]
    public void Test_ReadFile_Invalid_EmptyValue2()
    {
        var text = @"
LANGUAGE='LANG';
key=
key=""""
";
        Assert.Throws<SyntaxErrorException>(Setup(text).Read);
    }

    [Fact]
    public void Test_ReadFile_Invalid_AmbiguousQuoteValue()
    {
        var text = @"
LANGUAGE='LANG';
quoteOnly=""This value is interpreted as DQString
leadingQuotes=""""key\=123 value
";
        Assert.Throws<SyntaxErrorException>(() => Setup(text).Read());
    }

    [Fact]
    public void Test_ReadFile_DuplicateKey_Throws()
    {
        var text = @"
LANGUAGE='LANG';
key=value
key=value1
";
        Assert.Throws<InvalidLocalizationFileException>(() => Setup(text).Read());
    }


    [Fact]
    public void Test_EmptyList()
    {
        const string text = "LANGUAGE='ENGLISH';";
        var localizationFile = Setup(text).Read();
        Assert.Equal("ENGLISH", localizationFile.Language);
        Assert.Equal(new List<LocalizationEntry>(), localizationFile.Entries);
    }


    [Fact]
    public void Test_ReadFile_Integration()
    {
        var text = @"
LANGUAGE='ENGLISH';

# This is a comment

test123=""test string # with \"" all """" sorts of ' special = characters 
and a line break""

# This is a comment

other_key-complex .123		=	test string \# with \"" all \"" sorts of \' special \= characters
key1=123\n\r \""123\"" key \= 123

key2=value

empty=""""

quoteOnly=\""

trailingSpace=""dqstring with trailing space should get ignored""          
";

        Setup(text);
        var localizationFile = Setup(text).Read();
        Assert.Equal("ENGLISH", localizationFile.Language);
        Assert.Equal(new List<LocalizationEntry>
            {
                new("test123", @"test string # with "" all "" sorts of ' special = characters and a line break"),
                new("other_key-complex .123", @"test string # with "" all "" sorts of ' special = characters"),
                new("key1", @"123\n\r ""123"" key = 123"),
                new("key2", @"value"),
                new("empty", string.Empty),
                new("quoteOnly", "\""),
                new("trailingSpace", "dqstring with trailing space should get ignored"),
            },
            localizationFile.Entries);
    }
}