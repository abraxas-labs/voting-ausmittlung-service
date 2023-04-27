// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Configuration;

public class DmDocConfig : Lib.DmDoc.Configuration.DmDocConfig
{
    /// <summary>
    /// Gets or sets callback base URL of the VOTING Ausmittlung API.
    /// The base URL should just point to the base API endpoint (ex. http://localhost/api/).
    /// </summary>
    public string CallbackBaseUrl { get; set; } = string.Empty;

    public string GetProtocolExportCallbackUrl(Guid protocolExportId, string callbackToken)
        => $"{CallbackBaseUrl}result_export/webhook_callback?protocolExportId={protocolExportId}&callbackToken={callbackToken}";
}
