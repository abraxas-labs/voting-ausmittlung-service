// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class VoteCountingCircleBallotResult
{
    public VoteCountingCircleBallotResult(BallotResult ballotResult)
    {
        CountingCircle = ballotResult.VoteResult.CountingCircle;
        CountOfVoters = ballotResult.CountOfVoters.MapToNonNullableSubTotal();
        QuestionResults = ballotResult.QuestionResults
            .OrderBy(x => x.Question.Number)
            .ToList();
        TieBreakQuestionResults = ballotResult.TieBreakQuestionResults
            .OrderBy(x => x.Question.Number)
            .ToList();
    }

    public CountingCircle CountingCircle { get; }

    public PoliticalBusinessCountOfVoters CountOfVoters { get; }

    public IEnumerable<BallotQuestionResult> QuestionResults { get; }

    public IEnumerable<TieBreakQuestionResult> TieBreakQuestionResults { get; }
}
