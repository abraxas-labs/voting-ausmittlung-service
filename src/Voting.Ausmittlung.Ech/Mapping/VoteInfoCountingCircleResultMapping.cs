// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Globalization;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoCountingCircleResultMapping
{
    private const string LastStateChangeName = "lastCountingStatusChange";
    private const string ResultStateName = "countingStatus";

    internal static List<NamedElementType> GetNamedElements(CountingCircleResult result)
    {
        var namedElements = new List<NamedElementType>
        {
            new NamedElementType
            {
                ElementName = ResultStateName,
                Text = ((uint)result.State).ToString(),
            },
        };

        var lastStateChangeTimestamp = result.GetLastStateChangeTimestamp();
        if (lastStateChangeTimestamp.HasValue)
        {
            namedElements.Add(new NamedElementType
            {
                ElementName = LastStateChangeName,
                Text = lastStateChangeTimestamp.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            });
        }

        return namedElements;
    }

    internal static decimal DecimalToPercentage(decimal value) => decimal.Round(value * 100, 2);
}
