// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

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

    internal static IEnumerable<ElectionGroupInfoType> ToVoteInfoEchMajorityElectionGroups(
        this ICollection<MajorityElection> elections,
        Ech0252MappingContext ctx)
    {
        return elections
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .Select(x => new ElectionGroupInfoType
            {
                ElectionGroup = x.ToVoteInfoEchElectionGroup(ctx),
                CountingCircle = x.Results
                    .OrderBy(r => r.CountingCircle.Name)
                    .Select(r => r.CountingCircle.ToEch0252CountingCircle(x.Contest.DomainOfInfluenceId))
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
        Ech0252MappingContext ctx)
    {
        var canton = election.DomainOfInfluence.Canton;

        return new ElectionGroupInfoTypeElectionGroup()
        {
            ElectionGroupIdentification = election.Id.ToString(),
            SuperiorAuthority = ctx.GetSuperiorAuthority(election.DomainOfInfluence.Bfs)?.ToEchDomainOfInfluence(),
            DomainOfInfluence = election.DomainOfInfluence.ToEchDomainOfInfluence(),
            ElectionInformation = election.SecondaryMajorityElections
                .OrderBy(y => y.PoliticalBusinessNumber)
                .Select(y => y.ToVoteInfoEchElectionInfo(canton))
                .Prepend(election.ToVoteInfoEchElectionInfo(canton))
                .ToList(),
        };
    }

    private static ElectionGroupInfoTypeElectionGroupElectionInformation ToVoteInfoEchElectionInfo(
        this MajorityElection election,
        DomainOfInfluenceCanton canton)
    {
        return new ElectionGroupInfoTypeElectionGroupElectionInformation
        {
            Election = election.ToVoteInfoEchElection(election.Translations, PoliticalBusinessType.MajorityElection),
            ReferencedElectionAssociationId = election.MajorityElectionUnionEntries.FirstOrDefault()?.MajorityElectionUnionId.ToString(),
            Candidate = election.MajorityElectionCandidates
                .OrderBy(c => c.Number)
                .Select(c => c.ToVoteInfoEchCandidate(canton))
                .ToList(),
            OtherIdentification = election.ToOtherIdentification(),
        };
    }

    private static ElectionGroupInfoTypeElectionGroupElectionInformation ToVoteInfoEchElectionInfo(
        this SecondaryMajorityElection election,
        DomainOfInfluenceCanton canton)
    {
        return new ElectionGroupInfoTypeElectionGroupElectionInformation
        {
            Election = election.ToVoteInfoEchElection(election.Translations, PoliticalBusinessType.MajorityElection),
            ReferencedElectionAssociationId = election.PrimaryMajorityElection.MajorityElectionUnionEntries.FirstOrDefault()?.MajorityElectionUnionId.ToString(),
            Candidate = election.Candidates
                .OrderBy(c => c.Number)
                .Select(c => c.ToVoteInfoEchCandidate(canton))
                .ToList(),
        };
    }

    private static CandidateType ToVoteInfoEchCandidate(this MajorityElectionCandidate candidate, DomainOfInfluenceCanton canton)
    {
        return candidate.ToVoteInfoEchCandidate(
            canton,
            candidate.Translations.ToDictionary(x => x.Language, x => x.OccupationTitle),
            candidate.Translations.ToDictionary(x => x.Language, x => x.Party));
    }

    private static CandidateType ToVoteInfoEchCandidate(this SecondaryMajorityElectionCandidate candidate, DomainOfInfluenceCanton canton)
    {
        return candidate.ToVoteInfoEchCandidate(
            canton,
            candidate.Translations.ToDictionary(x => x.Language, x => x.OccupationTitle),
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
