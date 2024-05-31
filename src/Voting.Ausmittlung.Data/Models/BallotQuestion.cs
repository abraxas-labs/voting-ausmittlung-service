// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class BallotQuestion : BaseEntity
{
    public int Number { get; set; }

    public Guid BallotId { get; set; }

    public Ballot Ballot { get; set; } = null!; // set by ef

    public ICollection<BallotQuestionTranslation> Translations { get; set; } = new HashSet<BallotQuestionTranslation>();

    public ICollection<BallotQuestionResult> Results { get; set; } = new HashSet<BallotQuestionResult>();

    public BallotQuestionEndResult? EndResult { get; set; }

    public string Question => Translations.GetTranslated(x => x.Question);

    public ICollection<VoteResultBallotQuestionAnswer> BallotAnswers { get; set; } = new HashSet<VoteResultBallotQuestionAnswer>();

    public BallotQuestionType Type { get; set; }
}
