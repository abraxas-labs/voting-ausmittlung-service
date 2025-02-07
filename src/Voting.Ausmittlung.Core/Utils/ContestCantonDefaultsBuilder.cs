// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Data.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ContestCantonDefaultsBuilder
{
    private readonly CantonSettingsRepo _cantonSettingsRepo;
    private readonly ContestRepo _contestRepo;
    private readonly DataContext _dataContext;

    public ContestCantonDefaultsBuilder(
        CantonSettingsRepo cantonSettingsRepo,
        ContestRepo contestRepo,
        DataContext dataContext)
    {
        _cantonSettingsRepo = cantonSettingsRepo;
        _contestRepo = contestRepo;
        _dataContext = dataContext;
    }

    public async Task BuildForContest(Contest contest, DomainOfInfluenceCanton canton)
    {
        var cantonSettings = await LoadCantonSettings(canton);
        BuildCantonDefaultsOnContest(cantonSettings, contest);
    }

    public async Task RebuildForCanton(CantonSettings cantonSettings)
    {
        var contests = await _contestRepo.Query()
            .AsSplitQuery()
            .AsTracking()
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.CantonDefaults)
            .WhereInTestingPhase()
            .Where(x => x.DomainOfInfluence.Canton == cantonSettings.Canton)
            .ToListAsync();

        foreach (var contest in contests)
        {
            BuildCantonDefaultsOnContest(cantonSettings, contest);
        }

        await _dataContext.SaveChangesAsync();
    }

    private void BuildCantonDefaultsOnContest(CantonSettings cantonSettings, Contest contest)
    {
        contest.CantonDefaults = new ContestCantonDefaults
        {
            CountingCircleResultStateDescriptions = cantonSettings.CountingCircleResultStateDescriptions
                .ConvertAll(x => new ContestCantonDefaultsCountingCircleResultStateDescription { State = x.State, Description = x.Description }),
            StatePlausibilisedDisabled = cantonSettings.StatePlausibilisedDisabled,
            MajorityElectionAbsoluteMajorityAlgorithm = cantonSettings.MajorityElectionAbsoluteMajorityAlgorithm,
            ProtocolDomainOfInfluenceSortType = cantonSettings.ProtocolDomainOfInfluenceSortType,
            ProtocolCountingCircleSortType = cantonSettings.ProtocolCountingCircleSortType,
            EnabledVotingCardChannels = cantonSettings.EnabledVotingCardChannels
                .ConvertAll(x => new DomainOfInfluenceCantonDefaultsVotingCardChannel { Valid = x.Valid, VotingChannel = x.VotingChannel }),
            MajorityElectionInvalidVotes = cantonSettings.MajorityElectionInvalidVotes,
            CountingMachineEnabled = cantonSettings.CountingMachineEnabled,
            MajorityElectionUseCandidateCheckDigit = cantonSettings.MajorityElectionUseCandidateCheckDigit,
            ProportionalElectionUseCandidateCheckDigit = cantonSettings.ProportionalElectionUseCandidateCheckDigit,
            ManualPublishResultsEnabled = cantonSettings.ManualPublishResultsEnabled,
            EndResultFinalizeDisabled = cantonSettings.EndResultFinalizeDisabled,
            PublishResultsBeforeAuditedTentatively = cantonSettings.PublishResultsBeforeAuditedTentatively,
        };
    }

    private async Task<CantonSettings> LoadCantonSettings(DomainOfInfluenceCanton canton)
    {
        return await _cantonSettingsRepo.GetByDomainOfInfluenceCanton(canton)
            ?? new CantonSettings();
    }
}
