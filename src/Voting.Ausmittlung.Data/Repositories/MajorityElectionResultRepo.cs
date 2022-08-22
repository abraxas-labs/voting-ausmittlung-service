﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq.Expressions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class MajorityElectionResultRepo : PoliticalBusinessResultRepo<MajorityElectionResult>
{
    public MajorityElectionResultRepo(DataContext context)
        : base(context)
    {
    }

    protected override Expression<Func<MajorityElectionResult, bool>> FilterByPoliticalBusinessId(Guid id) =>
        x => x.MajorityElectionId == id;
}
