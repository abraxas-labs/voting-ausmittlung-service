// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public static class CountingCircleResultStateExtensions
{
    public static bool IsResultAuditedTentatively(this CountingCircleResultState state) =>
        state >= CountingCircleResultState.AuditedTentatively;
}
