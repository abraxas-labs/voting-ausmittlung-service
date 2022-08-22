// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using FluentValidation;
using Voting.Ausmittlung.Core.Services;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public abstract class ElectionResultAggregate : CountingCircleResultAggregate
{
    private readonly IValidator<PoliticalBusinessCountOfVoters> _countOfVotersValidator;

    protected ElectionResultAggregate(IValidator<PoliticalBusinessCountOfVoters> countOfVotersValidator, EventSignatureService eventSignatureService, IMapper mapper)
        : base(eventSignatureService, mapper)
    {
        _countOfVotersValidator = countOfVotersValidator;
    }

    public PoliticalBusinessCountOfVoters CountOfVoters { get; protected set; } = new();

    /// <summary>
    /// Gets a set of all BundleNumbers which are currently in use or were in use by a deleted bundle.
    /// </summary>
    protected HashSet<int> BundleNumbers { get; } = new();

    /// <summary>
    /// Gets a set of all bundle numbers which are deleted and not reused (yet).
    /// </summary>
    protected HashSet<int> DeletedUnusedBundleNumbers { get; } = new();

    protected void ValidateCountOfVoters(PoliticalBusinessCountOfVoters countOfVoters)
    {
        _countOfVotersValidator.ValidateAndThrow(countOfVoters);
    }

    protected int GetNextBundleNumber()
    {
        return BundleNumbers.Count == 0 ? 1 : BundleNumbers.Max() + 1;
    }

    protected void ResetBundleNumbers()
    {
        BundleNumbers.Clear();
        DeletedUnusedBundleNumbers.Clear();
    }
}
