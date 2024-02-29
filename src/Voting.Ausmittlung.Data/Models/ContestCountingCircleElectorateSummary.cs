// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class ContestCountingCircleElectorateSummary
{
    public ContestCountingCircleElectorateSummary(
        IReadOnlyCollection<CountingCircleElectorateBase> effectiveElectorates,
        IReadOnlyCollection<CountingCircleElectorateBase> contestCountingCircleElectorates)
    {
        EffectiveElectorates = effectiveElectorates;
        ContestCountingCircleElectorates = contestCountingCircleElectorates;
    }

    /// <summary>
    /// <para>Gets the effective electorate (which reflects how it can be grouped in the user interface).</para>
    /// <para>It only contains electorates with domain of influence types from a political business result of the corresponding counting circle and contest.</para>
    /// <para>It is ensured that electorates only contain distinct domain of influence type voting cards.</para>
    /// </summary>
    public IReadOnlyCollection<CountingCircleElectorateBase> EffectiveElectorates { get; }

    public IReadOnlyCollection<CountingCircleElectorateBase> ContestCountingCircleElectorates { get; }
}
