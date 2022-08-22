// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using CsvHelper.Configuration.Attributes;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;

internal abstract class WabstiCPoliticalBusinessData
{
    internal const int StartIndex = 1;
    internal const int EndIndex = int.MaxValue;

    [Name("Art")]
    [Index(StartIndex)]
    [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
    public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

    [Name("SortWahlkreis")]
    [Index(StartIndex + 1)]
    public int DomainOfInfluenceSortNumber { get; set; }

    [Name("SortGeschaeft")]
    [Index(StartIndex + 2)]
    public string PoliticalBusinessNumber { get; set; } = string.Empty;

    [Name("GeLfNr")]
    [Index(EndIndex - 1)]
    public Guid PoliticalBusinessId { get; set; }
}
