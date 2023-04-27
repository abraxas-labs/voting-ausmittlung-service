// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Models.Import;

public class VoteBallotResultImport
{
    private readonly Dictionary<int, BallotQuestionResultImport> _questionResults =
        new Dictionary<int, BallotQuestionResultImport>();

    private readonly Dictionary<int, TieBreakQuestionResultImport> _tieBreakQuestionResults =
        new Dictionary<int, TieBreakQuestionResultImport>();

    public VoteBallotResultImport(Guid ballotId)
    {
        BallotId = ballotId;
    }

    public Guid BallotId { get; }

    public int CountOfVoters { get; internal set; }

    /// <summary>
    /// Gets the count of blank ballots (= "Stimmzettel"), meaning all questions contained an empty vote.
    /// </summary>
    public int BlankBallotCount { get; internal set; }

    public IEnumerable<BallotQuestionResultImport> QuestionResults => _questionResults.Values;

    public IEnumerable<TieBreakQuestionResultImport> TieBreakQuestionResults => _tieBreakQuestionResults.Values;

    internal BallotQuestionResultImport GetOrAddQuestionResult(int questionNumber)
        => _questionResults.GetOrAdd(questionNumber, () => new BallotQuestionResultImport(questionNumber));

    internal TieBreakQuestionResultImport GetOrAddTieBreakQuestionResult(int questionNumber)
        => _tieBreakQuestionResults.GetOrAdd(questionNumber, () => new TieBreakQuestionResultImport(questionNumber));
}
