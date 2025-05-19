// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Ech.Models;

public class VotingImportElectionBallotPosition
{
    internal static readonly VotingImportElectionBallotPosition Empty = new(true, false, null, null);

    private VotingImportElectionBallotPosition(bool isEmpty, bool isWriteIn, string? writeInName, Guid? candidateId)
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

    public static VotingImportElectionBallotPosition ForCandidateId(Guid id) => new(false, false, null, id);

    public static VotingImportElectionBallotPosition ForWriteIn(string name) => new(false, true, name, null);
}
