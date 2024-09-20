// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Test.UtilsTest.DoubleProportional;

public class DoubleProportionalAlgorithmElectionTestResult
{
    public DoubleProportionalAlgorithmElectionTestResult(
        int voteCount,
        int totalNumberOfMandates,
        int[] listVoteCounts,
        decimal electionKey,
        decimal totalVoterNumber,
        int superApportionmentNumberOfMandates,
        int[] listSuperApportionmentNumberOfMandates,
        decimal[] listVoterNumbers)
    {
        VoteCount = voteCount;
        TotalNumberOfMandates = totalNumberOfMandates;
        ListVoteCounts = listVoteCounts;
        ElectionKey = electionKey;
        TotalVoterNumber = totalVoterNumber;
        SuperApportionmentNumberOfMandates = superApportionmentNumberOfMandates;
        ListSuperApportionmentNumberOfMandates = listSuperApportionmentNumberOfMandates;
        ListVoterNumbers = listVoterNumbers;
    }

    public int VoteCount { get; }

    public int TotalNumberOfMandates { get; }

    public int[] ListVoteCounts { get; }

    public decimal ElectionKey { get; }

    public decimal TotalVoterNumber { get; }

    public int SuperApportionmentNumberOfMandates { get; }

    public int[] ListSuperApportionmentNumberOfMandates { get; }

    public decimal[] ListVoterNumbers { get; }
}
