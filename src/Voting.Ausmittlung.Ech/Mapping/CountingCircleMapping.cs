// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Ech0155_4_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class CountingCircleMapping
{
    internal static CountingCircleType ToEchCountingCircle(this CountingCircle countingCircle)
    {
        return new CountingCircleType
        {
            CountingCircleId = countingCircle.BasisCountingCircleId.ToString(),
            CountingCircleName = countingCircle.Name,
        };
    }

    internal static Ech0252_2_0.CountingCircleType ToEch0252CountingCircle(this CountingCircle countingCircle)
    {
        return new Ech0252_2_0.CountingCircleType
        {
            CountingCircleId = countingCircle.BasisCountingCircleId.ToString(),
            CountingCircleName = countingCircle.Name,
        };
    }
}
