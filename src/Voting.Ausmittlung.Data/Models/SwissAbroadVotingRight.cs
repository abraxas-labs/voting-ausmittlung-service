// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum SwissAbroadVotingRight
{
    /// <summary>
    /// Swiss abroad voting right is unspecified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Swiss abroad have voting rights on one separate counting circle.
    /// </summary>
    SeparateCountingCircle = 1,

    /// <summary>
    /// Swiss abroad have voting rights on every counting circle.
    /// </summary>
    OnEveryCountingCircle = 2,

    /// <summary>
    /// Swiss abroad don't have any voting rights.
    /// </summary>
    NoRights = 3,
}
