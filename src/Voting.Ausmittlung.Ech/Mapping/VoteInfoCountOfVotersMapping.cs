// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Ech0155_5_2;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using DomainOfInfluence = Voting.Ausmittlung.Data.Models.DomainOfInfluence;
using SexType = Ech0044_4_1.SexType;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoCountOfVotersMapping
{
    internal static CountOfVotersInformationType ToVoteInfoCountOfVotersInfo(this CountingCircle countingCircle, DomainOfInfluence doi)
    {
        var totalCountOfVoters = countingCircle.ContestDetails.SingleOrDefault()?.GetTotalCountOfVotersForDomainOfInfluence(doi) ?? 0;
        var countOfVotersSubTotals = countingCircle.ContestDetails.SingleOrDefault()?.CountOfVotersInformationSubTotals
            .Where(x => x.DomainOfInfluenceType == doi.Type)
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
                IsMinor = x.VoterType == VoterType.Minor ? true : null,
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

    private static SexType? ToSexType(this Data.Models.SexType sexType)
    {
        return sexType switch
        {
            Data.Models.SexType.Male => SexType.Item1,
            Data.Models.SexType.Female => SexType.Item2,
            _ => throw new InvalidOperationException("Invalid sexType"),
        };
    }
}
