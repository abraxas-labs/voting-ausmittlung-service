// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class PoliticalBusinessResultBallotLog : BaseEntity
{
    public User User { get; set; } = new();

    public DateTime Timestamp { get; set; }
}
