// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingImport
{
    public EVotingImport(
        string echMessageId,
        Guid contestId,
        IReadOnlyCollection<EVotingPoliticalBusinessResult> results)
    {
        EchMessageId = echMessageId;
        ContestId = contestId;
        PoliticalBusinessResults = results;
    }

    public string EchMessageId { get; }

    public Guid ContestId { get; }

    public IReadOnlyCollection<EVotingPoliticalBusinessResult> PoliticalBusinessResults { get; set; }
}
