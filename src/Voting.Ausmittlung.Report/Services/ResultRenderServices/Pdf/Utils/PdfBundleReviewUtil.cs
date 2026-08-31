// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public class PdfBundleReviewUtil
{
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IMapper _mapper;

    public PdfBundleReviewUtil(
        IMapper mapper,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, Contest> contestRepo)
    {
        _mapper = mapper;
        _countingCircleRepo = countingCircleRepo;
        _contestRepo = contestRepo;
    }

    public async Task<PdfContest> GetContest(Guid contestId, CancellationToken ct)
    {
        var contest = await _contestRepo.Query()
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == contestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), new { contestId });

        return _mapper.Map<PdfContest>(contest);
    }

    public async Task<PdfCountingCircle> GetCountingCircle(Guid contestId, Guid? basisCountingCircleId, CancellationToken ct)
    {
        var countingCircle = await _countingCircleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ResponsibleAuthority)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .FirstOrDefaultAsync(x => x.SnapshotContestId == contestId && x.BasisCountingCircleId == basisCountingCircleId, ct)
            ?? throw new EntityNotFoundException(nameof(CountingCircle), new { contestId, basisCountingCircleId });

        return _mapper.Map<PdfCountingCircle>(countingCircle);
    }
}
