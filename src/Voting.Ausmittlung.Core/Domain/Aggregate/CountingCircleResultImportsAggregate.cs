// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Core.Utils;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class CountingCircleResultImportsAggregate : BaseResultImportsAggregate
{
    public CountingCircleResultImportsAggregate(EventInfoProvider eventInfoProvider)
    : base(eventInfoProvider)
    {
    }

    public override string AggregateName => "voting-countingCircleResultImports";
}
