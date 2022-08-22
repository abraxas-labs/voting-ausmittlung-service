// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionEndResultInitializer
{
    private readonly ProportionalElectionRepo _electionRepo;
    private readonly ProportionalElectionEndResultRepo _endResultRepo;
    private readonly ProportionalElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly ProportionalElectionListRepo _listRepo;
    private readonly DataContext _dataContext;

    public ProportionalElectionEndResultInitializer(
        ProportionalElectionRepo electionRepo,
        ProportionalElectionEndResultRepo endResultRepo,
        ProportionalElectionCandidateEndResultBuilder candidateEndResultBuilder,
        ProportionalElectionListRepo listRepo,
        DataContext dataContext)
    {
        _electionRepo = electionRepo;
        _endResultRepo = endResultRepo;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _listRepo = listRepo;
        _dataContext = dataContext;
    }

    internal async Task RebuildForElection(Guid proportionalElectionId, bool testingPhaseEnded)
    {
        var endResultId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(proportionalElectionId, testingPhaseEnded);
        var countOfCountingCircles = await _electionRepo.CountOfCountingCircles(proportionalElectionId);

        var existingEndResult = await _endResultRepo
            .Query()
            .FirstOrDefaultAsync(x => x.ProportionalElectionId == proportionalElectionId);

        if (testingPhaseEnded && existingEndResult != null)
        {
            if (existingEndResult.Id == endResultId)
            {
                throw new InvalidOperationException("Cannot build end result after testing phase ended when it is already built");
            }

            await _endResultRepo.DeleteByKey(existingEndResult.Id);
            existingEndResult = null;
        }

        if (existingEndResult == null)
        {
            existingEndResult = new ProportionalElectionEndResult
            {
                Id = endResultId,
                ProportionalElectionId = proportionalElectionId,
                TotalCountOfCountingCircles = countOfCountingCircles,
            };
            await _endResultRepo.Create(existingEndResult);
        }
        else
        {
            existingEndResult.TotalCountOfCountingCircles = countOfCountingCircles;
            await _endResultRepo.Update(existingEndResult);
        }

        var proportionalElection = await _electionRepo.GetWithEndResultsAsTracked(proportionalElectionId)
            ?? throw new EntityNotFoundException(proportionalElectionId);

        foreach (var list in proportionalElection.ProportionalElectionLists)
        {
            AddMissingEndResultsToProportionalElectionList(list);
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task ResetForElection(Guid proportionalElectionId)
    {
        var proportionalElectionEndResultId = await _endResultRepo.Query()
               .Where(x => x.ProportionalElectionId == proportionalElectionId)
               .Select(x => x.Id)
               .FirstOrDefaultAsync();

        await _endResultRepo.DeleteByKey(proportionalElectionEndResultId);
        await RebuildForElection(proportionalElectionId, true);
    }

    internal async Task InitializeForList(Guid proportionalElectionListId)
    {
        var list = await _listRepo.GetWithEndResultsAsTracked(proportionalElectionListId)
            ?? throw new EntityNotFoundException(proportionalElectionListId);

        AddMissingEndResultsToProportionalElectionList(list);
        await _dataContext.SaveChangesAsync();
    }

    private void AddMissingEndResultsToProportionalElectionList(ProportionalElectionList list)
    {
        list.EndResult ??= new ProportionalElectionListEndResult
        {
            ElectionEndResult = list.ProportionalElection.EndResult!,
        };

        _candidateEndResultBuilder.AddMissingCandidateEndResults(list.EndResult, list.ProportionalElectionCandidates);
    }
}
