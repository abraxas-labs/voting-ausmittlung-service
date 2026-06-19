// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Lib.Iam.SecondFactor.Models;
using Voting.Lib.Iam.SecondFactor.Services;
using Entity = Voting.Ausmittlung.TemporaryData.Models.SecondFactorTransaction;
using Repo = Voting.Lib.Database.Repositories.IDbRepository<Voting.Ausmittlung.TemporaryData.TemporaryDataContext, Voting.Ausmittlung.TemporaryData.Models.SecondFactorTransaction>;

namespace Voting.Ausmittlung.Core.Services;

public class SecondFactorTransactionStorageService : ISecondFactorTransactionRepository
{
    private readonly Repo _repo;
    private readonly TimeProvider _timeProvider;
    private readonly IMapper _mapper;

    public SecondFactorTransactionStorageService(Repo repo, TimeProvider timeProvider, IMapper mapper)
    {
        _repo = repo;
        _timeProvider = timeProvider;
        _mapper = mapper;
    }

    public async Task<SecondFactorTransaction> GetById(Guid transactionId)
    {
        var transaction = await _repo.GetByKey(transactionId)
                          ?? throw new EntityNotFoundException(nameof(TemporaryData.Models.SecondFactorTransaction), transactionId);
        return _mapper.Map<SecondFactorTransaction>(transaction);
    }

    public Task Create(SecondFactorTransaction transaction)
    {
        var entity = _mapper.Map<Entity>(transaction);
        return _repo.Create(entity);
    }

    public Task Update(SecondFactorTransaction transaction)
    {
        var entity = _mapper.Map<Entity>(transaction);
        return _repo.Update(entity);
    }

    public async Task DeleteExpired()
    {
        await _repo.Query()
            .Where(x => x.ExpiredAt < _timeProvider.GetUtcNowDateTime())
            .ExecuteDeleteAsync();
    }
}
