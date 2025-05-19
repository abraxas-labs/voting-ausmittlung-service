// (c) Copyright by Abraxas Informatik AG
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
using ResultImportType = Voting.Ausmittlung.Data.Models.ResultImportType;

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
        var importType = (ResultImportType)eventData.ImportType;

        // all legacy events are evoting events
        if (importType == ResultImportType.Unspecified)
        {
            importType = ResultImportType.EVoting;
        }

        var dataSource = importType.GetDataSource();
        var voteResult = await _voteResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Results).ThenInclude(x => x.QuestionResults).ThenInclude(x => x.Question)
            .Include(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults).ThenInclude(x => x.Question)
            .FirstOrDefaultAsync(x => x.CountingCircle.BasisCountingCircleId == countingCircleId && x.VoteId == voteId)
            ?? throw new EntityNotFoundException(nameof(VoteResult), new { countingCircleId, voteId });

        var ballotResultsByBallotId = voteResult.Results.ToDictionary(x => x.BallotId);
        foreach (var importedBallotResult in eventData.BallotResults)
        {
            var ballotId = GuidParser.Parse(importedBallotResult.BallotId);
            var ballotResult = ballotResultsByBallotId[ballotId];

            SetCountOfVoters(importType, ballotResult, importedBallotResult);
            ballotResult.CountOfVoters.UpdateVoterParticipation(voteResult.TotalCountOfVoters);

            ProcessBallotQuestionResults(dataSource, ballotResult, importedBallotResult.QuestionResults);
            ProcessTieBreakQuestionResults(dataSource, ballotResult, importedBallotResult.TieBreakQuestionResults);
        }

        if (importType == ResultImportType.EVoting)
        {
            voteResult.TotalSentEVotingVotingCards = eventData.CountOfVotersInformation?.TotalCountOfVoters;
        }

        await _voteResultRepo.Update(voteResult);
    }

    private static void SetCountOfVoters(
        ResultImportType importType,
        BallotResult ballotResult,
        VoteBallotResultImportEventData importedBallotResult)
    {
        var subTotal = ballotResult.CountOfVoters.GetNonNullableSubTotal(importType.GetDataSource());
        subTotal.ReceivedBallots = importedBallotResult.CountOfVoters;
        subTotal.BlankBallots = importedBallotResult.BlankBallotCount;
        subTotal.AccountedBallots = importedBallotResult.CountOfVoters - importedBallotResult.BlankBallotCount;
    }

    private void ProcessBallotQuestionResults(VotingDataSource dataSource, BallotResult ballotResult, IEnumerable<BallotQuestionResultImportEventData> importedQuestionResults)
    {
        var questionResultsByNumber = ballotResult.QuestionResults.ToDictionary(x => x.Question.Number);

        foreach (var importedQuestionResult in importedQuestionResults)
        {
            var questionResult = questionResultsByNumber[importedQuestionResult.QuestionNumber];
            var subTotal = questionResult.GetNonNullableSubTotal(dataSource);
            subTotal.TotalCountOfAnswerYes = importedQuestionResult.CountYes;
            subTotal.TotalCountOfAnswerNo = importedQuestionResult.CountNo;
            subTotal.TotalCountOfAnswerUnspecified = importedQuestionResult.CountUnspecified;
        }
    }

    private void ProcessTieBreakQuestionResults(VotingDataSource dataSource, BallotResult ballotResult, IEnumerable<TieBreakQuestionResultImportEventData> importedTieBreakQuestionResults)
    {
        var tieBreakQuestionResultsByNumber = ballotResult.TieBreakQuestionResults.ToDictionary(x => x.Question.Number);

        foreach (var importedTieBreakQuestionResult in importedTieBreakQuestionResults)
        {
            var tieBreakQuestionResult = tieBreakQuestionResultsByNumber[importedTieBreakQuestionResult.QuestionNumber];

            var subTotal = tieBreakQuestionResult.GetNonNullableSubTotal(dataSource);
            subTotal.TotalCountOfAnswerQ1 = importedTieBreakQuestionResult.CountQ1;
            subTotal.TotalCountOfAnswerQ2 = importedTieBreakQuestionResult.CountQ2;
            subTotal.TotalCountOfAnswerUnspecified = importedTieBreakQuestionResult.CountUnspecified;
        }
    }
}
