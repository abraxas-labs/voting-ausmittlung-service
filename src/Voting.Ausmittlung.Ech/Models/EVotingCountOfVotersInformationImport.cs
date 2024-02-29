// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public record EVotingCountOfVotersInformationImport(Guid ContestId, List<EVotingCountingCircleResultCountOfVotersInformation> CountingCircleResultsCountOfVotersInformations);
