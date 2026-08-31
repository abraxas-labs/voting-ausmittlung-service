// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class BaseCertificateChain<TCertificateInfo> : BaseEntity
    where TCertificateInfo : BaseCertificateInfo
{
    public string RawPem { get; set; } = string.Empty;

    public ICollection<TCertificateInfo>? Infos { get; set; }

    public Guid CantonSettingsId { get; set; }

    public CantonSettings? CantonSettings { get; set; }

    public User ImportedBy { get; set; } = new();

    public DateTime ImportedAt { get; set; }
}
