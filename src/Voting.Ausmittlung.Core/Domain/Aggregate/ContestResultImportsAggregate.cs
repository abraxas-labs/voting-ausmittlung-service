// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Core.Utils;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ContestResultImportsAggregate : BaseResultImportsAggregate
{
    public ContestResultImportsAggregate(EventInfoProvider eventInfoProvider)
        : base(eventInfoProvider)
    {
    }

    public override string AggregateName => "voting-contestResultImports";
}
