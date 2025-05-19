// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public static class ResultImportTypeExtensions
{
    public static VotingDataSource GetDataSource(this ResultImportType importType)
    {
        return importType switch
        {
            ResultImportType.EVoting => VotingDataSource.EVoting,
            ResultImportType.ECounting => VotingDataSource.ECounting,
            _ => throw new ArgumentOutOfRangeException(nameof(importType), importType, "Import type is not supported."),
        };
    }

    public static ResultImportType GetImportType(this VotingDataSource dataSource)
    {
        return dataSource switch
        {
            VotingDataSource.EVoting => ResultImportType.EVoting,
            VotingDataSource.ECounting => ResultImportType.ECounting,
            _ => throw new ArgumentOutOfRangeException(nameof(dataSource), dataSource, "Data source is not supported."),
        };
    }

    public static VotingChannel GetChannel(this ResultImportType importType)
    {
        return importType switch
        {
            ResultImportType.EVoting => VotingChannel.EVoting,
            _ => throw new ArgumentOutOfRangeException(nameof(importType), importType, "Import type is not supported."),
        };
    }
}
