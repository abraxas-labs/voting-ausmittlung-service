// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.ModelBuilders;

internal static class DbContextAccessor
{
    // Needed to have access to the DataContext inside model builders
    internal static DataContext DbContext { get; set; } = null!;
}
