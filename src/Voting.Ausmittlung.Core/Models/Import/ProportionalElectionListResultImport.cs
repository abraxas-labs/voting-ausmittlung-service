// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Models.Import;

public class ProportionalElectionListResultImport
{
    public ProportionalElectionListResultImport(Guid listId)
    {
        ListId = listId;
    }

    public int UnmodifiedListsCount { get; internal set; }

    public int UnmodifiedListVotesCount { get; internal set; }

    public int UnmodifiedListBlankRowsCount { get; internal set; }

    public int ModifiedListsCount { get; internal set; }

    public int ModifiedListVotesCount { get; internal set; }

    public int ListVotesCountOnOtherLists { get; internal set; }

    public int ModifiedListBlankRowsCount { get; internal set; }

    public Guid ListId { get; }
}
