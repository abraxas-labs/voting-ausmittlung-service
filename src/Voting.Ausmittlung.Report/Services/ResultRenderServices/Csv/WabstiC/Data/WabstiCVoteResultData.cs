// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;

internal class WabstiCVoteResultData : WabstiCResultData
{
    internal new const int StartIndex = WabstiCResultData.StartIndex * 100;

    [Name("StmHGJa")]
    [Index(StartIndex)]
    public int CountOfAnswerYesQ1 { get; set; }

    [Name("StmHGNein")]
    [Index(StartIndex + 1)]
    public int CountOfAnswerNoQ1 { get; set; }

    [Name("StmHGohneAw")]
    [Index(StartIndex + 2)]
    public int CountOfAnswerUnspecifiedQ1 { get; set; }

    [Name("StmN1Ja")]
    [Index(StartIndex + 3)]
    public int? CountOfAnswerYesQ2 { get; set; }

    [Name("StmN1Nein")]
    [Index(StartIndex + 4)]
    public int? CountOfAnswerNoQ2 { get; set; }

    [Name("StmN1ohneAw")]
    [Index(StartIndex + 5)]
    public int? CountOfAnswerUnspecifiedQ2 { get; set; }

    [Name("StmN11Ja")]
    [Index(StartIndex + 6)]
    public int? CountOfAnswerYesQ3 { get; set; }

    [Name("StmN11Nein")]
    [Index(StartIndex + 7)]
    public int? CountOfAnswerNoQ3 { get; set; }

    [Name("StmN11ohneAw")]
    [Index(StartIndex + 8)]
    public int? CountOfAnswerUnspecifiedQ3 { get; set; }

    [Name("StmN2Ja")]
    [Index(StartIndex + 9)]
    public int? CountOfAnswerYesTBQ1 { get; set; }

    [Name("StmN2Nein")]
    [Index(StartIndex + 10)]
    public int? CountOfAnswerNoTBQ1 { get; set; }

    [Name("StmN2ohneAw")]
    [Index(StartIndex + 11)]
    public int? CountOfAnswerUnspecifiedTBQ1 { get; set; }

    [Name("StmN21Ja")]
    [Index(StartIndex + 12)]
    public int? CountOfAnswerYesTBQ2 { get; set; }

    [Name("StmN21Nein")]
    [Index(StartIndex + 13)]
    public int? CountOfAnswerNoTBQ2 { get; set; }

    [Name("StmN21ohneAw")]
    [Index(StartIndex + 14)]
    public int? CountOfAnswerUnspecifiedTBQ2 { get; set; }

    [Name("StmN22Ja")]
    [Index(StartIndex + 15)]
    public int? CountOfAnswerYesTBQ3 { get; set; }

    [Name("StmN22Nein")]
    [Index(StartIndex + 16)]
    public int? CountOfAnswerNoTBQ3 { get; set; }

    [Name("StmN22ohneAw")]
    [Index(StartIndex + 17)]
    public int? CountOfAnswerUnspecifiedTBQ3 { get; set; }

    public IEnumerable<BallotQuestionResult> QuestionResults
    {
        set
        {
            CountOfAnswerYesQ1 = 0;
            CountOfAnswerYesQ2 = 0;
            CountOfAnswerYesQ3 = 0;
            CountOfAnswerNoQ1 = 0;
            CountOfAnswerNoQ2 = 0;
            CountOfAnswerNoQ3 = 0;
            CountOfAnswerUnspecifiedQ1 = 0;
            CountOfAnswerUnspecifiedQ2 = 0;
            CountOfAnswerUnspecifiedQ3 = 0;

            using var enumerator = value.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return;
            }

            CountOfAnswerYesQ1 = enumerator.Current.TotalCountOfAnswerYes;
            CountOfAnswerNoQ1 = enumerator.Current.TotalCountOfAnswerNo;
            CountOfAnswerUnspecifiedQ1 = enumerator.Current.TotalCountOfAnswerUnspecified;

            if (!enumerator.MoveNext())
            {
                return;
            }

            CountOfAnswerYesQ2 = enumerator.Current.TotalCountOfAnswerYes;
            CountOfAnswerNoQ2 = enumerator.Current.TotalCountOfAnswerNo;
            CountOfAnswerUnspecifiedQ2 = enumerator.Current.TotalCountOfAnswerUnspecified;

            if (!enumerator.MoveNext())
            {
                return;
            }

            CountOfAnswerYesQ3 = enumerator.Current.TotalCountOfAnswerYes;
            CountOfAnswerNoQ3 = enumerator.Current.TotalCountOfAnswerNo;
            CountOfAnswerUnspecifiedQ3 = enumerator.Current.TotalCountOfAnswerUnspecified;
        }
    }

    public IEnumerable<TieBreakQuestionResult> TieBreakQuestionResults
    {
        set
        {
            CountOfAnswerYesTBQ1 = 0;
            CountOfAnswerYesTBQ2 = 0;
            CountOfAnswerYesTBQ3 = 0;
            CountOfAnswerNoTBQ1 = 0;
            CountOfAnswerNoTBQ2 = 0;
            CountOfAnswerNoTBQ3 = 0;
            CountOfAnswerUnspecifiedTBQ1 = 0;
            CountOfAnswerUnspecifiedTBQ2 = 0;
            CountOfAnswerUnspecifiedTBQ3 = 0;

            using var enumerator = value.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return;
            }

            CountOfAnswerYesTBQ1 = enumerator.Current.TotalCountOfAnswerQ1;
            CountOfAnswerNoTBQ1 = enumerator.Current.TotalCountOfAnswerQ2;
            CountOfAnswerUnspecifiedTBQ1 = enumerator.Current.TotalCountOfAnswerUnspecified;

            if (!enumerator.MoveNext())
            {
                return;
            }

            CountOfAnswerYesTBQ2 = enumerator.Current.TotalCountOfAnswerQ1;
            CountOfAnswerNoTBQ2 = enumerator.Current.TotalCountOfAnswerQ2;
            CountOfAnswerUnspecifiedTBQ2 = enumerator.Current.TotalCountOfAnswerUnspecified;

            if (!enumerator.MoveNext())
            {
                return;
            }

            CountOfAnswerYesTBQ3 = enumerator.Current.TotalCountOfAnswerQ1;
            CountOfAnswerNoTBQ3 = enumerator.Current.TotalCountOfAnswerQ2;
            CountOfAnswerUnspecifiedTBQ3 = enumerator.Current.TotalCountOfAnswerUnspecified;
        }
    }
}
