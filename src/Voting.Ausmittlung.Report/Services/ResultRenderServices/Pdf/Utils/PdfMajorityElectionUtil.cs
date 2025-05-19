// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

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

    public PdfMajorityElectionUtil(
        IDbRepository<DataContext, SecondaryMajorityElection> secondaryElectionRepo)
    {
        _secondaryElectionRepo = secondaryElectionRepo;
    }

    public async Task FillEmptyVoteCountDisabled(PdfMajorityElection majorityElection)
    {
        var hasSecondaryElections = await _secondaryElectionRepo.Query()
            .AnyAsync(sme => sme.PrimaryMajorityElectionId == majorityElection.Id);

        majorityElection.EmptyVoteCountDisabled = majorityElection.NumberOfMandates == 1 && !hasSecondaryElections;
    }
}
