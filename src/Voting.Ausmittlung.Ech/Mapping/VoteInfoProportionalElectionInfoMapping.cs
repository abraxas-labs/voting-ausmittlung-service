// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ech0155_5_2;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoProportionalElectionInfoMapping
{
    private const string FederalIdentifier = "idBund";
    private const string EmptyListOrderNumber = "99";
    private const string EmptyListPosition = "99";
    private const string EmptyListShortDescription = "WoP";
    private static readonly Dictionary<string, string> EmptyListDescriptions = new()
    {
        { Languages.German, "Wahlzettel ohne Parteibezeichnung (Leere Liste)" },
        { Languages.French, "Bulletin de vote sans désignation de parti" },
        { Languages.Italian, "Scheda elettorale senza indicazione del partito" },
        { Languages.Romansh, "Cedel electoral senza indicaziun da la partida" },
    };

    internal static IEnumerable<ElectionGroupInfoType> ToVoteInfoEchProportionalElectionGroups(
        this IEnumerable<ProportionalElection> elections,
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

    internal static IEnumerable<ElectionAssociationType> ToVoteInfoEchElectionAssociations(this ICollection<ProportionalElectionUnion> unions)
    {
        return unions
            .OrderBy(u => u.Description)
            .Select(u => u.ToVoteInfoEchElectionAssociation());
    }

    private static ElectionGroupInfoTypeElectionGroup ToVoteInfoEchElectionGroup(
        this ProportionalElection election,
        Ech0252MappingContext ctx,
        Dictionary<Guid, int> positionBySuperiorAuthorityId)
    {
        var superiorAuthority = ctx.GetSuperiorAuthority(election.DomainOfInfluence.Id);
        var superiorAuthorityId = superiorAuthority?.Id ?? Guid.Empty;

        var previousPosition = positionBySuperiorAuthorityId.GetValueOrDefault(superiorAuthorityId, 0);
        var position = previousPosition + 1;
        positionBySuperiorAuthorityId[superiorAuthorityId] = position;

        return new ElectionGroupInfoTypeElectionGroup
        {
            ElectionGroupIdentification = election.Id.ToString(),
            SuperiorAuthority = superiorAuthority?.ToEchDomainOfInfluence(),
            DomainOfInfluence = election.DomainOfInfluence.ToEchDomainOfInfluence(),
            ElectionInformation = new List<ElectionGroupInfoTypeElectionGroupElectionInformation>
            {
                election.ToVoteInfoEchElectionInfo(ctx),
            },
            ElectionGroupPosition = position.ToString(),
        };
    }

    private static ElectionGroupInfoTypeElectionGroupElectionInformation ToVoteInfoEchElectionInfo(this ProportionalElection election, Ech0252MappingContext ctx)
    {
        return new ElectionGroupInfoTypeElectionGroupElectionInformation
        {
            Election = election.ToVoteInfoEchElection(ctx, election.Translations, PoliticalBusinessType.ProportionalElection),
            Quorum = GetQuorum(election.MandateAlgorithm),
            ReferencedElectionAssociationId = election.ProportionalElectionUnionEntries.FirstOrDefault()?.ProportionalElectionUnionId.ToString(),
            List = election.ProportionalElectionLists
                            .OrderBy(l => l.OrderNumber)
                            .Select(l => l.ToVoteInfoEchList(ctx))
                            .Append(CreateEmptyListType(election, ctx))
                            .ToList(),
            ListUnion = election.ProportionalElectionListUnions
                            .OrderBy(lu => lu.Position)
                            .Select(lu => lu.ToVoteInfoEchListUnion(ctx))
                            .ToList(),
            Candidate = election.ProportionalElectionLists
                            .SelectMany(l => l.ProportionalElectionCandidates)
                            .OrderBy(c => c.ProportionalElectionList.OrderNumber)
                            .ThenBy(c => c.Position)
                            .Select(c => c.ToEchCandidate(ctx))
                            .ToList(),
            OtherIdentification = election.ToOtherIdentification(),
        };
    }

    private static ElectionAssociationType ToVoteInfoEchElectionAssociation(this ProportionalElectionUnion union)
    {
        var mandateAlgorithm = union.ProportionalElectionUnionEntries.FirstOrDefault()?.ProportionalElection.MandateAlgorithm
            ?? ProportionalElectionMandateAlgorithm.Unspecified;

        return union.ToVoteInfoEchElectionAssociation(GetQuorum(mandateAlgorithm));
    }

    private static ListType ToVoteInfoEchList(this ProportionalElectionList list, Ech0252MappingContext ctx)
    {
        var descriptionInfos = list.Translations
            .FilterAndSortEchExportLanguages(ctx.EVoting)
            .Select(t => new ListDescriptionInformationTypeListDescriptionInfo
            {
                Language = t.Language,
                ListDescription = t.Description,
                ListDescriptionShort = t.ShortDescription,
            })
            .ToList();

        var candidatePositions = new List<CandidatePositionInformationType>();
        foreach (var candidate in list.ProportionalElectionCandidates.OrderBy(c => c.Position))
        {
            candidatePositions.Add(candidate.ToEchCandidatePosition(ctx, false));
            if (candidate.Accumulated)
            {
                candidatePositions.Add(candidate.ToEchCandidatePosition(ctx, true));
            }
        }

        return new ListType
        {
            ListIdentification = list.Id.ToString(),
            ListIndentureNumber = list.OrderNumber,
            ListDescription = descriptionInfos,
            IsEmptyList = false,
            ListOrderOfPrecedence = list.Position.ToString(),
            TotalPositionsOnList = list.ProportionalElectionCandidates.Sum(c => c.Accumulated ? 2 : 1).ToString(),
            CandidatePosition = candidatePositions,
            EmptyListPositions = list.BlankRowCount.ToString(),
            ListUnionBallotText = null,
        };
    }

    private static ListType CreateEmptyListType(ProportionalElection proportionalElection, Ech0252MappingContext ctx)
    {
        var descriptionInfos = LanguageMapping.GetEchExportLanguages(ctx.EVoting)
            .Select(language => new ListDescriptionInformationTypeListDescriptionInfo
            {
                Language = language,
                ListDescription = EmptyListDescriptions.TryGetValue(language, out var description)
                    ? description
                    : EmptyListDescriptions[Languages.German],
                ListDescriptionShort = EmptyListShortDescription,
            })
            .ToList();

        return new ListType
        {
            ListIdentification = AusmittlungUuidV5.BuildProportionalElectionEmptyList(proportionalElection.Id).ToString(),
            ListIndentureNumber = EmptyListOrderNumber,
            ListDescription = descriptionInfos,
            IsEmptyList = true,
            ListOrderOfPrecedence = EmptyListPosition,
            TotalPositionsOnList = "0",
            EmptyListPositions = proportionalElection.NumberOfMandates.ToString(),
            ListUnionBallotText = null,
        };
    }

    private static CandidateType ToEchCandidate(this ProportionalElectionCandidate candidate, Ech0252MappingContext ctx)
    {
        var candidateType = candidate.ToVoteInfoEchCandidate(
            ctx,
            PoliticalBusinessType.ProportionalElection,
            candidate.Translations.ToDictionary(x => x.Language, x => x.OccupationTitle),
            candidate.Translations.ToDictionary(x => x.Language, x => x.Occupation),
            candidate.Party?.Translations.ToDictionary(t => t.Language, t => t.ShortDescription),
            candidate.Party?.Translations.ToDictionary(t => t.Language, t => t.Name));

        candidateType.CandidateReference = GenerateCandidateReference(candidate);
        return candidateType;
    }

    private static ListUnionType ToVoteInfoEchListUnion(this ProportionalElectionListUnion listUnion, Ech0252MappingContext ctx)
    {
        var descriptionInfos = listUnion.Translations
            .FilterAndSortEchExportLanguages(ctx.EVoting)
            .Select(t => new ListUnionDescriptionTypeListUnionDescriptionInfo
            {
                Language = t.Language,
                ListUnionDescription = t.Description,
            })
            .ToList();

        var listIds = listUnion.ProportionalElectionListUnionEntries
            .OrderBy(e => e.ProportionalElectionListId)
            .Select(e => e.ProportionalElectionListId.ToString())
            .ToList();

        var relation = listUnion.IsSubListUnion ? ListRelationType.Item2 : ListRelationType.Item1;

        return new ListUnionType
        {
            ListUnionIdentification = listUnion.Id.ToString(),
            ListUnionDescription = descriptionInfos,
            ListUnionTypeProperty = relation,
            ReferencedList = listIds,
            ReferencedListUnion = listUnion.ProportionalElectionRootListUnionId?.ToString(),
        };
    }

    private static decimal? GetQuorum(ProportionalElectionMandateAlgorithm mandateAlgorithm)
    {
        return mandateAlgorithm switch
        {
            ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum => 0,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum => 5,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum => 5,
            _ => null,
        };
    }

    private static string GenerateCandidateReference(this ProportionalElectionCandidate candidate)
    {
        return $"{candidate.ProportionalElectionList.OrderNumber.PadLeft(2, '0')}.{candidate.Number.PadLeft(2, '0')}";
    }

    private static List<NamedIdType> ToOtherIdentification(this ProportionalElection proportionalElection)
    {
        return proportionalElection.FederalIdentification.HasValue
            ? new List<NamedIdType>
            {
                new NamedIdType
                {
                    IdName = FederalIdentifier,
                    Id = proportionalElection.FederalIdentification.Value.ToString(CultureInfo.InvariantCulture),
                },
            }
            : new List<NamedIdType>();
    }
}
