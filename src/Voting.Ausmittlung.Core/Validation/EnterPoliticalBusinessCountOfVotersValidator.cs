// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class EnterPoliticalBusinessCountOfVotersValidator : AbstractValidator<PoliticalBusinessCountOfVoters>
{
    public EnterPoliticalBusinessCountOfVotersValidator()
    {
        RuleFor(x => x.ConventionalInvalidBallots).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ConventionalAccountedBallots).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ConventionalBlankBallots).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ConventionalReceivedBallots).GreaterThanOrEqualTo(0);
    }
}
