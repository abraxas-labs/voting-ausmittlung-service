// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.BiproportionalApportionment;
using Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;
using Xunit;

namespace Voting.Ausmittlung.Test.BiproportionalApportionmentTests.TieAndTransfer;

public class VectorApportionmentMethodTest
{
    private readonly VectorApportionmentMethod _vectorApportionmentMethod = new();

    [Theory]
    [InlineData(0, new[] { 0, 2, 0, 1, 1, 2, 2, 1, 5, 4, 3, 6, 2, 3, 2, 1, 7, 3, }, 19033.3)]
    [InlineData(1, new[] { 1, 4, 1, 3, 1, 3, 1, 1, 3, 2, 1, 3, 1, 4, 1, 0, 4, 1 }, 19038.4)]
    [InlineData(2, new[] { 0, 1, 0, 1, 1, 1, 1, 1, 5, 4, 2, 3, 1, 2, 1, 0, 4, 1 }, 17817.8)]
    public void TestKantonratswahl2019(int listIndex, int[] expectedApportionment, decimal expectedMaxDivisor)
    {
        var kt = ZhKantonratswahlTestData.Kantonratswahl2019();
        var weights = new Weight[kt.ElectionNumberOfMandates.Length];

        for (var i = 0; i < kt.ElectionNumberOfMandates.Length; i++)
        {
            weights[i] = kt.Weights[i][listIndex];
        }

        var numberOfMandates = kt.UnionListNumberOfMandates[listIndex];
        var result = _vectorApportionmentMethod.Calculate(weights, numberOfMandates);

        result.Apportionment.SequenceEqual(expectedApportionment)
            .Should()
            .BeTrue();

        ((decimal)result.MaxDivisor).ApproxEquals(expectedMaxDivisor, 2);
    }
}
