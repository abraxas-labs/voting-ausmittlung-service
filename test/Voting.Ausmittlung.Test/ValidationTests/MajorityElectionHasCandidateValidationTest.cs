// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ValidationTests;

public class MajorityElectionHasCandidateValidationTest : BaseValidationTest<IValidator<MajorityElectionResult>, MajorityElectionResult>
{
    public MajorityElectionHasCandidateValidationTest()
        : base(SharedProto.Validation.MajorityElectionHasCandidates)
    {
    }

    [Fact]
    public void ShouldExcludeHasCandidateValidationInTestingPhase()
    {
        var context = BuildValidationContext();
        var validationResults = Validate(BuildMajorityElectionResult(), context);

        EnsureHasCount(validationResults, 0);
    }

    [Fact]
    public void ShouldIncludeHasCandidateValidationInActivePhase()
    {
        var context = BuildValidationContext(testingPhaseEnded: true);
        var validationResults = Validate(BuildMajorityElectionResult(), context);

        EnsureHasCount(validationResults, 2);
        EnsureIsValid(validationResults, false);
    }

    private MajorityElectionResult BuildMajorityElectionResult()
    {
        var contest = new Contest { CantonDefaults = new() };
        var result = new MajorityElectionResult()
        {
            MajorityElection = new()
            {
                Translations = new List<MajorityElectionTranslation>
                {
                    new()
                    {
                        ShortDescription = "Mw",
                        OfficialDescription = "Mw",
                        Language = Languages.German,
                    },
                },
                NumberOfMandates = 1,
                Contest = contest,
            },
            SecondaryMajorityElectionResults = new List<SecondaryMajorityElectionResult>
            {
                new()
                {
                    SecondaryMajorityElection = new()
                    {
                        Translations = new List<SecondaryMajorityElectionTranslation>
                        {
                            new()
                            {
                                ShortDescription = "NMw",
                                OfficialDescription = "NMw",
                                Language = Languages.German,
                            },
                        },
                        NumberOfMandates = 1,
                    },
                },
            },
        };

        foreach (var smeResult in result.SecondaryMajorityElectionResults)
        {
            smeResult.PrimaryResult = result;
            smeResult.SecondaryMajorityElection.PrimaryMajorityElection = result.MajorityElection;
        }

        return result;
    }
}
