﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfProportionalElectionUnionQuorumUnionListDoubleProportionalResultExportTest : PdfProportionalElectionUnionDoubleProportionalResultExportBaseTest
{
    public PdfProportionalElectionUnionQuorumUnionListDoubleProportionalResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Stimmenquorum der Listengruppen.pdf";

    protected override string TemplateKey => AusmittlungPdfProportionalElectionTemplates.UnionEndResultQuorumUnionListDoubleProportional.Key;

    protected override StartProtocolExportsRequest NewRequest()
    {
        return new StartProtocolExportsRequest
        {
            ContestId = ZhMockedData.ContestIdBund,
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(
                        TemplateKey,
                        SecureConnectTestDefaults.MockedTenantBund.Id,
                        politicalBusinessUnionId: ZhMockedData.ProportionalElectionUnionGuidKtrat)
                    .ToString(),
            },
        };
    }
}
