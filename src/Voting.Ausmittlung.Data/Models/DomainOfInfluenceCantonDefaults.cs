// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class DomainOfInfluenceCantonDefaults
{
    public List<ProportionalElectionMandateAlgorithm> ProportionalElectionMandateAlgorithms { get; set; } = new();

    public CantonMajorityElectionAbsoluteMajorityAlgorithm MajorityElectionAbsoluteMajorityAlgorithm { get; set; }

    public bool MajorityElectionInvalidVotes { get; set; }

    public SwissAbroadVotingRight SwissAbroadVotingRight { get; set; }

    /// <summary>
    /// Gets or sets the enabled voting card channels.
    /// This does never include E-Voting.
    /// </summary>
    public ICollection<DomainOfInfluenceCantonDefaultsVotingCardChannel> EnabledVotingCardChannels { get; set; }
        = new HashSet<DomainOfInfluenceCantonDefaultsVotingCardChannel>();
}
