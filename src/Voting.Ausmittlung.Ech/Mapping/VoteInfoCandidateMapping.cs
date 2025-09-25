// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0010_6_0;
using Ech0155_5_1;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;
using Voting.Ausmittlung.Ech.Utils;
using Voting.Lib.Common;
using CandidateType = Ech0252_2_0.CandidateType;
using SexType = Ech0044_4_1.SexType;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoCandidateMapping
{
    private const int MaxCandidateReferenceLength = 10;
    private const string IncumbentText = "bisher";

    private static readonly DateTime DefaultDateOfBirth = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static CandidatePositionInformationType ToEchCandidatePosition(this ProportionalElectionCandidate candidate, Ech0252MappingContext ctx, bool accumulatedPosition)
    {
        var text = candidate.ToEchCandidateText(
            ctx,
            PoliticalBusinessType.ProportionalElection,
            candidate.Translations.ToDictionary(t => t.Language, t => t.OccupationTitle),
            candidate.Party?.Translations.ToDictionary(x => x.Language, x => x.Name));

        var position = accumulatedPosition ? candidate.AccumulatedPosition : candidate.Position;
        return new CandidatePositionInformationType
        {
            PositionOnList = position.ToString(),
            CandidateReferenceOnPosition = GenerateCandidateReference(candidate),
            CandidateIdentification = candidate.Id.ToString(),
            CandidateTextOnPosition = text.CandidateTextInfo,
        };
    }

    internal static CandidateType ToVoteInfoEchCandidate(
        this ElectionCandidate candidate,
        Ech0252MappingContext ctx,
        PoliticalBusinessType politicalBusinessType,
        Dictionary<string, string> occupationTitleTranslations,
        Dictionary<string, string> occupationTranslations,
        Dictionary<string, string>? partyShortTranslations,
        Dictionary<string, string>? partyLongTranslations)
    {
        var candidateText = candidate.ToEchCandidateText(ctx, politicalBusinessType, occupationTitleTranslations, partyShortTranslations);

        var occupationInfos = occupationTranslations?
            .FilterEchExportLanguages(ctx.EVoting)
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .OrderBy(t => t.Key)
            .Select(x => new OccupationalTitleInformationTypeOccupationalTitleInfo
            {
                Language = x.Key,
                OccupationalTitle = x.Value,
            })
            .ToList();

        var address = BuildDwellingAddress(candidate, out var isSwiss);
        return new CandidateType
        {
            CandidateIdentification = candidate.Id.ToString(),
            FamilyName = candidate.LastName,
            FirstName = candidate.FirstName,
            PoliticalFirstName = candidate.PoliticalFirstName,
            PoliticalFamilyName = candidate.PoliticalLastName,
            CallName = candidate.PoliticalFirstName,
            CandidateReference = candidate.Number,
            CandidateText = candidateText.CandidateTextInfo,
            DateOfBirth = candidate.DateOfBirth ?? DefaultDateOfBirth,
            Sex = candidate.Sex.ToEchSexType(),
            OccupationalTitle = occupationInfos?.Count == 0 ? null : occupationInfos,
            Title = candidate.Title,
            DwellingAddress = address,
            LanguageOfCorrespondence = Languages.German,
            PartyAffiliation = ToPartyInfos(partyShortTranslations, partyLongTranslations, ctx),
            IncumbentYesNo = candidate.Incumbent,
            Role = null,
            Swiss = !isSwiss || string.IsNullOrEmpty(candidate.Origin) ? [] : [candidate.Origin],
        };
    }

    internal static CandidateTextInformationType ToEchCandidateText(
        this ElectionCandidate candidate,
        Ech0252MappingContext ctx,
        PoliticalBusinessType politicalBusinessType,
        Dictionary<string, string> occupationTitleTranslations,
        Dictionary<string, string>? partyTranslations = null)
    {
        var dateOfBirthText = DomainOfInfluenceCantonDataTransformer.EchCandidateDateOfBirthText(ctx.Canton, candidate.DateOfBirth ?? DefaultDateOfBirth);
        var localityText = string.IsNullOrEmpty(candidate.Locality) ? string.Empty : $", {candidate.Locality}";
        var candidateTextBase = $"{dateOfBirthText}{{0}}{localityText}{{1}}{{2}}";
        var textInfos = new CandidateTextInformationType();
        foreach (var language in LanguageMapping.GetEchExportLanguages(ctx.EVoting))
        {
            var occupationTitleText = string.Empty;
            if (occupationTitleTranslations.TryGetValue(language, out var occupationTitle))
            {
                occupationTitleText = !string.IsNullOrEmpty(occupationTitle) ? $", {occupationTitle}" : null;
            }

            var partyText = string.Empty;
            if (partyTranslations?.TryGetValue(language, out string? partyTranslatedText) != null)
            {
                partyTranslatedText = DomainOfInfluenceCantonDataTransformer.EchCandidatePartyText(ctx.Canton, politicalBusinessType, partyTranslatedText);
                partyText = !string.IsNullOrEmpty(partyTranslatedText) ? $", {partyTranslatedText}" : null;
            }

            var incumbentText = !candidate.Incumbent
                ? string.Empty
                : $", {IncumbentText}";

            textInfos.CandidateTextInfo.Add(new CandidateTextInformationTypeCandidateTextInfo
            {
                Language = language,
                CandidateText = string.Format(candidateTextBase, occupationTitleText, partyText, incumbentText),
            });
        }

        return textInfos;
    }

    internal static SexType ToEchSexType(this Data.Models.SexType sex)
    {
        return sex switch
        {
            Data.Models.SexType.Male => SexType.Item1,
            Data.Models.SexType.Female => SexType.Item2,
            _ => SexType.Item3,
        };
    }

    internal static MrMrsType ToEchMrMrsType(this Data.Models.SexType sex)
    {
        return sex == Data.Models.SexType.Male
            ? MrMrsType.Item2
            : MrMrsType.Item1;
    }

    internal static WriteInCandidateType ToVoteInfoEchWriteInCandidate(
        this MajorityElectionCandidateBase candidate,
        Dictionary<string, string> occupationTranslations,
        Dictionary<string, string>? partyShortTranslations,
        Dictionary<string, string>? partyLongTranslations,
        Ech0252MappingContext ctx)
    {
        var occupationInfos = occupationTranslations
            .FilterEchExportLanguages(ctx.EVoting)
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .OrderBy(t => t.Key)
            .Select(x => new OccupationalTitleInformationTypeOccupationalTitleInfo
            {
                Language = x.Key,
                OccupationalTitle = x.Value,
            })
            .ToList();

        var dwellingAddress = BuildDwellingAddress(candidate, out var isSwiss);
        var writeInCandidate = new WriteInCandidateType
        {
            CandidateIdentification = candidate.Id.ToString(),
            FamilyName = candidate.LastName,
            FirstName = candidate.FirstName,
            CallName = candidate.PoliticalFirstName,
            Title = candidate.Title,
            OccupationalTitle = occupationInfos.Count == 0 ? null : occupationInfos,
            Swiss = !isSwiss || string.IsNullOrEmpty(candidate.Origin) ? null : [candidate.Origin],
            PartyAffiliation = ToPartyInfos(partyShortTranslations, partyLongTranslations, ctx),
            IncumbentYesNo = candidate.Incumbent,
        };

        if (candidate.DateOfBirth.HasValue)
        {
            writeInCandidate.DateOfBirth = candidate.DateOfBirth.Value;
        }

        if (isSwiss || !string.IsNullOrEmpty(candidate.Locality))
        {
            writeInCandidate.DwellingAddress = dwellingAddress;
        }

        if (candidate.Sex != Data.Models.SexType.Unspecified)
        {
            writeInCandidate.Sex = candidate.Sex.ToEchSexType();
        }

        return writeInCandidate;
    }

    internal static string GenerateCandidateReference(this ProportionalElectionCandidate candidate)
    {
        var reference = $"{candidate.ProportionalElectionList.OrderNumber.PadLeft(2, '0')}.{candidate.Number.PadLeft(2, '0')}";
        return reference.Length > MaxCandidateReferenceLength
            ? reference[..MaxCandidateReferenceLength]
            : reference;
    }

    private static List<PartyAffiliationInformationTypePartyAffiliationInfo>? ToPartyInfos(
        Dictionary<string, string>? partyShortTranslations,
        Dictionary<string, string>? partyLongTranslations,
        Ech0252MappingContext ctx)
    {
        var partyInfos = partyShortTranslations?
            .FilterEchExportLanguages(ctx.EVoting)
            .Where(t => !string.IsNullOrEmpty(t.Value))
            .OrderBy(t => t.Key)
            .Select(x => new PartyAffiliationInformationTypePartyAffiliationInfo
            {
                Language = x.Key,
                PartyAffiliationShort = x.Value,
            })
            .ToList()
            ?? [];

        foreach (var partyInfo in partyInfos)
        {
            if (partyLongTranslations != null && partyLongTranslations.TryGetValue(partyInfo.Language, out var partyLong))
            {
                partyInfo.PartyAffiliationLong = partyLong;
            }
        }

        return partyInfos.Count == 0 ? null : partyInfos;
    }

    private static AddressInformationType BuildDwellingAddress(ElectionCandidate candidate, out bool isSwiss)
    {
        var zipCodeIsSwiss = int.TryParse(candidate.ZipCode, out var zipCode) && zipCode is > 1000 and <= 9999;
        var country = CountryUtils.GetCountryFromIsoId(candidate.Country);
        isSwiss = zipCodeIsSwiss && (country == null || country.IsoId == CountryUtils.SwissCountryIso);

        return new AddressInformationType
        {
            SwissZipCode = isSwiss ? (uint?)zipCode : null,
            ForeignZipCode = isSwiss ? null : candidate.ZipCode,
            Town = candidate.Locality.Truncate(40),
            Street = candidate.Street.Truncate(60),
            HouseNumber = candidate.HouseNumber,
            Country = new CountryType
            {
                CountryId = (ushort)(country?.Id ?? CountryUtils.SwissCountryId),
                CountryIdIso2 = country?.IsoId ?? CountryUtils.SwissCountryIso,
                CountryNameShort = country?.Description ?? CountryUtils.SwissCountryNameShort,
            },
        };
    }
}
