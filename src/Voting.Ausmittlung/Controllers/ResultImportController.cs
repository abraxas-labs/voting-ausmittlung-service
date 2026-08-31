// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Core.Services.Write.Import;
using Voting.Ausmittlung.Core.Utils.ECountingSignature;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Utils;
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
    private const string ECountingImportFileExtension = "ecountsig";

    private readonly EVotingResultImportWriter _eVotingResultImportWriter;
    private readonly ECountingResultImportWriter _eCountingResultImportWriter;
    private readonly MultipartRequestHelper _multipartRequestHelper;
    private readonly ECountingEchFileDecryptor _eCountingEchFileDecryptor;

    public ResultImportController(
        EVotingResultImportWriter eVotingResultImportWriter,
        ECountingResultImportWriter eCountingResultImportWriter,
        MultipartRequestHelper multipartRequestHelper,
        ECountingEchFileDecryptor eCountingEchFileDecryptor)
    {
        _eVotingResultImportWriter = eVotingResultImportWriter;
        _eCountingResultImportWriter = eCountingResultImportWriter;
        _multipartRequestHelper = multipartRequestHelper;
        _eCountingEchFileDecryptor = eCountingEchFileDecryptor;
    }

    /// <summary>
    /// Imports the e-counting results.
    /// </summary>
    /// <param name="contestId">The contestId of the contest to which the results should be imported.</param>
    /// <param name="countingCircleId">The id of the counting circle.</param>
    /// <returns>A task representing the async operation.</returns>
    [AuthorizePermission(Permissions.Import.ImportECounting)]
    [HttpPost("e-counting/{contestId:Guid}/{countingCircleId:Guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImportRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImportRequestSize)]
    [DisableFormValueModelBinding]
    public Task ImportECounting(Guid contestId, Guid countingCircleId)
        => Import(ResultImportType.ECounting, contestId, countingCircleId);

    /// <summary>
    /// Imports the results.
    /// </summary>
    /// <param name="contestId">The contestId of the contest to which the results should be imported.</param>
    /// <returns>A task representing the async operation.</returns>
    [AuthorizePermission(Permissions.Import.ImportEVoting)]
    [HttpPost("e-voting/{contestId:Guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImportRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxImportRequestSize)]
    [DisableFormValueModelBinding]
    public Task ImportEVoting(Guid contestId)
        => Import(ResultImportType.EVoting, contestId, null);

    private async Task Import(ResultImportType importType, Guid contestId, Guid? countingCircleId)
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
                [MediaTypeNames.Text.Xml, MediaTypeNames.Application.Octet]);

            if (ech0222Stream == null || ech0222FileName == null)
            {
                throw new ValidationException("Did not receive a eCH-0222 file or its name.");
            }

            if (importType is ResultImportType.ECounting)
            {
                if (!ech0222FileName.EndsWith($".{ECountingImportFileExtension}"))
                {
                    throw new ValidationException($"E-Counting imports must have the {ECountingImportFileExtension} file extension");
                }

                if (IsXmlStream(ech0222Stream))
                {
                    throw new ResultImportECountingUnencryptedFileException();
                }
            }

            BaseCertificateInfo? eCountingClientCertificate = null;

            if (importType is ResultImportType.ECounting)
            {
                var (verifiedEch0222Stream, clientCertificateInfo) = await _eCountingEchFileDecryptor.VerifyAndExtractEch0222AndClientCertificate(ech0222Stream, countingCircleId!.Value);
                await ech0222Stream.DisposeAsync().ConfigureAwait(false);
                ech0222Stream = verifiedEch0222Stream;
                eCountingClientCertificate = clientCertificateInfo;
            }

            var importMeta = new ResultImportMeta(
                importType,
                Ech0222VersionFinder.GetEch0222Version(ech0222Stream),
                contestId,
                countingCircleId,
                ech0222FileName,
                ech0222Stream,
                ech0110FileName,
                ech0110Stream,
                eCountingClientCertificate);
            await RunImport(importMeta);
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

    private async Task RunImport(ResultImportMeta importMeta)
    {
        switch (importMeta.ImportType)
        {
            case ResultImportType.EVoting:
                await _eVotingResultImportWriter.Import(importMeta);
                break;
            case ResultImportType.ECounting:
                await _eCountingResultImportWriter.Import(importMeta);
                break;
            default:
                throw new InvalidOperationException("Unknown import type.");
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

    private bool IsXmlStream(Stream stream)
    {
        if (stream.Length == 0)
        {
            return false;
        }

        var originalStreamPosition = stream.Position;
        try
        {
            using var reader = XmlReader.Create(stream);
            return reader.Read();
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            stream.Position = originalStreamPosition;
        }
    }
}
