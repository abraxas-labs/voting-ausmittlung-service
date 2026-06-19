// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class RationalNumber
{
    public decimal Quotient { get; set; }

    public string Dividend { get; set; } = string.Empty;

    public string Divisor { get; set; } = string.Empty;
}
