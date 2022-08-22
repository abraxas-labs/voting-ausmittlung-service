// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class CountingCircleContactPerson : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string FamilyName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string MobilePhone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public CountingCircle? CountingCircleDuringEvent { get; set; }

    public Guid? CountingCircleDuringEventId { get; set; }

    public CountingCircle? CountingCircleAfterEvent { get; set; }

    public Guid? CountingCircleAfterEventId { get; set; }
}
