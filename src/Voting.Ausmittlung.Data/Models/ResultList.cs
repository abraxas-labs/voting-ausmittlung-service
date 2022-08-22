// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public class ResultList
{
    private static readonly DomainOfInfluenceCantonDefaultsVotingCardChannel EVotingVotingCardChannel =
        new() { Valid = true, VotingChannel = VotingChannel.EVoting };

    public ResultList(
        Contest contest,
        CountingCircle countingCircle,
        ContestCountingCircleDetails? details,
        List<SimpleCountingCircleResult> results,
        bool currentTenantIsResponsible,
        Guid? contestCountingCircleContactPersonId,
        bool mustUpdateContactPersons)
    {
        Contest = contest;
        CountingCircle = countingCircle;
        Details = details;
        Results = results;
        CurrentTenantIsResponsible = currentTenantIsResponsible;
        ContestCountingCircleContactPersonId = contestCountingCircleContactPersonId;
        MustUpdateContactPersons = mustUpdateContactPersons;
    }

    public Contest Contest { get; }

    public CountingCircle CountingCircle { get; }

    public ContestCountingCircleDetails? Details { get; }

    public List<SimpleCountingCircleResult> Results { get; }

    public bool CurrentTenantIsResponsible { get; }

    public Guid? ContestCountingCircleContactPersonId { get; }

    public bool MustUpdateContactPersons { get; }

    public bool HasUnmappedEVotingWriteIns => Results.OfType<MajorityElectionResult>().Any(x => x.HasUnmappedWriteIns);

    public CountingCircleResultState State =>
        Results.Count == 0
            ? CountingCircleResultState.Initial
            : Results.Min(r => r.State);

    public bool SwissAbroadHaveVotingRightsOnAnyBusiness =>
        Results.Any(r =>
            r.PoliticalBusiness!.SwissAbroadVotingRight == SwissAbroadVotingRight.OnEveryCountingCircle);

    /// <summary>
    /// Gets the enabled voting card channels.
    /// This includes E-Voting, if enabled on the contest.
    /// </summary>
    public IEnumerable<DomainOfInfluenceCantonDefaultsVotingCardChannel> EnabledVotingCardChannels
        => Contest.EVoting
        ? Contest.DomainOfInfluence.CantonDefaults.EnabledVotingCardChannels.Append(EVotingVotingCardChannel).OrderByPriority()
        : Contest.DomainOfInfluence.CantonDefaults.EnabledVotingCardChannels.OrderByPriority();
}
