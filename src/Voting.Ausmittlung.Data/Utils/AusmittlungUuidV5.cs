// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;
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
    private static readonly Guid VotingAusmittlungContestCountingCircleImportsNamespace = Guid.Parse("505a4c60-bfb1-4884-acd3-0e9d5ea70928");
    private static readonly Guid VotingAusmittlungContestImportsNamespace = Guid.Parse("b4164784-6088-40fc-9c2d-053373ce8719");
    private static readonly Guid VotingAusmittlungVoteBallotResultNamespace = Guid.Parse("90e9edff-c0d2-4823-91c1-0d386c06a0ad");
    private static readonly Guid VotingAusmittlungResultExportConfigurationNamespace = Guid.Parse("8b09d497-9b95-4b06-bf00-673b55d03c1c");
    private static readonly Guid VotingAusmittlungDomainOfInfluencePartyNamespace = Guid.Parse("f7f443a0-2d6e-4fd2-be2e-e277ae3e3758");
    private static readonly Guid VotingAusmittlungCountingCircleSnapshotNamespace = Guid.Parse("2608f8af-861b-4b8e-95a2-1f3ea740d56e");
    private static readonly Guid VotingAusmittlungDomainOfInfluenceSnapshotNamespace = Guid.Parse("afbfdc15-97dd-4777-bf83-c62b9cf44e0a");
    private static readonly Guid VotingAusmittlungPoliticalBusinessResultNamespace = Guid.Parse("d9400ff4-1cff-429a-bb9a-99243cdec279");
    private static readonly Guid VotingAusmittlungPoliticalBusinessEndResultNamespace = Guid.Parse("934e92fb-cc6b-4d8d-9d5c-5f0f8e561e46");
    private static readonly Guid VotingAusmittlungPoliticalBusinessUnionEndResultNamespace = Guid.Parse("857e8405-e154-4788-b420-2099ac6e4b4b");
    private static readonly Guid VotingAusmittlungProtocolExportNamespace = Guid.Parse("2995a75c-7795-4c1e-874a-4c133faf636e");
    private static readonly Guid VotingAusmittlungExportTemplateNamespace = Guid.Parse("3d9fc696-281f-4af7-9320-c4ca129c2f90");
    private static readonly Guid VotingAusmittlungCountingCircleElectorateSnapshotNamespace = Guid.Parse("4ea8f0da-1e21-429e-bc02-f24bad32cf37");
    private static readonly Guid VotingAusmittlungContestCountingCircleElectorateNamespace = Guid.Parse("4ea8f0da-1e21-429e-bc02-f24bad32cf37");
    private static readonly Guid VotingAusmittlungProportionalElectionUnionListNamespace = Guid.Parse("9c829147-4c4a-4a59-80b7-4cef41667eab");
    private static readonly Guid VotingAusmittlungDoubleProportionalResultNamespace = Guid.Parse("2ad18af5-75f3-4c04-87df-78744880e4e1");

    // keep in sync with Basis
    private static readonly Guid VotingBasisProportionalElectionNamespace = Guid.Parse("9602b447-bd9d-4ee0-a15c-94eb2f88e79b");

    public static Guid BuildContestCountingCircleDetails(Guid contestId, Guid basisCountingCircleId, bool testingPhaseEnded)
        => Create(VotingAusmittlungContestCountingCircleDetailsNamespace, testingPhaseEnded, contestId, basisCountingCircleId);

    public static Guid BuildContestImports(Guid contestId, bool testingPhaseEnded)
        => Create(VotingAusmittlungContestImportsNamespace, testingPhaseEnded, contestId);

    public static Guid BuildContestCountingCircleImports(Guid contestId, Guid basisCountingCircleId, bool testingPhaseEnded)
        => Create(VotingAusmittlungContestCountingCircleImportsNamespace, testingPhaseEnded, contestId, basisCountingCircleId);

    public static Guid BuildCountingCircleElectorateSnapshot(Guid contestId, Guid basisCountingCircleId, Guid electorateId)
        => Create(VotingAusmittlungCountingCircleElectorateSnapshotNamespace, contestId, basisCountingCircleId, electorateId);

    public static Guid BuildContestCountingCircleElectorate(
        Guid contestId,
        Guid countingCircleId,
        IReadOnlyCollection<DomainOfInfluenceType> domainOfInfluenceTypes)
    {
        var domainOfInfluenceTypesId = string.Join(VotingAusmittlungSeparator, domainOfInfluenceTypes);
        return UuidV5.Create(VotingAusmittlungContestCountingCircleElectorateNamespace, string.Join(VotingAusmittlungSeparator, contestId, countingCircleId, domainOfInfluenceTypesId));
    }

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

    public static Guid BuildPoliticalBusinessUnionEndResult(Guid politicalBusinessUnionId, bool testingPhaseEnded)
        => Create(VotingAusmittlungPoliticalBusinessUnionEndResultNamespace, testingPhaseEnded, politicalBusinessUnionId);

    public static Guid BuildDoubleProportionalResult(Guid? proportionalElectionUnionId, Guid? proportionalElectionId, bool testingPhaseEnded)
        => Create(VotingAusmittlungDoubleProportionalResultNamespace, testingPhaseEnded, proportionalElectionId ?? Guid.Empty, proportionalElectionUnionId ?? Guid.Empty);

    public static Guid BuildProportionalElectionEmptyList(Guid proportionalElectionId)
        => Create(VotingBasisProportionalElectionNamespace, proportionalElectionId);

    public static Guid BuildProportionalElectionUnionList(Guid proportionalElectionUnionId, string orderNumber, string description)
        => UuidV5.Create(VotingAusmittlungProportionalElectionUnionListNamespace, string.Join(VotingAusmittlungSeparator, new[] { proportionalElectionUnionId.ToString(), orderNumber, description }));

    public static Guid BuildExportTemplate(
        string exportKey,
        string tenantId,
        Guid? countingCircleId = null,
        Guid? politicalBusinessId = null,
        Guid? politicalBusinessUnionId = null,
        DomainOfInfluenceType domainOfInfluenceType = DomainOfInfluenceType.Unspecified,
        Guid? politicalBusinessResultBundleId = null,
        Guid? domainOfInfluenceId = null)
    {
        var idParts = new List<object?>
        {
            exportKey,
            tenantId,
            countingCircleId,
            politicalBusinessId,
            politicalBusinessUnionId,
        };

        // this is necessary for backwards compatibility
        if (politicalBusinessResultBundleId.HasValue)
        {
            idParts.Add(politicalBusinessResultBundleId);
        }

        idParts.Add((int)domainOfInfluenceType);

        // This was added later on, existing templates should not be affected by this
        if (domainOfInfluenceId.HasValue)
        {
            idParts.Add(domainOfInfluenceId);
        }

        return UuidV5.Create(VotingAusmittlungExportTemplateNamespace, string.Join(VotingAusmittlungSeparator, idParts));
    }

    public static Guid BuildProtocolExport(Guid contestId, bool testingPhaseEnded, Guid exportTemplateId)
        => Create(VotingAusmittlungProtocolExportNamespace, testingPhaseEnded, contestId, exportTemplateId);

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
            string.Join(VotingAusmittlungSeparator, existingGuids) + VotingAusmittlungSeparator + testingPhaseEnded);
    }
}
