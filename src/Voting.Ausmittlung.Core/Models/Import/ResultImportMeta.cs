// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;

namespace Voting.Ausmittlung.Core.Models.Import;

public class ResultImportMeta
{
    public ResultImportMeta(
        Guid contestId,
        string fileName,
        Stream fileContent)
    {
        ContestId = contestId;
        FileContent = fileContent;
        FileName = fileName;
    }

    internal Guid ContestId { get; }

    internal Stream FileContent { get; }

    internal string FileName { get; }
}
