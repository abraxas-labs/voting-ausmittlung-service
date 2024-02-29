// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class ProtocolSortUtilTest
{
    [Theory]
    [InlineData(ProtocolDomainOfInfluenceSortType.SortNumber, new int[] { 5, 4, 3, 2, 1 })]
    [InlineData(ProtocolDomainOfInfluenceSortType.Alphabetical, new int[] { 4, 5, 1, 3, 2 })]
    public void ShouldReturnOrderByDomainOfInfluence(ProtocolDomainOfInfluenceSortType sortType, int[] expectedResult)
    {
        var list = new List<DomainOfInfluenceWrapper>
        {
            new DomainOfInfluenceWrapper("Horn", 4, 1),
            new DomainOfInfluenceWrapper("Wil", 3, 2),
            new DomainOfInfluenceWrapper("Uzwil", 2, 3),
            new DomainOfInfluenceWrapper("Arnegg", 2, 4),
            new DomainOfInfluenceWrapper("Gossau", 1, 5),
        };

        list = list
            .OrderByDomainOfInfluence(
                x => x.DomainOfInfluence,
                new DomainOfInfluenceCantonDefaults { ProtocolDomainOfInfluenceSortType = sortType })
            .ToList();

        list.Select(x => x.Data).SequenceEqual(expectedResult).Should().BeTrue();
    }

    [Theory]
    [InlineData(ProtocolCountingCircleSortType.SortNumber, new int[] { 5, 4, 3, 2, 1 })]
    [InlineData(ProtocolCountingCircleSortType.Alphabetical, new int[] { 4, 5, 1, 3, 2 })]
    public void ShouldReturnOrderByCountingCircleWithSortType(ProtocolCountingCircleSortType sortType, int[] expectedResult)
    {
        var list = new List<CountingCircleWrapper>
        {
            new CountingCircleWrapper("Horn", 4, 1),
            new CountingCircleWrapper("Wil", 3, 2),
            new CountingCircleWrapper("Uzwil", 2, 3),
            new CountingCircleWrapper("Arnegg", 2, 4),
            new CountingCircleWrapper("Gossau", 1, 5),
        };

        list = list
            .OrderByCountingCircle(
                x => x.CountingCircle,
                new DomainOfInfluenceCantonDefaults { ProtocolCountingCircleSortType = sortType })
            .ToList();

        list.Select(x => x.Data).SequenceEqual(expectedResult).Should().BeTrue();
    }

    private class DomainOfInfluenceWrapper
    {
        public DomainOfInfluenceWrapper(string name, int sortNumber, int data)
        {
            DomainOfInfluence = new()
            {
                Name = name,
                SortNumber = sortNumber,
            };
            Data = data;
        }

        public DomainOfInfluence DomainOfInfluence { get; }

        public int Data { get; }
    }

    private class CountingCircleWrapper
    {
        public CountingCircleWrapper(string name, int sortNumber, int data)
        {
            CountingCircle = new()
            {
                Name = name,
                SortNumber = sortNumber,
            };
            Data = data;
        }

        public CountingCircle CountingCircle { get; }

        public int Data { get; }
    }
}
