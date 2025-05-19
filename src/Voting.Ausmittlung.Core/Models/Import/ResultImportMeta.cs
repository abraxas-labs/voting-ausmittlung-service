// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;

namespace Voting.Ausmittlung.Core.Models.Import;

public class ResultImportMeta
{
    public ResultImportMeta(
        ResultImportType importType,
        Ech0222Version importVersion,
        Guid contestId,
        Guid? basisCountingCircleId,
        string eCH0222FileName,
        Stream eCH0222FileContent,
        string? eCH0110FileName,
        Stream? eCH0110FileContent)
    {
        ImportType = importType;
        ImportVersion = importVersion;
        ContestId = contestId;
        BasisCountingCircleId = basisCountingCircleId;
        Ech0222FileContent = eCH0222FileContent;
        Ech0222FileName = eCH0222FileName;
        Ech0110FileContent = eCH0110FileContent;
        Ech0110FileName = eCH0110FileName;
    }

    public ResultImportType ImportType { get; }

    internal Ech0222Version ImportVersion { get; }

    internal Guid ContestId { get; }

    internal Guid? BasisCountingCircleId { get; }

    internal Stream Ech0222FileContent { get; }

    internal string Ech0222FileName { get; }

    internal Stream? Ech0110FileContent { get; }

    internal string? Ech0110FileName { get; }

    internal string GetUnifiedFileName()
    {
        return Ech0110FileName == null
            ? Ech0222FileName
            : $"{Ech0222FileName} / {Ech0110FileName}";
    }

    internal void Validate()
    {
        switch (ImportType)
        {
            case ResultImportType.EVoting:
                if (Ech0110FileName == null || Ech0110FileContent == null)
                {
                    throw new ValidationException("eCH 0110 file and file name are required for eVoting imports.");
                }

                if (ImportVersion != Ech0222Version.V1)
                {
                    throw new ValidationException("eVoting imports only support eCH 0220 v1.0 (semantic 1.0)");
                }

                if (BasisCountingCircleId.HasValue)
                {
                    throw new ValidationException("eVoting imports need to happen on the contest");
                }

                break;
            case ResultImportType.ECounting:
                if (ImportVersion != Ech0222Version.V3)
                {
                    throw new ValidationException("eCounting imports only support eCH 0220 v1.2 (semantic 3.0)");
                }

                if (!BasisCountingCircleId.HasValue)
                {
                    throw new ValidationException("eCounting imports need to happen on one counting circle");
                }

                break;
            default:
                throw new ValidationException("Unknown import type");
        }
    }
}
