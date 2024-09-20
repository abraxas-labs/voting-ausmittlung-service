// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.BiproportionalApportionment;

namespace Voting.Ausmittlung.Test.BiproportionalApportionmentTests;

public class BiproportionalTestData
{
    public BiproportionalTestData(int[][] voteCountMatrix, int[] electionNumberOfMandates, int[] unionListNumberOfMandates, string[] listDescriptions)
    {
        var weights = new Weight[electionNumberOfMandates.Length][];
        for (var i = 0; i < voteCountMatrix.Length; i++)
        {
            weights[i] = new Weight[voteCountMatrix[0].Length];

            for (var j = 0; j < voteCountMatrix[0].Length; j++)
            {
                weights[i][j] = new Weight()
                {
                    VoteCount = voteCountMatrix[i][j],
                    Name = listDescriptions[j],
                };
            }
        }

        Weights = weights;
        ElectionNumberOfMandates = electionNumberOfMandates;
        UnionListNumberOfMandates = unionListNumberOfMandates;
    }

    public Weight[][] Weights { get; }

    public int[] ElectionNumberOfMandates { get; }

    public int[] UnionListNumberOfMandates { get; }
}
