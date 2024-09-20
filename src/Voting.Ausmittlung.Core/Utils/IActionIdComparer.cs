// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Utils;

public interface IActionIdComparer
{
    bool Compare(ActionId actionId, string actionIdHash);
}
