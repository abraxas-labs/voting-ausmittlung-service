// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class MajorityElectionBallotGroupProcessor :
    IEventProcessor<MajorityElectionBallotGroupCreated>,
    IEventProcessor<MajorityElectionBallotGroupUpdated>,
    IEventProcessor<MajorityElectionBallotGroupDeleted>,
    IEventProcessor<MajorityElectionBallotGroupCandidatesUpdated>
{
    private readonly IDbRepository<DataContext, MajorityElectionBallotGroup> _repo;
    private readonly IDbRepository<DataContext, MajorityElectionBallotGroupEntry> _entryRepo;
    private readonly MajorityElectionBallotGroupResultBuilder _ballotGroupResultBuilder;
    private readonly IMapper _mapper;

    public MajorityElectionBallotGroupProcessor(
        IDbRepository<DataContext, MajorityElectionBallotGroup> repo,
        IDbRepository<DataContext, MajorityElectionBallotGroupEntry> entryRepo,
        MajorityElectionBallotGroupResultBuilder ballotGroupResultBuilder,
        IMapper mapper)
    {
        _repo = repo;
        _entryRepo = entryRepo;
        _ballotGroupResultBuilder = ballotGroupResultBuilder;
        _mapper = mapper;
    }

    public async Task Process(MajorityElectionBallotGroupCreated eventData)
    {
        var model = _mapper.Map<MajorityElectionBallotGroup>(eventData.BallotGroup);
        await _repo.Create(model);
        await _ballotGroupResultBuilder.Initialize(model.MajorityElectionId, model.Id);
        await UpdateAllCandidateCountsOk(model.Id);
    }

    public async Task Process(MajorityElectionBallotGroupUpdated eventData)
    {
        var model = _mapper.Map<MajorityElectionBallotGroup>(eventData.BallotGroup);

        var existingBallotGroup = await _repo.Query()
            .AsTracking()
            .Include(bg => bg.Entries)
            .FirstOrDefaultAsync(bg => bg.Id == model.Id)
            ?? throw new EntityNotFoundException(model.Id);

        existingBallotGroup.Description = model.Description;
        existingBallotGroup.ShortDescription = model.ShortDescription;

        var existingBallotGroupEntries = existingBallotGroup.Entries.ToDictionary(x => x.Id);

        foreach (var entry in model.Entries)
        {
            if (!existingBallotGroupEntries.TryGetValue(entry.Id, out var existingEntry))
            {
                entry.BallotGroupId = existingBallotGroup.Id;
                await _entryRepo.Create(entry);
                existingBallotGroup.Entries.Add(entry);
            }
            else
            {
                // When the blank row count is used (old version) this field is set in the BallotGroupUpdated event instead of the BallotGroupCandidatesUpdated event.
                if (!eventData.BallotGroup.BlankRowCountUnused)
                {
                    existingEntry.BlankRowCount = entry.BlankRowCount;
                }
            }
        }

        await _repo.Update(existingBallotGroup);
        await UpdateAllCandidateCountsOk(model.Id);
    }

    public async Task Process(MajorityElectionBallotGroupDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.BallotGroupId);

        var existingBallotGroup = await _repo.GetByKey(id);
        if (existingBallotGroup == null)
        {
            throw new EntityNotFoundException(id);
        }

        await _repo.DeleteByKey(id);

        var ballotGroupsToUpdate = await _repo.Query()
            .Where(bg => bg.MajorityElectionId == existingBallotGroup.MajorityElectionId
                && bg.Position > existingBallotGroup.Position)
            .ToListAsync();

        foreach (var ballotGroup in ballotGroupsToUpdate)
        {
            ballotGroup.Position--;
        }

        await _repo.UpdateRange(ballotGroupsToUpdate);
    }

    public async Task Process(MajorityElectionBallotGroupCandidatesUpdated eventData)
    {
        var ballotGroupId = GuidParser.Parse(eventData.BallotGroupCandidates.BallotGroupId);
        var entryIds = eventData.BallotGroupCandidates.EntryCandidates.Select(e => GuidParser.Parse(e.BallotGroupEntryId)).ToList();
        var entryCandidates = eventData.BallotGroupCandidates.EntryCandidates.ToDictionary(
            e => GuidParser.Parse(e.BallotGroupEntryId),
            e => e.CandidateIds.Select(GuidParser.Parse).ToList());
        var individualCandidatesVoteCountByEntryId = eventData.BallotGroupCandidates.EntryCandidates.ToDictionary(
            e => GuidParser.Parse(e.BallotGroupEntryId),
            e => e.IndividualCandidatesVoteCount);
        var blankRowCountByEntryId = eventData.BallotGroupCandidates.EntryCandidates.ToDictionary(
            e => GuidParser.Parse(e.BallotGroupEntryId),
            e => e.BlankRowCount);
        await _ballotGroupResultBuilder.UpdateCandidates(entryIds, individualCandidatesVoteCountByEntryId, blankRowCountByEntryId, entryCandidates);
        await UpdateAllCandidateCountsOk(ballotGroupId);
    }

    private async Task UpdateAllCandidateCountsOk(Guid ballotGroupId)
    {
        var ballotGroup = await _repo.Query()
            .AsTracking()
            .Include(bg => bg.MajorityElection.SecondaryMajorityElections)
            .FirstOrDefaultAsync(bg => bg.Id == ballotGroupId)
            ?? throw new EntityNotFoundException(ballotGroupId);

        var electionNumberOfMandates = ballotGroup.MajorityElection.SecondaryMajorityElections
            .ToDictionary(e => e.Id, e => e.NumberOfMandates);
        electionNumberOfMandates.Add(ballotGroup.MajorityElectionId, ballotGroup.MajorityElection.NumberOfMandates);

        // If we do the `Candidates.Count` directly in the ToDictionary call then EF core doesn't resolve it correctly.
        var entryCounts = await _entryRepo.Query()
            .Where(e => e.BallotGroupId == ballotGroupId)
            .Select(x => new
            {
                Id = x.PrimaryMajorityElectionId ?? x.SecondaryMajorityElectionId,
                Count = x.BlankRowCount + x.IndividualCandidatesVoteCount + x.Candidates.Count,
            })
            .ToDictionaryAsync(
                e => e.Id ?? throw new InvalidOperationException($"Failed to build ballot entry count dictionary for ballot group with id '{ballotGroupId}' because primary and secondary election id is null"),
                e => e.Count);

        var allCandidateCountsOk = true;
        foreach (var (electionId, numberOfMandates) in electionNumberOfMandates)
        {
            if (!entryCounts.TryGetValue(electionId, out var candidateCount) || candidateCount != numberOfMandates)
            {
                allCandidateCountsOk = false;
                break;
            }
        }

        if (allCandidateCountsOk == ballotGroup.AllCandidateCountsOk)
        {
            return;
        }

        ballotGroup.AllCandidateCountsOk = allCandidateCountsOk;
        await _repo.Update(ballotGroup);
    }
}
