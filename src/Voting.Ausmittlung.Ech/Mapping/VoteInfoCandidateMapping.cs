// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Ech0010_6_0;
using Ech0155_5_0;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using CandidateType = Ech0252_2_0.CandidateType;
using SexType = Ech0044_4_1.SexType;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoCandidateMapping
{
    private const int SwissCountryId = 8100;
    private const string SwissCountryIso = "CH";
    private const string SwissCountryNameShort = "Schweiz";
    private const string IncumbentText = "bisher";

    internal static CandidatePositionInformationType ToEchCandidatePosition(this ProportionalElectionCandidate candidate, bool accumulatedPosition, DomainOfInfluenceCanton canton)
    {
        var text = candidate.ToEchCandidateText(
            canton,
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
        DomainOfInfluenceCanton canton,
        PoliticalBusinessType politicalBusinessType,
        Dictionary<string, string> occupationTitleTranslations,
        Dictionary<string, string> occupationTranslations,
        Dictionary<string, string>? partyShortTranslations,
        Dictionary<string, string>? partyLongTranslations)
    {
        var candidateText = candidate.ToEchCandidateText(canton, politicalBusinessType, occupationTitleTranslations, partyShortTranslations);

        var occupationInfos = occupationTranslations?
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .OrderBy(t => t.Key)
            .Select(x => new OccupationalTitleInformationTypeOccupationalTitleInfo
            {
                Language = x.Key,
                OccupationalTitle = x.Value,
            })
            .ToList();

        var partyInfos = partyShortTranslations?
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

        var zipCodeIsSwiss = int.TryParse(candidate.ZipCode, out var zipCode) && zipCode is > 1000 and <= 9999;

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
            DateOfBirth = candidate.DateOfBirth,
            Sex = candidate.Sex.ToEchSexType(),
            OccupationalTitle = occupationInfos?.Count == 0 ? null : occupationInfos,
            DwellingAddress = new AddressInformationType
            {
                SwissZipCode = zipCodeIsSwiss ? (uint?)zipCode : null,
                ForeignZipCode = zipCodeIsSwiss ? null : candidate.ZipCode,
                Town = candidate.Locality != string.Empty ? candidate.Locality : UnknownMapping.UnknownValue,
                Country = new CountryType
                {
                    CountryId = SwissCountryId,
                    CountryIdIso2 = SwissCountryIso,
                    CountryNameShort = SwissCountryNameShort,
                },
            },
            MrMrs = candidate.Sex.ToEchMrMrsType(),
            LanguageOfCorrespondence = Languages.German,
            PartyAffiliation = partyInfos.Count == 0 ? null : partyInfos,
            IncumbentYesNo = candidate.Incumbent,
            Role = null,
        };
    }

    internal static CandidateTextInformationType ToEchCandidateText(
        this ElectionCandidate candidate,
        DomainOfInfluenceCanton canton,
        PoliticalBusinessType politicalBusinessType,
        Dictionary<string, string> occupationTitleTranslations,
        Dictionary<string, string>? partyTranslations = null)
    {
        var dateOfBirthText = DomainOfInfluenceCantonDataTransformer.EchCandidateDateOfBirthText(canton, candidate.DateOfBirth);
        var candidateTextBase = $"{dateOfBirthText}, {{0}}{candidate.Locality}{{1}}{{2}}";
        var textInfos = new CandidateTextInformationType();
        foreach (var language in Languages.All.OrderBy(l => l))
        {
            var occupationTitleText = string.Empty;
            if (occupationTitleTranslations.TryGetValue(language, out var occupationTitle))
            {
                occupationTitleText = !string.IsNullOrEmpty(occupationTitle) ? $"{occupationTitle}, " : null;
            }

            var partyText = string.Empty;
            if (partyTranslations?.TryGetValue(language, out string? partyTranslatedText) != null)
            {
                partyTranslatedText = DomainOfInfluenceCantonDataTransformer.EchCandidatePartyText(canton, politicalBusinessType, partyTranslatedText);
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
        Dictionary<string, string> partyTranslations)
    {
        var occupationInfos = occupationTranslations
            .OrderBy(t => t.Key)
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .Select(x => new OccupationalTitleInformationTypeOccupationalTitleInfo
            {
                Language = x.Key,
                OccupationalTitle = x.Value,
            })
            .ToList();

        var partyInfos = partyTranslations
            .OrderBy(t => t.Key)
            .Select(x => new PartyAffiliationInformationTypePartyAffiliationInfo
            {
                Language = x.Key,
                PartyAffiliationShort = x.Value,
            })
            .ToList();

        var zipCodeIsSwiss = int.TryParse(candidate.ZipCode, out var zipCode) && zipCode is > 1000 and <= 9999;

        return new WriteInCandidateType
        {
            CandidateIdentification = candidate.Id.ToString(),
            FamilyName = candidate.LastName,
            FirstName = candidate.FirstName,
            CallName = candidate.PoliticalFirstName,
            Title = candidate.Title,
            DateOfBirth = candidate.DateOfBirth,
            Sex = candidate.Sex.ToEchSexType(),
            OccupationalTitle = occupationInfos.Count == 0 ? null : occupationInfos,
            DwellingAddress = new AddressInformationType
            {
                SwissZipCode = zipCodeIsSwiss ? (uint?)zipCode : null,
                ForeignZipCode = zipCodeIsSwiss ? null : candidate.ZipCode,
                Town = candidate.Locality != string.Empty ? candidate.Locality : UnknownMapping.UnknownValue,
                Country = new CountryType
                {
                    CountryId = SwissCountryId,
                    CountryIdIso2 = SwissCountryIso,
                    CountryNameShort = SwissCountryNameShort,
                },
            },
            MrMrs = candidate.Sex.ToEchMrMrsType(),
            PartyAffiliation = partyInfos.Count == 0 ? null : partyInfos,
        };
    }

    private static string GenerateCandidateReference(this ProportionalElectionCandidate candidate)
    {
        return $"{candidate.ProportionalElectionList.OrderNumber.PadLeft(2, '0')}.{candidate.Number.PadLeft(2, '0')}";
    }
}
