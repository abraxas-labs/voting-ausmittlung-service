// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Extensions;

public static class ProportionalElectionMandateAlgorithmExtensions
{
    public static bool IsDoubleProportional(this ProportionalElectionMandateAlgorithm mandateAlgorithm)
        => mandateAlgorithm is ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum
            or ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum
            or ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum;

    public static bool IsUnionDoubleProportional(this ProportionalElectionMandateAlgorithm mandateAlgorithm)
        => mandateAlgorithm is ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum
        or ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum;

    public static bool IsNonUnionDoubleProportional(this ProportionalElectionMandateAlgorithm mandateAlgorithm)
        => mandateAlgorithm is ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum;
}
