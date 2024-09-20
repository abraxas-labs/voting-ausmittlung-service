// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Test.UtilsTest.DoubleProportional;

public class DoubleProportionalAlgorithmUnionTestResult
{
    public DoubleProportionalAlgorithmUnionTestResult(
        int cantonalQuorum,
        int voteCount,
        int totalNumberOfMandates,
        int[] unionListVoteCounts,
        int[] electionVoteCounts,
        int[] electionQuorums,
        bool[] unionListAnyQuorumReachedList,
        decimal electionKey,
        decimal totalVoterNumber,
        int superApportionmentNumberOfMandates,
        int[] unionListSuperApportionmentNumberOfMandates,
        decimal[] unionListVoterNumbers,
        int[][] subApportionment,
        decimal[] electionDivisors,
        decimal[] unionListDivisors)
    {
        CantonalQuorum = cantonalQuorum;
        VoteCount = voteCount;
        TotalNumberOfMandates = totalNumberOfMandates;
        UnionListVoteCounts = unionListVoteCounts;
        ElectionVoteCounts = electionVoteCounts;
        ElectionQuorums = electionQuorums;
        UnionListAnyQuorumReachedList = unionListAnyQuorumReachedList;
        ElectionKey = electionKey;
        TotalVoterNumber = totalVoterNumber;
        SuperApportionmentNumberOfMandates = superApportionmentNumberOfMandates;
        UnionListSuperApportionmentNumberOfMandates = unionListSuperApportionmentNumberOfMandates;
        UnionListVoterNumbers = unionListVoterNumbers;
        SubApportionment = subApportionment;
        ElectionDivisors = electionDivisors;
        UnionListDivisors = unionListDivisors;
    }

    public int CantonalQuorum { get; }

    public int VoteCount { get; }

    public int TotalNumberOfMandates { get; }

    public int[] UnionListVoteCounts { get; }

    public int[] ElectionVoteCounts { get; }

    public int[] ElectionQuorums { get; }

    public bool[] UnionListAnyQuorumReachedList { get; }

    public decimal ElectionKey { get; }

    public decimal TotalVoterNumber { get; }

    public int SuperApportionmentNumberOfMandates { get; }

    public int[] UnionListSuperApportionmentNumberOfMandates { get; }

    public decimal[] UnionListVoterNumbers { get; }

    public int[][] SubApportionment { get; }

    public decimal[] ElectionDivisors { get; }

    public decimal[] UnionListDivisors { get; }
}
