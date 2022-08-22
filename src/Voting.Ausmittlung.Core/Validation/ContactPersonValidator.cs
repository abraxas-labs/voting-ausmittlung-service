// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Validation;

public class ContactPersonValidator : AbstractValidator<ContactPerson>
{
    public ContactPersonValidator()
    {
        RuleFor(v => v.Email).EmailAddress().Unless(x => string.IsNullOrEmpty(x.Email));
        RuleFor(v => v.Phone).NotEmpty();
    }
}
