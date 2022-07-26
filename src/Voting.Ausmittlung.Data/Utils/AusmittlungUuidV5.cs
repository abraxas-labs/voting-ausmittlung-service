// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Data.Utils;

/// <summary>
/// Can be used for cases where deterministic id's are required.
/// <remarks>
/// For example for the contest counting circle details:
/// Since only one counting circle details can be created per contest / counting circle
/// a uuid v5 can be used based on the id of the contest + counting circle.
/// Keep in sync with Voting.Migration!
/// </remarks>
/// </summary>
public static class AusmittlungUuidV5
{
    private const string VotingAusmittlungSeparator = ":";

    private static readonly Guid VotingAusmittlungContestCountingCircleDetailsNamespace = Guid.Parse("e843c2dc-226f-4c84-b324-7aa02d3dfc7c");
    private static readonly Guid VotingAusmittlungVoteBallotResultNamespace = Guid.Parse("90e9edff-c0d2-4823-91c1-0d386c06a0ad");
    private static readonly Guid VotingAusmittlungResultExportConfigurationNamespace = Guid.Parse("8b09d497-9b95-4b06-bf00-673b55d03c1c");
    private static readonly Guid VotingAusmittlungDomainOfInfluencePartyNamespace = Guid.Parse("f7f443a0-2d6e-4fd2-be2e-e277ae3e3758");
    private static readonly Guid VotingAusmittlungCountingCircleSnapshotNamespace = Guid.Parse("2608f8af-861b-4b8e-95a2-1f3ea740d56e");
    private static readonly Guid VotingAusmittlungDomainOfInfluenceSnapshotNamespace = Guid.Parse("afbfdc15-97dd-4777-bf83-c62b9cf44e0a");
    private static readonly Guid VotingAusmittlungPoliticalBusinessResultNamespace = Guid.Parse("d9400ff4-1cff-429a-bb9a-99243cdec279");
    private static readonly Guid VotingAusmittlungPoliticalBusinessEndResultNamespace = Guid.Parse("934e92fb-cc6b-4d8d-9d5c-5f0f8e561e46");

    public static Guid BuildContestCountingCircleDetails(Guid contestId, Guid basisCountingCircleId, bool testingPhaseEnded)
        => Create(VotingAusmittlungContestCountingCircleDetailsNamespace, testingPhaseEnded, contestId, basisCountingCircleId);

    public static Guid BuildVoteBallotResult(Guid ballotId, Guid basisCountingCircleId)
        => Create(VotingAusmittlungVoteBallotResultNamespace, ballotId, basisCountingCircleId);

    public static Guid BuildResultExportConfiguration(Guid contestId, Guid exportConfigurationId)
        => Create(VotingAusmittlungResultExportConfigurationNamespace, contestId, exportConfigurationId);

    public static Guid BuildDomainOfInfluenceParty(Guid contestId, Guid domainOfInfluencePartyId)
        => Create(VotingAusmittlungDomainOfInfluencePartyNamespace, contestId, domainOfInfluencePartyId);

    public static Guid BuildDomainOfInfluenceSnapshot(Guid contestId, Guid domainOfInfluenceId)
        => Create(VotingAusmittlungDomainOfInfluenceSnapshotNamespace, contestId, domainOfInfluenceId);

    public static Guid BuildCountingCircleSnapshot(Guid contestId, Guid countingCircleId)
        => Create(VotingAusmittlungCountingCircleSnapshotNamespace, contestId, countingCircleId);

    public static Guid BuildPoliticalBusinessResult(Guid politicalBusinessId, Guid basisCountingCircleId, bool testingPhaseEnded)
        => Create(VotingAusmittlungPoliticalBusinessResultNamespace, testingPhaseEnded, politicalBusinessId, basisCountingCircleId);

    public static Guid BuildPoliticalBusinessEndResult(Guid politicalBusinessId, bool testingPhaseEnded)
        => Create(VotingAusmittlungPoliticalBusinessEndResultNamespace, testingPhaseEnded, politicalBusinessId);

    private static Guid Create(Guid ns, params Guid[] existingGuids)
    {
        return UuidV5.Create(
            ns,
            string.Join(
                VotingAusmittlungSeparator,
                existingGuids));
    }

    private static Guid Create(Guid ns, bool testingPhaseEnded, params Guid[] existingGuids)
    {
        return UuidV5.Create(
            ns,
            string.Join(VotingAusmittlungSeparator, existingGuids) + VotingAusmittlungSeparator + testingPhaseEnded.ToString());
    }
}
