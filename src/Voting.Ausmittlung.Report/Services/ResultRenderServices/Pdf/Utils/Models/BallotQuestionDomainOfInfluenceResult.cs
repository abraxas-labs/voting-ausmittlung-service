// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class BallotQuestionDomainOfInfluenceResult :
        IHasSubTotals<BallotQuestionResultSubTotal>,
        IBallotQuestionResultTotal<int>
{
    public BallotQuestion Question { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public BallotResult BallotResult { get; set; } = null!;

    public Guid BallotResultId { get; set; }

    public BallotQuestionResultSubTotal EVotingSubTotal { get; set; } = new BallotQuestionResultSubTotal();

    public BallotQuestionResultSubTotal ConventionalSubTotal { get; set; } = new BallotQuestionResultSubTotal();

    public int TotalCountOfAnswerYes => EVotingSubTotal.TotalCountOfAnswerYes + ConventionalSubTotal.TotalCountOfAnswerYes;

    public int TotalCountOfAnswerNo => EVotingSubTotal.TotalCountOfAnswerNo + ConventionalSubTotal.TotalCountOfAnswerNo;

    public int TotalCountOfAnswerUnspecified => EVotingSubTotal.TotalCountOfAnswerUnspecified + ConventionalSubTotal.TotalCountOfAnswerUnspecified;

    public bool HasMajority => TotalCountOfAnswerYes > TotalCountOfAnswerNo;

    public decimal PercentageYes =>

        // total count of answers cannot be negative, checked by business rules
        TotalCountOfAnswerYes == 0 && TotalCountOfAnswerNo == 0
            ? 0
            : (decimal)TotalCountOfAnswerYes / (TotalCountOfAnswerYes + TotalCountOfAnswerNo);

    public decimal PercentageNo => 1 - PercentageYes;

    public int CountOfAnswerTotal => TotalCountOfAnswerYes + TotalCountOfAnswerNo + TotalCountOfAnswerUnspecified;

    public int CountOfCountingCircleYes { get; set; }

    public int CountOfCountingCircleNo { get; set; }
}
