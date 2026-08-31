// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ResultImportECountingClientCertificateInfo : BaseCertificateInfo
{
    public Guid ResultImportId { get; set; }

    public ResultImport? ResultImport { get; set; }

    public Guid CaCertificateInfoId { get; set; }

    public CantonSettingsECountingCertificateInfo? CaCertificateInfo { get; set; }
}
