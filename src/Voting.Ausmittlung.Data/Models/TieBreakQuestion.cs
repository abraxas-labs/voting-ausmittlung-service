// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class TieBreakQuestion : BaseEntity
{
    public int Number { get; set; }

    public int Question1Number { get; set; }

    public int Question2Number { get; set; }

    public Ballot Ballot { get; set; } = null!;

    public Guid BallotId { get; set; }

    public ICollection<TieBreakQuestionTranslation> Translations { get; set; } = new HashSet<TieBreakQuestionTranslation>();

    public ICollection<TieBreakQuestionResult> Results { get; set; } = new HashSet<TieBreakQuestionResult>();

    public TieBreakQuestionEndResult? EndResult { get; set; }

    public string Question => Translations.GetTranslated(x => x.Question);

    public ICollection<VoteResultBallotTieBreakQuestionAnswer> BallotAnswers { get; set; } = new HashSet<VoteResultBallotTieBreakQuestionAnswer>();
}
