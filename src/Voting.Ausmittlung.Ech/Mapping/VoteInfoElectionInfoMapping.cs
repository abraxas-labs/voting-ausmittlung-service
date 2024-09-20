﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0155_5_0;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoElectionInfoMapping
{
    internal static ElectionType ToVoteInfoEchElection<TPoliticalBusinessTranslation>(
        this Election election,
        ICollection<TPoliticalBusinessTranslation> translations,
        PoliticalBusinessType politicalBusinessType)
        where TPoliticalBusinessTranslation : PoliticalBusinessTranslation
    {
        var descriptionInfos = translations
            .OrderBy(t => t.Language)
            .Select(t => new ElectionDescriptionInformationTypeElectionDescriptionInfo
            {
                Language = t.Language,
                ElectionDescription = t.OfficialDescription.Truncate(255),
                ElectionDescriptionShort = t.ShortDescription,
            })
            .ToList();

        return new ElectionType
        {
            ElectionIdentification = election.Id.ToString(),
            NumberOfMandates = election.NumberOfMandates.ToString(),
            ElectionDescription = descriptionInfos,
            TypeOfElection = politicalBusinessType == PoliticalBusinessType.ProportionalElection
                ? TypeOfElectionType.Item1
                : TypeOfElectionType.Item2,
        };
    }

    internal static ElectionAssociationType ToVoteInfoEchElectionAssociation<TPoliticalBusinessUnion>(this TPoliticalBusinessUnion union, decimal? quorum)
        where TPoliticalBusinessUnion : PoliticalBusinessUnion
    {
        return new()
        {
            ElectionAssociationId = union.Id.ToString(),
            ElectionAssociationName = union.Description,
            Quorum = quorum,
        };
    }
}
