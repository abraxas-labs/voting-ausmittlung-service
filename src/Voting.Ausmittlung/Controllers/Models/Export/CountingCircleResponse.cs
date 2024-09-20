// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class CountingCircleResponse
{
    public Guid Id { get; set; }

    public string Bfs { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
