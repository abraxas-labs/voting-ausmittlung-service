// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading;
using System.Threading.Tasks;
using Voting.Ausmittlung.Report.Models;

namespace Voting.Ausmittlung.Core.Services.Export;

public interface IDokConnector
{
    Task<string> Save(string eaiMessageType, FileModel file, CancellationToken ct);
}
