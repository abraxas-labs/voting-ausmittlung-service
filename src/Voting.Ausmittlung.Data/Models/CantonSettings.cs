// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class CantonSettings : BaseEntity
{
    public DomainOfInfluenceCanton Canton { get; set; }

    public string SecureConnectId { get; set; } = string.Empty;

    public string AuthorityName { get; set; } = string.Empty;

    public CantonMajorityElectionAbsoluteMajorityAlgorithm MajorityElectionAbsoluteMajorityAlgorithm { get; set; }

    public bool MajorityElectionInvalidVotes { get; set; }

    public ProtocolDomainOfInfluenceSortType ProtocolDomainOfInfluenceSortType { get; set; }

    public ProtocolCountingCircleSortType ProtocolCountingCircleSortType { get; set; }

    public SwissAbroadVotingRight SwissAbroadVotingRight { get; set; }

    public List<DomainOfInfluenceType> SwissAbroadVotingRightDomainOfInfluenceTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the enabled voting card channels.
    /// This does never include E-Voting.
    /// </summary>
    public List<CantonSettingsVotingCardChannel> EnabledVotingCardChannels { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether counting circles can use counting machines or not.
    /// </summary>
    public bool CountingMachineEnabled { get; set; }

    public bool NewZhFeaturesEnabled { get; set; }

    public bool MajorityElectionUseCandidateCheckDigit { get; set; }

    public bool ProportionalElectionUseCandidateCheckDigit { get; set; }

    public List<CountingCircleResultStateDescription> CountingCircleResultStateDescriptions { get; set; } = new();

    public bool StatePlausibilisedDisabled { get; set; }

    public bool PublishResultsEnabled { get; set; }

    public bool PublishResultsBeforeAuditedTentatively { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether political business end results can be finalized via UI or not.
    /// If set to true, no user interaction can explicitly change finalized, it will always be implicitly finalized.
    /// </summary>
    public bool EndResultFinalizeDisabled { get; set; }
}
