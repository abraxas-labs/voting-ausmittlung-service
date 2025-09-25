// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Extensions;

public static class CountingCircleResultStateExtensions
{
    public static bool IsSubmissionDone(this CountingCircleResultState state)
        => state >= CountingCircleResultState.SubmissionDone;
}
