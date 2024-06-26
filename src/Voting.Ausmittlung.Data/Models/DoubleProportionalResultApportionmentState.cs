﻿// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum DoubleProportionalResultApportionmentState
{
    Unspecified,
    Initial,
    Error,
    HasOpenLotDecision,
    Completed,
}
