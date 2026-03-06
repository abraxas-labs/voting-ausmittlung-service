// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public class PdfMajorityElectionUtil
{
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _secondaryElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResult> _resultRepo;

    public PdfMajorityElectionUtil(
        IDbRepository<DataContext, SecondaryMajorityElection> secondaryElectionRepo,
        IDbRepository<DataContext, MajorityElectionResult> resultRepo)
    {
        _secondaryElectionRepo = secondaryElectionRepo;
        _resultRepo = resultRepo;
    }

    public async Task SetEndResultIsComplete(Guid majorityElectionId, PdfMajorityElectionEndResult? pdfEndResult, IEnumerable<MajorityElectionCandidateEndResultBase> candidateEndResults)
    {
        if (pdfEndResult == null || !pdfEndResult.AllCountingCirclesDone)
        {
            return;
        }

        var hasOpenLotDecision = candidateEndResults.Any(c => c.LotDecisionRequired && !c.LotDecision);
        if (hasOpenLotDecision)
        {
            return;
        }

        pdfEndResult.IsComplete = await _resultRepo.Query()
            .Where(x => x.MajorityElectionId == majorityElectionId)
            .AllAsync(x => x.State >= CountingCircleResultState.AuditedTentatively);
    }

    public async Task FillEmptyVoteCountDisabled(PdfMajorityElection majorityElection)
    {
        var hasSecondaryElections = await _secondaryElectionRepo.Query()
            .AnyAsync(sme => sme.PrimaryMajorityElectionId == majorityElection.Id);

        majorityElection.EmptyVoteCountDisabled = majorityElection.NumberOfMandates == 1 && !hasSecondaryElections;
    }
}
