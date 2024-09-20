// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class ElectionCandidate : BaseEntity
{
    public string Number { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PoliticalFirstName { get; set; } = string.Empty;

    public string PoliticalLastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public SexType Sex { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool Incumbent { get; set; }

    public string ZipCode { get; set; } = string.Empty;

    public string Locality { get; set; } = string.Empty;

    public int Position { get; set; }

    public abstract string Occupation { get; }

    public abstract string OccupationTitle { get; }

    public string Origin { get; set; } = string.Empty;

    public int CheckDigit { get; set; }
}
