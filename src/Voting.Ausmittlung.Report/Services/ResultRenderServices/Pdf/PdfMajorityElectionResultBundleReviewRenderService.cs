// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfMajorityElectionResultBundleReviewRenderService : IRendererService
{
    private readonly IDbRepository<DataContext, MajorityElectionResultBundle> _majorityElectionResultBundleRepo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly TemplateService _templateService;

    public PdfMajorityElectionResultBundleReviewRenderService(
        IDbRepository<DataContext, MajorityElectionResultBundle> majorityElectionResultBundleRepo,
        IMapper mapper,
        TemplateService templateService,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IClock clock)
    {
        _majorityElectionResultBundleRepo = majorityElectionResultBundleRepo;
        _mapper = mapper;
        _templateService = templateService;
        _countingCircleRepo = countingCircleRepo;
        _clock = clock;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        if (ctx.PoliticalBusinessResultBundleId == null || ctx.PoliticalBusinessResultBundleId == Guid.Empty)
        {
            throw new ValidationException("Political business result bundle id must not be null or empty.");
        }

        var bundle = await _majorityElectionResultBundleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Ballots.Where(b => b.MarkedForReview))
            .ThenInclude(x => x.BallotCandidates.Where(c => c.Selected))
            .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.Ballots.Where(b => b.MarkedForReview))
            .ThenInclude(x => x.SecondaryMajorityElectionBallots
                .OrderBy(y => y.SecondaryMajorityElectionResult.SecondaryMajorityElection.PoliticalBusinessNumber))
            .ThenInclude(x => x.BallotCandidates.Where(c => c.Selected))
            .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.ElectionResult.MajorityElection.Translations)
            .FirstOrDefaultAsync(x => x.Id == ctx.PoliticalBusinessResultBundleId, ct)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResultBundle), ctx.PoliticalBusinessResultBundleId);

        if (bundle.ElectionResult.MajorityElectionId != ctx.PoliticalBusinessId)
        {
            throw new ValidationException("political business id is not valid for bundle");
        }

        if (!bundle.ElectionResult.PoliticalBusiness.Active)
        {
            throw new ValidationException("political business is not active");
        }

        bundle.Ballots = bundle.Ballots.OrderBy(x => x.Number).ToList();
        foreach (var ballot in bundle.Ballots)
        {
            ballot.BallotCandidates = ballot.BallotCandidates.OrderBy(x => x.Candidate.Position).ToList();

            // add check digit to each candidate in this bundle, this is done manually as it is only used in the bundle review exports
            if (bundle.ElectionResult.EntryParams?.CandidateCheckDigit == true)
            {
                foreach (var ballotCandidate in ballot.BallotCandidates)
                {
                    ballotCandidate.Candidate.Number = $"{ballotCandidate.Candidate.Number}{ballotCandidate.Candidate.CheckDigit}";
                }
            }
        }

        var countingCircle = await _countingCircleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ResponsibleAuthority)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .FirstOrDefaultAsync(x => x.SnapshotContestId == ctx.ContestId && x.BasisCountingCircleId == ctx.BasisCountingCircleId, ct)
            ?? throw new EntityNotFoundException(nameof(CountingCircle), new { ctx.ContestId, ctx.BasisCountingCircleId });

        var pdfCountingCircle = _mapper.Map<PdfCountingCircle>(countingCircle);
        var bundleReview = new PdfPoliticalBusinessResultBundleReview
        {
            TemplateKey = ctx.Template.Key,
            GeneratedAt = _clock.UtcNow.ConvertUtcTimeToSwissTime(),
            CountingCircle = pdfCountingCircle,
            MajorityElectionResultBundle = _mapper.Map<PdfMajorityElectionResultBundle>(bundle),
            PoliticalBusiness = _mapper.Map<PdfPoliticalBusiness>(bundle.ElectionResult.MajorityElection),
        };

        return await _templateService.RenderToPdf(ctx, bundleReview, bundle.Number.ToString());
    }
}
