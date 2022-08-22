// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AutoMapper;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public abstract class CountingCircleResultAggregate : BaseEventSignatureAggregate
{
    protected CountingCircleResultAggregate(EventSignatureService eventSignatureService, IMapper mapper)
        : base(eventSignatureService, mapper)
    {
    }

    public CountingCircleResultState State { get; protected set; } = CountingCircleResultState.Initial;

    public abstract void FlagForCorrection(Guid contestId, string comment = "");

    protected void EnsureInState(params CountingCircleResultState[] validStates)
    {
        if (!validStates.Contains(State))
        {
            throw new ValidationException($"This operation is not possible for state {State}");
        }
    }
}
