// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Converters;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Store;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Xml;

public class XmlEch0252ProportionalElectionResultWithCandidateResultsInfoRenderService
    : XmlEch0252ProportionalElectionResultRenderServiceBase
{
    public XmlEch0252ProportionalElectionResultWithCandidateResultsInfoRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Contest> contestRepo,
        Ech0252Serializer ech0252Serializer,
        IAuth auth)
        : base(templateService, contestRepo, ech0252Serializer, auth)
    {
    }

    protected override bool IncludeCandidateListResultsInfo => true;
}
