// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Core.Utils;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Test.Mocks;

public class ActionIdComparerMock : IActionIdComparer
{
    public const string ActionIdHashMock = "action-id-mock";

    public bool Compare(ActionId actionId, string actionIdHash)
    {
        return ActionIdHashMock.Equals(actionIdHash, StringComparison.Ordinal);
    }
}
