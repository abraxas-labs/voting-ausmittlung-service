// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public static class CountingCircleResultStateExtensions
{
    public static bool IsResultCollectionDone(this CountingCircleResultState state) =>
        state >= CountingCircleResultState.SubmissionDone;
}
