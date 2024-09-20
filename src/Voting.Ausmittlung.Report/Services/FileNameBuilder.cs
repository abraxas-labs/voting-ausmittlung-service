// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Report.Services;

public static class FileNameBuilder
{
    private const string FileNameReplacement = "_";
    private static readonly Regex _validFileName = new("[^0-9a-zA-ZÀ-ÿ-_. ]", RegexOptions.Compiled); // according to VOTING-1905

    public static string GenerateFileName(
    string fileNameFormat,
    ExportFileFormat format,
    IReadOnlyCollection<string>? filenameArgs)
    {
        var filename = $"{fileNameFormat}.{format.ToString().ToLower(CultureInfo.InvariantCulture)}";
        if (filenameArgs == null || filenameArgs.Count == 0)
        {
            return ReplaceInvalidFileNameChars(filename);
        }

        // replace placeholders {0}, {1}, ...
        filename = string.Format(
            CultureInfo.InvariantCulture,
            filename,
            filenameArgs.OfType<object?>().ToArray());
        return ReplaceInvalidFileNameChars(filename);
    }

    internal static string GenerateFileName(
        TemplateModel template,
        IReadOnlyCollection<string>? filenameArgs)
        => GenerateFileName(template.Filename, template.Format, filenameArgs);

    private static string ReplaceInvalidFileNameChars(string filename)
        => _validFileName.Replace(filename, FileNameReplacement);
}
