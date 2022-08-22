// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public static class DomainOfInfluenceTypeExtensions
{
    public static bool IsPolitical(this DomainOfInfluenceType type)
        => type is >= DomainOfInfluenceType.Ch and <= DomainOfInfluenceType.Sk;
}
