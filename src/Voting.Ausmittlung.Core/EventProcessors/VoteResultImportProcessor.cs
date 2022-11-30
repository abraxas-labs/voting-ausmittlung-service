// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class VoteResultImportProcessor : IEventProcessor<VoteResultImported>
{
    private readonly IDbRepository<DataContext, VoteResult> _voteResultRepo;

    public VoteResultImportProcessor(IDbRepository<DataContext, VoteResult> voteResultRepo)
    {
        _voteResultRepo = voteResultRepo;
    }

    public async Task Process(VoteResultImported eventData)
    {
        var voteId = GuidParser.Parse(eventData.VoteId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var voteResult = await _voteResultRepo.Query()
                         .AsSplitQuery()
                         .Include(x => x.Results).ThenInclude(x => x.QuestionResults).ThenInclude(x => x.Question)
                         .Include(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults).ThenInclude(x => x.Question)
                         .FirstOrDefaultAsync(x =>
                             x.CountingCircle.BasisCountingCircleId == countingCircleId && x.VoteId == voteId)
                     ?? throw new EntityNotFoundException(nameof(VoteResult), new { countingCircleId, voteId });

        var ballotResultsByBallotId = voteResult.Results.ToDictionary(x => x.BallotId);
        foreach (var importedBallotResult in eventData.BallotResults)
        {
            var ballotId = GuidParser.Parse(importedBallotResult.BallotId);
            var ballotResult = ballotResultsByBallotId[ballotId];

            ballotResult.CountOfVoters.EVotingReceivedBallots = importedBallotResult.CountOfVoters;
            ballotResult.CountOfVoters.EVotingAccountedBallots = importedBallotResult.CountOfVoters;
            ballotResult.CountOfVoters.UpdateVoterParticipation(voteResult.TotalCountOfVoters);

            ProcessBallotQuestionResults(ballotResult, importedBallotResult.QuestionResults);
            ProcessTieBreakQuestionResults(ballotResult, importedBallotResult.TieBreakQuestionResults);
        }

        await _voteResultRepo.Update(voteResult);
    }

    private void ProcessBallotQuestionResults(BallotResult ballotResult, IEnumerable<BallotQuestionResultImportEventData> importedQuestionResults)
    {
        var questionResultsByNumber = ballotResult.QuestionResults.ToDictionary(x => x.Question.Number);

        foreach (var importedQuestionResult in importedQuestionResults)
        {
            var questionResult = questionResultsByNumber[importedQuestionResult.QuestionNumber];

            questionResult.EVotingSubTotal.TotalCountOfAnswerYes = importedQuestionResult.CountYes;
            questionResult.EVotingSubTotal.TotalCountOfAnswerNo = importedQuestionResult.CountNo;
            questionResult.EVotingSubTotal.TotalCountOfAnswerUnspecified = importedQuestionResult.CountUnspecified;
        }
    }

    private void ProcessTieBreakQuestionResults(BallotResult ballotResult, IEnumerable<TieBreakQuestionResultImportEventData> importedTieBreakQuestionResults)
    {
        var tieBreakQuestionResultsByNumber = ballotResult.TieBreakQuestionResults.ToDictionary(x => x.Question.Number);

        foreach (var importedTieBreakQuestionResult in importedTieBreakQuestionResults)
        {
            var tieBreakQuestionResult = tieBreakQuestionResultsByNumber[importedTieBreakQuestionResult.QuestionNumber];

            tieBreakQuestionResult.EVotingSubTotal.TotalCountOfAnswerQ1 = importedTieBreakQuestionResult.CountQ1;
            tieBreakQuestionResult.EVotingSubTotal.TotalCountOfAnswerQ2 = importedTieBreakQuestionResult.CountQ2;
            tieBreakQuestionResult.EVotingSubTotal.TotalCountOfAnswerUnspecified = importedTieBreakQuestionResult.CountUnspecified;
        }
    }
}
