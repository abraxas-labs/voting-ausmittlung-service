// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class CountingCircleElectorate : CountingCircleElectorateBase
{
    public Guid CountingCircleId { get; set; }

    public CountingCircle CountingCircle { get; set; } = null!;
}
