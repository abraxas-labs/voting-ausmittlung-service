// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Ausmittlung.Report.Services;
using Voting.Lib.VotingExports.Models;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class FileNameBuilderTest
{
    [Theory]
    [InlineData("Standard {0}", new[] { "Ausschaffungsinitiative" }, "Standard Ausschaffungsinitiative.pdf")]
    [InlineData("Invalid chars no argument \\/äö$", new string[0], "Invalid chars no argument __äö_.pdf")]
    [InlineData("Invalid chars \\/äö$ {0} ({1})", new[] { "Regierungswahl", "Kanton SG" }, "Invalid chars __äö_ Regierungswahl _Kanton SG_.pdf")]
    [InlineData("Invalid chars in argument {0}", new[] { "Regierungswahl/\\" }, "Invalid chars in argument Regierungswahl__.pdf")]
    public void GenerateFileNameShouldBuildAndCleanFileName(string fileNameFormat, string[] args, string expectedFileName)
    {
        FileNameBuilder.GenerateFileName(fileNameFormat, ExportFileFormat.Pdf, args)
            .Should()
            .Be(expectedFileName);
    }
}
