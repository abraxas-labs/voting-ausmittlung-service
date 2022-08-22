// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Validation.Models;

public class ValidationContext
{
    private PoliticalBusinessNullableCountOfVoters? _countOfVoters;

    public ValidationContext(
        DomainOfInfluence responsibleForPlausibilisationDomainOfInfluence,
        ContestCountingCircleDetails currentContestCountingCircleDetails,
        ContestCountingCircleDetails? previousContestCountingCircleDetails = null)
    {
        PlausibilisationConfiguration = responsibleForPlausibilisationDomainOfInfluence.PlausibilisationConfiguration;
        CurrentContestCountingCircleDetails = currentContestCountingCircleDetails;
        PreviousContestCountingCircleDetails = previousContestCountingCircleDetails;

        if (PlausibilisationConfiguration == null)
        {
            return;
        }

        // use the nested counting circle id as search criteria, because that is loaded from the read model.
        var doiCc = responsibleForPlausibilisationDomainOfInfluence.CountingCircles
            .FirstOrDefault(doiCc => doiCc.CountingCircleId == currentContestCountingCircleDetails.CountingCircle.Id)
            ?? throw new EntityNotFoundException(currentContestCountingCircleDetails.CountingCircle.Id);

        ComparisonCountOfVotersConfiguration = responsibleForPlausibilisationDomainOfInfluence
            .PlausibilisationConfiguration!
            .ComparisonCountOfVotersConfigurations
            .FirstOrDefault(x => x.Category == doiCc.ComparisonCountOfVotersCategory);
    }

    public PlausibilisationConfiguration? PlausibilisationConfiguration { get; }

    public ContestCountingCircleDetails CurrentContestCountingCircleDetails { get; }

    public ContestCountingCircleDetails? PreviousContestCountingCircleDetails { get; }

    public PoliticalBusinessNullableCountOfVoters CountOfVoters
    {
        get => _countOfVoters ?? throw new InvalidOperationException($"{nameof(CountOfVoters)} is not set");
        set => _countOfVoters = value;
    }

    public PoliticalBusinessType? PoliticalBusinessType { get; set; }

    public DomainOfInfluenceType PoliticalBusinessDomainOfInfluenceType { get; set; }

    public bool IsDetailedEntry { get; set; }

    public ComparisonCountOfVotersConfiguration? ComparisonCountOfVotersConfiguration { get; }
}
