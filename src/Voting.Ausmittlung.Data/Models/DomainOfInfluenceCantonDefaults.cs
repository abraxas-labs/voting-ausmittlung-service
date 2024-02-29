// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class DomainOfInfluenceCantonDefaults
{
    public CantonMajorityElectionAbsoluteMajorityAlgorithm MajorityElectionAbsoluteMajorityAlgorithm { get; set; }

    public bool MajorityElectionInvalidVotes { get; set; }

    public ProtocolDomainOfInfluenceSortType ProtocolDomainOfInfluenceSortType { get; set; }

    public ProtocolCountingCircleSortType ProtocolCountingCircleSortType { get; set; }

    public SwissAbroadVotingRight SwissAbroadVotingRight { get; set; }

    /// <summary>
    /// Gets or sets the enabled voting card channels.
    /// This does never include E-Voting.
    /// </summary>
    public ICollection<DomainOfInfluenceCantonDefaultsVotingCardChannel> EnabledVotingCardChannels { get; set; }
        = new HashSet<DomainOfInfluenceCantonDefaultsVotingCardChannel>();

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can use counting machines or not.
    /// </summary>
    public bool CountingMachineEnabled { get; set; }

    public bool NewZhFeaturesEnabled { get; set; }

    public bool MajorityElectionUseCandidateCheckDigit { get; set; }

    public bool ProportionalElectionUseCandidateCheckDigit { get; set; }
}
