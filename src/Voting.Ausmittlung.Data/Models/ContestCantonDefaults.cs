// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ContestCantonDefaults : BaseEntity
{
    public Guid ContestId { get; set; }

    public Contest? Contest { get; set; }

    public ICollection<ContestCantonDefaultsCountingCircleResultStateDescription> CountingCircleResultStateDescriptions { get; set; }
        = new HashSet<ContestCantonDefaultsCountingCircleResultStateDescription>();

    public bool StatePlausibilisedDisabled { get; set; }

    public CantonMajorityElectionAbsoluteMajorityAlgorithm MajorityElectionAbsoluteMajorityAlgorithm { get; set; }

    public ProtocolDomainOfInfluenceSortType ProtocolDomainOfInfluenceSortType { get; set; }

    public ProtocolCountingCircleSortType ProtocolCountingCircleSortType { get; set; }

    /// <summary>
    /// Gets or sets the enabled voting card channels.
    /// This does never include E-Voting.
    /// </summary>
    public ICollection<DomainOfInfluenceCantonDefaultsVotingCardChannel> EnabledVotingCardChannels { get; set; }
        = new HashSet<DomainOfInfluenceCantonDefaultsVotingCardChannel>();

    public bool MajorityElectionInvalidVotes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can use counting machines or not.
    /// </summary>
    public bool CountingMachineEnabled { get; set; }

    public bool MajorityElectionUseCandidateCheckDigit { get; set; }

    public bool ProportionalElectionUseCandidateCheckDigit { get; set; }

    public bool ManualPublishResultsEnabled { get; set; }

    public bool PublishResultsBeforeAuditedTentatively { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether political business end results can be finalized via UI or not.
    /// If set to true, no user interaction can explicitly change finalized, it will always be implicitly finalized.
    /// </summary>
    public bool EndResultFinalizeDisabled { get; set; }
}
