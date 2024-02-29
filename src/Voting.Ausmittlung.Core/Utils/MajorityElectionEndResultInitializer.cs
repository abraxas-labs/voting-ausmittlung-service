// (c) Copyright 2024 by Abraxas Informatik AG
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

public class MajorityElectionEndResultInitializer
{
    private readonly MajorityElectionRepo _electionRepo;
    private readonly MajorityElectionEndResultRepo _endResultRepo;
    private readonly SecondaryMajorityElectionRepo _secondaryMajorityElectionRepo;
    private readonly MajorityElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly DataContext _dataContext;

    public MajorityElectionEndResultInitializer(
        MajorityElectionRepo electionRepo,
        MajorityElectionEndResultRepo endResultRepo,
        SecondaryMajorityElectionRepo secondaryMajorityElectionRepo,
        MajorityElectionCandidateEndResultBuilder candidateEndResultBuilder,
        DataContext dataContext)
    {
        _electionRepo = electionRepo;
        _endResultRepo = endResultRepo;
        _secondaryMajorityElectionRepo = secondaryMajorityElectionRepo;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _dataContext = dataContext;
    }

    internal async Task RebuildForElection(Guid majorityElectionId, bool testingPhaseEnded)
    {
        var endResultId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(majorityElectionId, testingPhaseEnded);
        var countOfCountingCircles = await _electionRepo.CountOfCountingCircles(majorityElectionId);

        var existingEndResult = await _endResultRepo
            .Query()
            .FirstOrDefaultAsync(x => x.MajorityElectionId == majorityElectionId);

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
            existingEndResult = new MajorityElectionEndResult
            {
                Id = endResultId,
                MajorityElectionId = majorityElectionId,
                TotalCountOfCountingCircles = countOfCountingCircles,
            };
            await _endResultRepo.Create(existingEndResult);
        }
        else
        {
            existingEndResult.TotalCountOfCountingCircles = countOfCountingCircles;
            await _endResultRepo.Update(existingEndResult);
        }

        var majorityElection = await _electionRepo.GetWithEndResultsAsTracked(majorityElectionId)
            ?? throw new EntityNotFoundException(majorityElectionId);

        _candidateEndResultBuilder.AddMissingMajorityElectionCandidateEndResults(
            majorityElection.EndResult!,
            majorityElection.MajorityElectionCandidates);

        foreach (var secondaryMajorityElection in majorityElection.SecondaryMajorityElections)
        {
            AddMissingEndResultsToSecondaryMajorityElection(secondaryMajorityElection);
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task InitializeForSecondaryElection(Guid secondaryMajorityElectionId)
    {
        var secondaryMajorityElection = await _secondaryMajorityElectionRepo.GetWithEndResultsAsTracked(secondaryMajorityElectionId)
            ?? throw new EntityNotFoundException(secondaryMajorityElectionId);

        AddMissingEndResultsToSecondaryMajorityElection(secondaryMajorityElection);
        await _dataContext.SaveChangesAsync();
    }

    internal async Task ResetForElection(Guid majorityElectionId)
    {
        var majorityElectionEndResultId = await _endResultRepo.Query()
               .Where(x => x.MajorityElectionId == majorityElectionId)
               .Select(x => x.Id)
               .FirstOrDefaultAsync();

        await _endResultRepo.DeleteByKey(majorityElectionEndResultId);
        await RebuildForElection(majorityElectionId, true);
    }

    private void AddMissingEndResultsToSecondaryMajorityElection(SecondaryMajorityElection secondaryMajorityElection)
    {
        secondaryMajorityElection.EndResult ??= new SecondaryMajorityElectionEndResult
        {
            PrimaryMajorityElectionEndResult = secondaryMajorityElection.PrimaryMajorityElection.EndResult!,
        };

        _candidateEndResultBuilder.AddMissingSecondaryMajorityElectionCandidateEndResults(secondaryMajorityElection.EndResult, secondaryMajorityElection.Candidates);
    }
}
