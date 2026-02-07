// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data.Models;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ValidationTests;

public class ContestCountingCircleDetailsCountingMachineNotUnspecifiedValidationTest : BaseValidationTest<IValidator<ContestCountingCircleDetails>, ContestCountingCircleDetails>
{
    public ContestCountingCircleDetailsCountingMachineNotUnspecifiedValidationTest()
        : base(SharedProto.Validation.ContestCountingCircleDetailsCountingMachineNotUnspecified)
    {
    }

    [Fact]
    public void ShouldReturnEmptyWithDisabledCountingMachines()
    {
        var context = BuildValidationContext();
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);
        EnsureHasCount(validationResults, 0);
    }

    [Fact]
    public void ShouldReturnInvalidWhenUnspecified()
    {
        var context = BuildValidationContext(cantonDefaultsCustomizer: c => c.CountingMachineEnabled = true);
        context.CurrentContestCountingCircleDetails.CountingMachine = CountingMachine.Unspecified;
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);
        EnsureHasCount(validationResults, 1);
        EnsureIsValid(validationResults, false);
    }

    [Fact]
    public void ShouldReturnValidWhenSpecified()
    {
        var context = BuildValidationContext(cantonDefaultsCustomizer: c => c.CountingMachineEnabled = true);
        context.CurrentContestCountingCircleDetails.CountingMachine = CountingMachine.None;
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);
        EnsureHasCount(validationResults, 1);
        EnsureIsValid(validationResults, true);
    }
}
