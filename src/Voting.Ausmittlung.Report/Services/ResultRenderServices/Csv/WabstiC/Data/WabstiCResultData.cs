// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using CsvHelper.Configuration.Attributes;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;

internal abstract class WabstiCResultData : WabstiCPoliticalBusinessData, IWabstiCSwissAbroadCountOfVoters
{
    internal new const int StartIndex = WabstiCPoliticalBusinessData.StartIndex * 100;
    internal new const int EndIndex = WabstiCPoliticalBusinessData.EndIndex / 100;

    [Ignore]
    public Guid CountingCircleId { get; set; }

    [Name("BfsNrGemeinde")]
    [Index(StartIndex)]
    public string CountingCircleBfs { get; set; } = string.Empty;

    [Name("EinheitCode")]
    [Index(StartIndex + 1)]
    public string CountingCircleCode { get; set; } = string.Empty;

    [Name("Stimmberechtigte")]
    [Index(StartIndex + 2)]
    public int CountOfVotersTotal { get; set; }

    [Name("StimmberechtigteAusl")]
    [Index(StartIndex + 3)]
    public int CountOfVotersTotalSwissAbroad { get; set; }

    [Name("Stimmbeteiligung")]
    [TypeConverter(typeof(WabstiCPercentageConverter))]
    [Index(StartIndex + 4)]
    public decimal VoterParticipation { get; set; }

    [Name("StmAbgegeben")]
    [Index(StartIndex + 5)]
    public int TotalReceivedBallots { get; set; }

    [Name("StmUngueltig")]
    [Index(StartIndex + 6)]
    public int CountOfInvalidBallots { get; set; }

    [Name("StmLeer")]
    [Index(StartIndex + 7)]
    public int CountOfBlankBallots { get; set; }

    [Name("StmGueltig")]
    [Index(StartIndex + 8)]
    public int CountOfAccountedBallots { get; set; }

    [Name("FreigabeGde")]
    [TypeConverter(typeof(WabstiCTimeConverter))]
    [Index(EndIndex)]
    public DateTime? SubmissionDoneTimestamp { get; set; }
}
