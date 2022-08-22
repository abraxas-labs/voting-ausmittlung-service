// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Data.Models;
using VotingCardResultDetail = Voting.Ausmittlung.Core.Domain.VotingCardResultDetail;

namespace Voting.Ausmittlung.Core.Validation;

public class VotingCardResultDetailValidator : AbstractValidator<VotingCardResultDetail>
{
    public VotingCardResultDetailValidator()
    {
        RuleFor(x => x.Valid).Must(x => x).Unless(x => x.Channel == VotingChannel.ByMail);
        RuleFor(x => x.Channel)
            .IsInEnum()
            .NotEqual(VotingChannel.Unspecified);
        RuleFor(x => x.DomainOfInfluenceType).IsInEnum().NotEqual(DomainOfInfluenceType.Unspecified);
        RuleFor(x => x.CountOfReceivedVotingCards).GreaterThanOrEqualTo(0);
    }
}
