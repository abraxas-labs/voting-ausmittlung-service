// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Voting.Ausmittlung.Test;

public static class AsyncStreamReaderExtensions
{
    public static async Task<List<T>> ReadNIgnoreCancellation<T>(
        this IAsyncStreamReader<T> responseStream,
        int responseCount,
        CancellationToken ct)
    {
        var responses = new List<T>();

        try
        {
            await foreach (var resp in responseStream.ReadAllAsync(ct))
            {
                responses.Add(resp);

                if (responseCount == responses.Count)
                {
                    return responses;
                }
            }
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.Cancelled)
        {
        }

        return responses;
    }
}
