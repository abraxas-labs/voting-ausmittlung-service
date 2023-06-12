// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;

namespace Voting.Ausmittlung.Core.Models.Import;

public class ResultImportMeta
{
    public ResultImportMeta(
        Guid contestId,
        string eCH0222FileName,
        Stream eCH0222FileContent,
        string eCH0110FileName,
        Stream eCH0110FileContent)
    {
        ContestId = contestId;
        Ech0222FileContent = eCH0222FileContent;
        Ech0222FileName = eCH0222FileName;
        Ech0110FileContent = eCH0110FileContent;
        Ech0110FileName = eCH0110FileName;
    }

    internal Guid ContestId { get; }

    internal Stream Ech0222FileContent { get; }

    internal string Ech0222FileName { get; }

    internal Stream Ech0110FileContent { get; }

    internal string Ech0110FileName { get; }
}
