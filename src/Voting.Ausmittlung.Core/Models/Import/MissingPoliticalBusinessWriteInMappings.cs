// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models.Import;

public class MissingPoliticalBusinessWriteInMappings
{
    internal MissingPoliticalBusinessWriteInMappings(PoliticalBusiness politicalBusiness, IReadOnlyCollection<MissingWriteInMapping> missingWriteInMappings)
    {
        PoliticalBusiness = politicalBusiness;
        MissingWriteInMappings = missingWriteInMappings;
    }

    public PoliticalBusiness PoliticalBusiness { get; }

    public IReadOnlyCollection<MissingWriteInMapping> MissingWriteInMappings { get; }
}
