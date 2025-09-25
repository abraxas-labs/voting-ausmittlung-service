// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoMajorityElectionInfoMapping
{
    private const string FederalIdentifier = "idBund";
    private const string DecisiveMajorityElementName = "decisiveMajority";
    private const string AbsoluteMajorityText = "absolute";
    private const string RelativeMajorityText = "relative";

    internal static IEnumerable<ElectionGroupInfoType> ToVoteInfoEchMajorityElectionGroups(
        this IEnumerable<MajorityElection> elections,
        Ech0252MappingContext ctx,
        Dictionary<Guid, int> positionBySuperiorAuthorityId)
    {
        return elections
            .Select(x => new ElectionGroupInfoType
            {
                ElectionGroup = x.ToVoteInfoEchElectionGroup(ctx, positionBySuperiorAuthorityId),
                CountingCircle = x.Results
                    .OrderBy(r => r.CountingCircle.Name)
                    .Select(r => r.CountingCircle.ToEch0252CountingCircle())
                    .ToList(),
            });
    }

    internal static IEnumerable<ElectionAssociationType> ToVoteInfoEchElectionAssociations(this ICollection<MajorityElectionUnion> unions)
    {
        return unions
            .OrderBy(u => u.Description)
            .Select(u => u.ToVoteInfoEchElectionAssociation(null));
    }

    private static ElectionGroupInfoTypeElectionGroup ToVoteInfoEchElectionGroup(
        this MajorityElection election,
        Ech0252MappingContext ctx,
        Dictionary<Guid, int> positionBySuperiorAuthorityId)
    {
        var canton = election.DomainOfInfluence.Canton;
        var superiorAuthority = ctx.GetSuperiorAuthority(election.DomainOfInfluence.Id);
        var superiorAuthorityId = superiorAuthority?.Id ?? Guid.Empty;

        var previousPosition = positionBySuperiorAuthorityId.GetValueOrDefault(superiorAuthorityId, 0);
        var position = previousPosition + 1;
        positionBySuperiorAuthorityId[superiorAuthorityId] = position;

        return new ElectionGroupInfoTypeElectionGroup
        {
            ElectionGroupIdentification = election.Id.ToString(),
            ElectionGroupPosition = position.ToString(),
            SuperiorAuthority = superiorAuthority?.ToEchDomainOfInfluence(),
            DomainOfInfluence = election.DomainOfInfluence.ToEchDomainOfInfluence(),
            ElectionInformation = election.SecondaryMajorityElections
                .OrderBy(y => y.PoliticalBusinessNumber)
                .Select(y => y.ToVoteInfoEchElectionInfo(ctx))
                .Prepend(election.ToVoteInfoEchElectionInfo(ctx))
                .ToList(),
        };
    }

    private static ElectionGroupInfoTypeElectionGroupElectionInformation ToVoteInfoEchElectionInfo(
        this MajorityElection election,
        Ech0252MappingContext ctx)
    {
        return new ElectionGroupInfoTypeElectionGroupElectionInformation
        {
            Election = election.ToVoteInfoEchElection(ctx, election.Translations, PoliticalBusinessType.MajorityElection),
            ReferencedElectionAssociationId = election.MajorityElectionUnionEntries.FirstOrDefault()?.MajorityElectionUnionId.ToString(),
            Candidate = election.MajorityElectionCandidates
                .Where(x => !x.CreatedDuringActiveContest)
                .OrderBy(c => c.Number)
                .Select(c => c.ToVoteInfoEchCandidate(ctx))
                .ToList(),
            OtherIdentification = election.ToOtherIdentification(),
            NamedElement = new List<NamedElementType>
            {
                new()
                {
                    ElementName = DecisiveMajorityElementName,
                    Text = election.MandateAlgorithm == MajorityElectionMandateAlgorithm.AbsoluteMajority ? AbsoluteMajorityText : RelativeMajorityText,
                },
            },
        };
    }

    private static ElectionGroupInfoTypeElectionGroupElectionInformation ToVoteInfoEchElectionInfo(
        this SecondaryMajorityElection election,
        Ech0252MappingContext ctx)
    {
        return new ElectionGroupInfoTypeElectionGroupElectionInformation
        {
            Election = election.ToVoteInfoEchElection(ctx, election.Translations, PoliticalBusinessType.MajorityElection),
            ReferencedElectionAssociationId = election.PrimaryMajorityElection.MajorityElectionUnionEntries.FirstOrDefault()?.MajorityElectionUnionId.ToString(),
            Candidate = election.Candidates
                .Where(x => !x.CreatedDuringActiveContest)
                .OrderBy(c => c.Number)
                .Select(c => c.ToVoteInfoEchCandidate(ctx))
                .ToList(),
            NamedElement = new List<NamedElementType>
            {
                new()
                {
                    ElementName = DecisiveMajorityElementName,
                    Text = election.PrimaryMajorityElection.MandateAlgorithm == MajorityElectionMandateAlgorithm.AbsoluteMajority
                        ? AbsoluteMajorityText
                        : RelativeMajorityText,
                },
            },
        };
    }

    private static CandidateType ToVoteInfoEchCandidate(this MajorityElectionCandidate candidate, Ech0252MappingContext ctx)
    {
        return candidate.ToVoteInfoEchCandidate(
            ctx,
            PoliticalBusinessType.MajorityElection,
            candidate.Translations.ToDictionary(x => x.Language, x => x.OccupationTitle),
            candidate.Translations.ToDictionary(x => x.Language, x => x.Occupation),
            candidate.Translations.ToDictionary(x => x.Language, x => x.Party),
            candidate.Translations.ToDictionary(x => x.Language, x => x.Party));
    }

    private static CandidateType ToVoteInfoEchCandidate(this SecondaryMajorityElectionCandidate candidate, Ech0252MappingContext ctx)
    {
        return candidate.ToVoteInfoEchCandidate(
            ctx,
            PoliticalBusinessType.SecondaryMajorityElection,
            candidate.Translations.ToDictionary(x => x.Language, x => x.OccupationTitle),
            candidate.Translations.ToDictionary(x => x.Language, x => x.Occupation),
            candidate.Translations.ToDictionary(x => x.Language, x => x.Party),
            candidate.Translations.ToDictionary(x => x.Language, x => x.Party));
    }

    private static List<NamedIdType> ToOtherIdentification(this MajorityElection majorityElection)
    {
        return majorityElection.FederalIdentification.HasValue
            ? new List<NamedIdType>
            {
                new NamedIdType
                {
                    IdName = FederalIdentifier,
                    Id = majorityElection.FederalIdentification.Value.ToString(CultureInfo.InvariantCulture),
                },
            }
            : new List<NamedIdType>();
    }
}
