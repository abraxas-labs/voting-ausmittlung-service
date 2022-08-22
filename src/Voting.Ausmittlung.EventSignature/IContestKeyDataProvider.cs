// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.EventSignature;

/// <summary>
/// A provider of contest key data for event signature.
/// </summary>
public interface IContestKeyDataProvider
{
    /// <summary>
    /// Executes a function with a contest cache read lock and passes the contest key data to the function handle.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="contestId">Contest id.</param>
    /// <param name="f">Function to execute in read lock.</param>
    /// <returns>Result of the function handle.</returns>
    T WithKeyData<T>(Guid contestId, Func<ContestCacheEntryKeyData?, T> f);
}
