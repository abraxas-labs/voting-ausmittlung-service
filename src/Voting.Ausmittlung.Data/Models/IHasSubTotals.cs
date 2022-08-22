// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface IHasSubTotals<TSubTotal>
{
    TSubTotal EVotingSubTotal { get; set; }

    TSubTotal ConventionalSubTotal { get; set; }
}

public interface IHasSubTotals<TSubTotal, TNullableSubTotal>
    where TNullableSubTotal : INullableSubTotal<TSubTotal>
{
    TSubTotal EVotingSubTotal { get; set; }

    TNullableSubTotal ConventionalSubTotal { get; set; }
}
