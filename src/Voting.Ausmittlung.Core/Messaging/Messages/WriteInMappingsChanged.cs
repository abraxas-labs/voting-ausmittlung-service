// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Messaging.Messages;

// the name of the rabbit queue is generated based on the fqn of this class
// if it is moved or renamed this should be considered.
public class WriteInMappingsChanged
{
    public WriteInMappingsChanged(Guid electionResultId, bool isReset, int duplicatedCandidates, int invalidDueToEmptyBallot)
    {
        ElectionResultId = electionResultId;
        IsReset = isReset;
        DuplicatedCandidates = duplicatedCandidates;
        InvalidDueToEmptyBallot = invalidDueToEmptyBallot;
    }

    public Guid ElectionResultId { get; }

    public bool IsReset { get; }

    public int DuplicatedCandidates { get; }

    public int InvalidDueToEmptyBallot { get; }
}
