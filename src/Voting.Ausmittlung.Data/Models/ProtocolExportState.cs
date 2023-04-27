// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum ProtocolExportState
{
    /// <summary>
    /// Protocol export state is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Protocol export is being generated.
    /// </summary>
    Generating,

    /// <summary>
    /// Protocol export has been completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Protocol export has failed.
    /// </summary>
    Failed,
}
