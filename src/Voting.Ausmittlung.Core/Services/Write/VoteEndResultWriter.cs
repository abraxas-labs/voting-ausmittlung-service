// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class VoteEndResultWriter : PoliticalBusinessEndResultWriter<VoteEndResultAggregate, DataModels.VoteEndResult>
{
    private readonly IDbRepository<DataContext, DataModels.VoteEndResult> _endResultRepo;

    public VoteEndResultWriter(
        ILogger<VoteEndResultWriter> logger,
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        ContestService contestService,
        PermissionService permissionService,
        IDbRepository<DataContext, DataModels.VoteEndResult> endResultRepo,
        SecondFactorTransactionWriter secondFactorTransactionWriter)
        : base(logger, aggregateRepository, aggregateFactory, contestService, permissionService, secondFactorTransactionWriter)
    {
        _endResultRepo = endResultRepo;
    }

    protected override Task<DataModels.VoteEndResult?> GetEndResult(Guid politicalBusinessId, string tenantId)
    {
        return _endResultRepo.Query()
            .FirstOrDefaultAsync(x =>
                x.VoteId == politicalBusinessId
                && x.Vote.DomainOfInfluence.SecureConnectId == tenantId);
    }
}
