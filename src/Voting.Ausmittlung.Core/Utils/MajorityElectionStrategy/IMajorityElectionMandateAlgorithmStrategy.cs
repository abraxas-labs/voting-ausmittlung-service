// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public interface IMajorityElectionMandateAlgorithmStrategy
{
    MajorityElectionMandateAlgorithm MandateAlgorithm { get; }

    /// <summary>
    /// Gets the absolute majority algorithm of which this is the implementation for the provided <see cref="MandateAlgorithm"/> when it has absolute majority mandates
    /// If null, this is the default implementation for the provided <see cref="MandateAlgorithm"/>.
    /// </summary>
    CantonMajorityElectionAbsoluteMajorityAlgorithm? AbsoluteMajorityAlgorithm { get; }

    void RecalculateCandidateEndResultStates(MajorityElectionEndResult majorityElectionEndResult);
}
