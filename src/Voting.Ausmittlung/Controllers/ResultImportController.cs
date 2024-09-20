// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Core.Services.Write.Import;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.Rest.Files;
using Voting.Lib.Rest.Utils;

namespace Voting.Ausmittlung.Controllers;

[ApiController]
[Route("api/result_import")]
public class ResultImportController : ControllerBase
{
    private const long MaxImportRequestSize = 250 * 1000 * 1000; // 250MB
    private const string Ech0222FormName = "ech0222File";
    private const string Ech0110FormName = "ech0110File";
    private const int BufferSize = 4096;

    private readonly ResultImportWriter _resultImportWriter;
    private readonly MultipartRequestHelper _multipartRequestHelper;

    public ResultImportController(ResultImportWriter resultImportWriter, MultipartRequestHelper multipartRequestHelper)
    {
        _resultImportWriter = resultImportWriter;
        _multipartRequestHelper = multipartRequestHelper;
    }

    /// <summary>
    /// Imports the results.
    /// </summary>
    /// <param name="contestId">The contestId of the contest to which the results should be imported.</param>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>A task representing the async operation.</returns>
    [AuthorizePermission(Permissions.Import.ImportData)]
    [HttpPost("{contestId:Guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImportRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImportRequestSize)]
    [DisableFormValueModelBinding]
    public async Task Import(Guid contestId, CancellationToken ct)
    {
        Stream? ech0222Stream = null;
        Stream? ech0110Stream = null;
        string? ech0222FileName = null;
        string? ech0110FileName = null;

        try
        {
            await _multipartRequestHelper.ReadFiles(
                Request,
                async multipartFile =>
                {
                    switch (multipartFile.FormFieldName)
                    {
                        case Ech0222FormName:
                            ech0222Stream = await BufferToTemporaryFile(multipartFile.Content);
                            ech0222FileName = multipartFile.FileName;
                            break;
                        case Ech0110FormName:
                            ech0110Stream = await BufferToTemporaryFile(multipartFile.Content);
                            ech0110FileName = multipartFile.FileName;
                            break;
                    }
                },
                [MediaTypeNames.Text.Xml]);

            if (ech0222Stream == null || ech0222FileName == null || ech0110Stream == null || ech0110FileName == null)
            {
                throw new ValidationException("Did not receive a eCH-0222 and eCH-0110 file or their names.");
            }

            await _resultImportWriter.Import(
                new ResultImportMeta(
                    contestId,
                    ech0222FileName,
                    ech0222Stream,
                    ech0110FileName,
                    ech0110Stream),
                ct);
        }
        finally
        {
            if (ech0222Stream != null)
            {
                await ech0222Stream.DisposeAsync().ConfigureAwait(false);
            }

            if (ech0110Stream != null)
            {
                await ech0110Stream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    // We support very high file sizes, which may be even too big for MemoryStreams.
    // Buffer them to temporary files to allow seeking.
    // The temporary files are automatically deleted after the stream is disposed.
    private async Task<Stream> BufferToTemporaryFile(Stream stream)
    {
        var filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var fs = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            BufferSize,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        await stream.CopyToAsync(fs);
        fs.Seek(0, SeekOrigin.Begin);
        return fs;
    }
}
