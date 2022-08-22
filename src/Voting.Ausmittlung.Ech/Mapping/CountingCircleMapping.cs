// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using eCH_0155_4_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class CountingCircleMapping
{
    internal static CountingCircleType ToEchCountingCircle(this CountingCircle countingCircle)
    {
        return CountingCircleType.Create(countingCircle.BasisCountingCircleId.ToString(), countingCircle.Name);
    }
}
