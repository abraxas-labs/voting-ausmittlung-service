// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface ISummableSubTotal<in TNonNullableSelf>
{
    void Add(TNonNullableSelf other, int deltaFactor = 1);
}
