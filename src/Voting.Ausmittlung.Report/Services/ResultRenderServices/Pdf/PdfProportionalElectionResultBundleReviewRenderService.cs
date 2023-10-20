// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionResultBundleReviewRenderService : IRendererService
{
    private const string ListWithoutPartyOrderNumber = "99";
    private const string ListWithoutPartyShortDescription = "WoP";

    private readonly IDbRepository<DataContext, ProportionalElectionResultBundle> _proportionalElectionResultBundleRepo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IMapper _mapper;
    private readonly TemplateService _templateService;

    public PdfProportionalElectionResultBundleReviewRenderService(
        IDbRepository<DataContext, ProportionalElectionResultBundle> proportionalElectionResultBundleRepo,
        IMapper mapper,
        TemplateService templateService,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo)
    {
        _proportionalElectionResultBundleRepo = proportionalElectionResultBundleRepo;
        _mapper = mapper;
        _templateService = templateService;
        _countingCircleRepo = countingCircleRepo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        if (ctx.PoliticalBusinessResultBundleId == null || ctx.PoliticalBusinessResultBundleId == Guid.Empty)
        {
            throw new ValidationException("Political business result bundle id must not be null or empty.");
        }

        var bundle = await _proportionalElectionResultBundleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Ballots.Where(b => b.MarkedForReview))
            .ThenInclude(x => x.BallotCandidates)
            .ThenInclude(x => x.Candidate.ProportionalElectionList)
            .Include(x => x.Ballots.Where(b => b.MarkedForReview))
            .ThenInclude(x => x.BallotCandidates)
            .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.List!.Translations)
            .Include(x => x.ElectionResult.ProportionalElection.Translations)
            .FirstOrDefaultAsync(x => x.Id == ctx.PoliticalBusinessResultBundleId, ct)
            ?? throw new EntityNotFoundException(nameof(ProportionalElectionResultBundle), ctx.PoliticalBusinessResultBundleId);

        if (bundle.ElectionResult.ProportionalElectionId != ctx.PoliticalBusinessId)
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
            ballot.BallotCandidates = ballot.BallotCandidates.OrderBy(x => x.Position).ThenBy(x => x.RemovedFromList).ToList();
        }

        var countingCircle = await _countingCircleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ResponsibleAuthority)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ContestDetails.Where(co => co.ContestId == ctx.ContestId))
            .ThenInclude(x => x.VotingCards)
            .Include(x => x.ContestDetails)
            .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(x => x.SnapshotContestId == ctx.ContestId && x.BasisCountingCircleId == ctx.BasisCountingCircleId, ct)
            ?? throw new EntityNotFoundException(nameof(CountingCircle), new { ctx.ContestId, ctx.BasisCountingCircleId });
        var ccDetails = countingCircle.ContestDetails.FirstOrDefault();
        ccDetails?.OrderVotingCardsAndSubTotals();

        var pdfCountingCircle = _mapper.Map<PdfCountingCircle>(countingCircle);
        pdfCountingCircle.ContestCountingCircleDetails = _mapper.Map<PdfContestCountingCircleDetails>(ccDetails);
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(pdfCountingCircle.ContestCountingCircleDetails, ctx.DomainOfInfluenceType);

        var pdfBundle = _mapper.Map<PdfProportionalElectionResultBundle>(bundle);
        pdfBundle.List ??= new PdfProportionalElectionSimpleList
        {
            OrderNumber = ListWithoutPartyOrderNumber,
            ShortDescription = ListWithoutPartyShortDescription,
        };

        var bundleReview = new PdfPoliticalBusinessResultBundleReview
        {
            TemplateKey = ctx.Template.Key,
            CountingCircle = pdfCountingCircle,
            ProportionalElectionResultBundle = pdfBundle,
            PoliticalBusiness = _mapper.Map<PdfPoliticalBusiness>(bundle.ElectionResult.ProportionalElection),
        };

        return await _templateService.RenderToPdf(ctx, bundleReview, bundle.Number.ToString());
    }
}
