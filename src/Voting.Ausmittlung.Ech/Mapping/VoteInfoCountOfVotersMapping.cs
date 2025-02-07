// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0155_5_1;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoCountOfVotersMapping
{
    private const string MinorElementName = "ExtendedType";
    private const string MinorText = "Minderjährige";

    internal static CountOfVotersInformationType ToVoteInfoCountOfVotersInfo(this CountingCircle countingCircle, DomainOfInfluenceType doiType)
    {
        var totalCountOfVoters = countingCircle.ContestDetails.SingleOrDefault()?.TotalCountOfVoters ?? 0;
        var countOfVotersSubTotals = countingCircle.ContestDetails.SingleOrDefault()?.CountOfVotersInformationSubTotals
            .OrderBy(x => x.VoterType)
            .ThenBy(x => x.Sex);
        return new CountOfVotersInformationType
        {
            CountOfVotersTotal = (uint)totalCountOfVoters,
            SubtotalInfo = countOfVotersSubTotals?.Select(x => new CountOfVotersInformationTypeSubtotalInfo
            {
                CountOfVoters = (uint)(x.CountOfVoters ?? 0),
                VoterType = x.VoterType.ToVoterType(),
                Sex = x.Sex.ToSexType(),
                NamedElement = x.VoterType == VoterType.Minor ? ToMinorNamedElement() : null,
            }).ToList(),
        };
    }

    private static VoterTypeType? ToVoterType(this VoterType voterType)
    {
        return voterType switch
        {
            VoterType.Swiss => VoterTypeType.Item1,
            VoterType.SwissAbroad => VoterTypeType.Item2,
            VoterType.Foreigner => VoterTypeType.Item3,
            VoterType.Minor => null,
            _ => throw new InvalidOperationException("Invalid voterType"),
        };
    }

    private static Ech0044_4_1.SexType? ToSexType(this SexType sexType)
    {
        return sexType switch
        {
            SexType.Male => Ech0044_4_1.SexType.Item1,
            SexType.Female => Ech0044_4_1.SexType.Item2,
            _ => throw new InvalidOperationException("Invalid sexType"),
        };
    }

    private static List<NamedElementType> ToMinorNamedElement()
    {
        return
        [
            new NamedElementType() { ElementName = MinorElementName, Text = MinorText, }
        ];
    }
}
