// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Iam.AuthenticationScheme;

namespace Voting.Ausmittlung.Core.Configuration;

public class SecureConnectConfig : SecureConnectOptions
{
    public string ServiceUserId { get; set; } = string.Empty;

    public string AbraxasTenantId { get; set; } = string.Empty;

    public string AppShortNameErfassung { get; set; } = string.Empty;

    public string AppShortNameMonitoring { get; set; } = string.Empty;

    public string Temporary2FATenantId { get; set; } = string.Empty;
}
