// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class CantonSettings : BaseEntity
{
    public DomainOfInfluenceCanton Canton { get; set; }

    public string SecureConnectId { get; set; } = string.Empty;

    public string AuthorityName { get; set; } = string.Empty;

    public List<ProportionalElectionMandateAlgorithm> ProportionalElectionMandateAlgorithms { get; set; } = new();

    public CantonMajorityElectionAbsoluteMajorityAlgorithm MajorityElectionAbsoluteMajorityAlgorithm { get; set; }

    public bool MajorityElectionInvalidVotes { get; set; }

    public SwissAbroadVotingRight SwissAbroadVotingRight { get; set; }

    public List<DomainOfInfluenceType> SwissAbroadVotingRightDomainOfInfluenceTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the enabled voting card channels.
    /// This does never include E-Voting.
    /// </summary>
    public List<CantonSettingsVotingCardChannel> EnabledVotingCardChannels { get; set; } = new();
}
