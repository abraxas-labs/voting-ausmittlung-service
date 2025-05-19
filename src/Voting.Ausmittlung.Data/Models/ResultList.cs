// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Data.Models;

public class ResultList
{
    private static readonly DomainOfInfluenceCantonDefaultsVotingCardChannel EVotingVotingCardChannel =
        new() { Valid = true, VotingChannel = VotingChannel.EVoting };

    public ResultList(
        Contest contest,
        CountingCircle countingCircle,
        ContestCountingCircleDetails details,
        ContestCountingCircleElectorateSummary electorateSummary,
        List<SimpleCountingCircleResult> results,
        bool currentTenantIsResponsible,
        Guid? contestCountingCircleContactPersonId,
        bool mustUpdateContactPersons)
    {
        Contest = contest;
        CountingCircle = countingCircle;
        Details = details;
        ElectorateSummary = electorateSummary;
        Results = results;
        CurrentTenantIsResponsible = currentTenantIsResponsible;
        ContestCountingCircleContactPersonId = contestCountingCircleContactPersonId;
        MustUpdateContactPersons = mustUpdateContactPersons;
    }

    public Contest Contest { get; }

    public CountingCircle CountingCircle { get; }

    public ContestCountingCircleDetails Details { get; }

    public ContestCountingCircleElectorateSummary ElectorateSummary { get; }

    public List<SimpleCountingCircleResult> Results { get; }

    public bool CurrentTenantIsResponsible { get; }

    public Guid? ContestCountingCircleContactPersonId { get; }

    public bool MustUpdateContactPersons { get; }

    public bool HasUnmappedEVotingWriteIns => Results.Any(x => x.HasUnmappedEVotingWriteIns);

    public bool HasUnmappedECountingWriteIns => Results.Any(x => x.HasUnmappedECountingWriteIns);

    public bool HasUnmappedWriteIns => HasUnmappedEVotingWriteIns || HasUnmappedECountingWriteIns;

    public CountingCircleResultState State =>
        Results.Count == 0
            ? CountingCircleResultState.Initial
            : Results.Min(r => r.State);

    public IEnumerable<VoterType> EnabledVoterTypes =>
        VoterTypesBuilder.BuildEnabledVoterTypes(Results.ConvertAll(r => r.PoliticalBusiness!.DomainOfInfluence));

    /// <summary>
    /// Gets the enabled voting card channels.
    /// This includes E-Voting, if enabled on the counting circle.
    /// </summary>
    public IEnumerable<DomainOfInfluenceCantonDefaultsVotingCardChannel> EnabledVotingCardChannels
        => Details.EVoting
        ? Contest.CantonDefaults.EnabledVotingCardChannels.Append(EVotingVotingCardChannel).OrderByPriority()
        : Contest.CantonDefaults.EnabledVotingCardChannels.OrderByPriority();
}
