// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class VoteEndResultVotingCardDetail : EndResultVotingCardDetail
{
    public Guid VoteEndResultId { get; set; }
}
