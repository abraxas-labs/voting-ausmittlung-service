// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Ech0155_5_2;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Ech;
using ZhProportionalDivisor_1_0;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoProportionalElectionResultExtensionMapping
{
    internal static ExtensionType? ToVoteInfoEchProportionalElectionResultDeliveryExtension(
        this ICollection<ProportionalElection> proportionalElections,
        EchSerializer echSerializer)
    {
        var dpResults = proportionalElections
            .Select(pe => pe.DoubleProportionalResult)
            .Concat(proportionalElections
            .Where(pe => pe.ProportionalElectionUnionEntries != null)
            .SelectMany(pe => pe.ProportionalElectionUnionEntries
                .Select(ue => ue.ProportionalElectionUnion.DoubleProportionalResult)
                .WhereNotNull()))
            .WhereNotNull()
            .DistinctBy(dp => dp.Id)
            .OrderBy(dp => dp.MandateAlgorithm)
            .ThenBy(dp => dp.ProportionalElectionUnionId)
            .ThenBy(dp => dp.ProportionalElectionId)
            .ToList();

        if (dpResults.Count == 0)
        {
            return null;
        }

        var divisors = dpResults.ConvertAll(r => r.ToVoteInfoEchProportionalDivisor());
        var extension = new ExtensionType();

        foreach (var divisor in divisors)
        {
            extension.Any.Add(echSerializer.Serialize(divisor));
        }

        return extension;
    }

    private static ProportionalDivisor ToVoteInfoEchProportionalDivisor(this DoubleProportionalResult dpResult)
    {
        var isUnion = dpResult.ProportionalElectionUnionId.HasValue;
        var divisor = new ProportionalDivisor();

        if (!isUnion)
        {
            divisor.ElectionGroup =
            [
                new ElectionGroupType
                {
                    ElectionGroupIdentification = dpResult.ProportionalElectionId.ToString(),
                    Divisor = dpResult.ElectionKeyCalculation.ToVoteInfoEchExtensionDivisor(),
                }
            ];

            return divisor;
        }

        divisor.ElectionAssociation = new ElectionAssociationType
        {
            ElectionAssociationId = dpResult.ProportionalElectionUnionId.ToString(),
            Divisor = dpResult.ElectionKeyCalculation.ToVoteInfoEchExtensionDivisor(),
            ListGroupDivisor = dpResult.Columns
                .OrderBy(column => column.UnionListId)
                .Select(column => new ListGroupDivisorType()
                {
                    ListIdentification = column.UnionListId!.Value.ToString(),
                    Value = column.DivisorCalculation.SelectDivisor.ToVoteInfoEchExtensionDivisorValue(),
                    UpperLimit = column.DivisorCalculation.UpperBoundary.ToVoteInfoEchExtensionDivisorValue(),
                    LowerLimit = column.DivisorCalculation.LowerBoundary.ToVoteInfoEchExtensionDivisorValue(),
                }).ToList(),
        };

        divisor.ElectionGroup = dpResult.Rows
            .OrderBy(row => row.ProportionalElectionId)
            .Select(row => new ElectionGroupType
            {
                ElectionGroupIdentification = row.ProportionalElectionId.ToString(),
                DistrictDivisor = row.DivisorCalculation.ToVoteInfoEchExtensionDivisor(),
            }).ToList();

        return divisor;
    }

    private static DivisorType ToVoteInfoEchExtensionDivisor(this SelectDivisorCalculation calculation)
    {
        return new()
        {
            Value = calculation.SelectDivisor.ToVoteInfoEchExtensionDivisorValue(),
            UpperLimit = calculation.UpperBoundary.ToVoteInfoEchExtensionDivisorValue(),
            LowerLimit = calculation.LowerBoundary.ToVoteInfoEchExtensionDivisorValue(),
        };
    }

    private static DivisorValueType ToVoteInfoEchExtensionDivisorValue(this RationalNumber rationalNumber)
    {
        if (long.TryParse(rationalNumber.Dividend, out var dividend) && long.TryParse(rationalNumber.Divisor, out var divisor))
        {
            return new()
            {
                Dividend = dividend,
                Divisor = divisor,
            };
        }

        return new() { Quotient = rationalNumber.Quotient };
    }
}
