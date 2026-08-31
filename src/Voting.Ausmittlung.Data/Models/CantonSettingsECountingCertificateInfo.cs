// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class CantonSettingsECountingCertificateInfo : BaseCertificateInfo
{
    public Guid CertificateChainId { get; set; }

    public CantonSettingsECountingCertificateChain? CertificateChain { get; set; }

    public ICollection<ResultImportECountingClientCertificateInfo>? SignedECountingClientCertificates { get; set; }
}
