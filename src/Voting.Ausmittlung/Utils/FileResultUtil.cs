// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Lib.Common;
using Voting.Lib.Rest.Files;

namespace Voting.Ausmittlung.Utils;

internal static class FileResultUtil
{
    public static async Task<FileResult> CreateFileResult(
        IAsyncEnumerable<FileModelWrapper> fileModels,
        bool isMultiExport,
        IClock clock,
        CancellationToken ct)
    {
        if (isMultiExport)
        {
            return SingleFileResult.CreateZipFile(fileModels, "export.zip", clock.UtcNow.ConvertUtcTimeToSwissTime(), ct);
        }

        var enumerator = fileModels.GetAsyncEnumerator(ct);
        if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("At least one file is required");
        }

        var file = enumerator.Current;
        if (await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException("At maximum one files is supported if " + nameof(isMultiExport) + " is false");
        }

        return SingleFileResult.Create(file, ct);
    }
}
