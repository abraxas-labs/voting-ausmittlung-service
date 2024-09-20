// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingElectionBallotPosition
{
    internal static readonly EVotingElectionBallotPosition Empty = new(true, false, null, null);

    private EVotingElectionBallotPosition(bool isEmpty, bool isWriteIn, string? writeInName, Guid? candidateId)
    {
        IsEmpty = isEmpty;
        IsWriteIn = isWriteIn;
        WriteInName = writeInName;
        CandidateId = candidateId;
    }

    public bool IsEmpty { get; internal set; }

    public bool IsWriteIn { get; internal set; }

    public string? WriteInName { get; internal set; }

    public Guid? CandidateId { get; internal set; }

    public static EVotingElectionBallotPosition ForCandidateId(Guid id) => new(false, false, null, id);

    public static EVotingElectionBallotPosition ForWriteIn(string name) => new(false, true, name, null);
}
