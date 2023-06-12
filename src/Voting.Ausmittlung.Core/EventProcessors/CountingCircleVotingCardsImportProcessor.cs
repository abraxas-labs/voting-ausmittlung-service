// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class CountingCircleVotingCardsImportProcessor : IEventProcessor<CountingCircleVotingCardsImported>
{
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _repo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly AggregatedContestCountingCircleDetailsBuilder _aggregatedContestCountingCircleDetailsBuilder;
    private readonly IDbRepository<DataContext, ResultImport> _resultImportRepo;

    public CountingCircleVotingCardsImportProcessor(
        IDbRepository<DataContext, ContestCountingCircleDetails> repo,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        AggregatedContestCountingCircleDetailsBuilder aggregatedContestCountingCircleDetailsBuilder,
        IDbRepository<DataContext, ResultImport> resultImportRepo)
    {
        _repo = repo;
        _countingCircleRepo = countingCircleRepo;
        _contestRepo = contestRepo;
        _aggregatedContestCountingCircleDetailsBuilder = aggregatedContestCountingCircleDetailsBuilder;
        _resultImportRepo = resultImportRepo;
    }

    public async Task Process(CountingCircleVotingCardsImported eventData)
    {
        var contestId = GuidParser.Parse(eventData.ContestId);

        var testingPhaseEnded = await _contestRepo
            .Query()
            .Where(x => x.Id == contestId)
            .Select(x => x.TestingPhaseEnded)
            .FirstAsync();

        var votingCardsByCcDetailsId = eventData.CountingCircleVotingCards
            .ToDictionary(x => AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, Guid.Parse(x.CountingCircleId), testingPhaseEnded));
        var votingCardDetailIds = votingCardsByCcDetailsId.Keys.ToHashSet();

        // The counting circle details are guaranteed to always exist, as they are initialized with empty values
        var existingCcDetails = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards.Where(vc => vc.Channel == VotingChannel.EVoting))
            .Where(x => votingCardDetailIds.Contains(x.Id))
            .ToListAsync();

        if (existingCcDetails.Count != votingCardsByCcDetailsId.Count)
        {
            throw new InvalidOperationException("Could not find details for all imported voting cards");
        }

        // The imported voting cards do not have a domain of influence type, as this information is not available during the import
        // As a workaround, we assign the imported voting cards to all necessary DOI types on the corresponding counting circles.
        var countingCircleIds = existingCcDetails.ConvertAll(x => x.CountingCircleId);
        var domainOfInfluenceTypesByCcId = await _countingCircleRepo
            .Query()
            .Where(cc => countingCircleIds.Contains(cc.Id))
            .Include(x => x.SimpleResults).ThenInclude(x => x.PoliticalBusiness!).ThenInclude(x => x.DomainOfInfluence)
            .Select(x => new
            {
                x.Id,
                DomainOfInfluenceTypes = x.SimpleResults.Select(sr => sr.PoliticalBusiness!.DomainOfInfluence.Type).ToHashSet(),
            })
            .ToDictionaryAsync(x => x.Id, x => x.DomainOfInfluenceTypes);

        foreach (var details in existingCcDetails)
        {
            var receivedVotingCards = votingCardsByCcDetailsId[details.Id].CountOfReceivedVotingCards;
            var eVotingVotingCardsByDoiType = details.VotingCards
                .Where(x => x.Channel == VotingChannel.EVoting)
                .ToDictionary(x => x.DomainOfInfluenceType);

            if (!domainOfInfluenceTypesByCcId.TryGetValue(details.CountingCircleId, out var domainOfInfluenceTypes))
            {
                throw new InvalidOperationException($"Could not find the domain of influence types for counting circle {details.CountingCircleId}");
            }

            foreach (var domainOfInfluenceType in domainOfInfluenceTypes)
            {
                if (eVotingVotingCardsByDoiType.TryGetValue(domainOfInfluenceType, out var votingCards))
                {
                    votingCards.CountOfReceivedVotingCards = receivedVotingCards;
                }
                else
                {
                    details.VotingCards.Add(new VotingCardResultDetail
                    {
                        Channel = VotingChannel.EVoting,
                        Valid = true,
                        DomainOfInfluenceType = domainOfInfluenceType,
                        CountOfReceivedVotingCards = receivedVotingCards,
                    });
                }
            }
        }

        await _repo.UpdateRange(existingCcDetails);
        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedVotingCards(contestId, existingCcDetails, false);

        await TrackImportedCountingCircles(GuidParser.Parse(eventData.ImportId), existingCcDetails.Select(x => x.CountingCircleId));
    }

    private async Task TrackImportedCountingCircles(Guid importId, IEnumerable<Guid> countingCircleIds)
    {
        var import = await _resultImportRepo.GetByKey(importId)
            ?? throw new EntityNotFoundException(nameof(ResultImport), importId);

        import.ImportedCountingCircles = countingCircleIds
            .Select(ccId => new ResultImportCountingCircle { CountingCircleId = ccId })
            .ToList();

        await _resultImportRepo.Update(import);
    }
}
