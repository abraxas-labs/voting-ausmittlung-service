﻿// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ContestCountingCircleElectorate : CountingCircleElectorateBase
{
    public Guid CountingCircleId { get; set; }

    public CountingCircle CountingCircle { get; set; } = null!;

    public Guid ContestId { get; set; }

    public Contest Contest { get; set; } = null!;
}
