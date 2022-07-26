﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Voting.Ausmittlung.Report.Services.ResultRenderServices;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Report.Models;

public class FileModel
{
    private readonly Func<PipeWriter, CancellationToken, Task> _writer;

    public FileModel(
        ReportRenderContext renderContext,
        string filename,
        ExportFileFormat format,
        Func<PipeWriter, CancellationToken, Task> writer)
    {
        RenderContext = renderContext;
        _writer = writer;
        Filename = filename;
        Format = format;
    }

    public FileModel(
        ReportRenderContext renderContext,
        string filename,
        ExportFileFormat format,
        string echMessageId,
        Func<PipeWriter, CancellationToken, Task> writer)
        : this(renderContext, filename, format, writer)
    {
        EchMessageId = echMessageId;
    }

    public string Filename { get; }

    public ExportFileFormat Format { get; }

    public ReportRenderContext RenderContext { get; }

    public string? EchMessageId { get; }

    public Task Write(PipeWriter writer, CancellationToken ct = default) => _writer(writer, ct);

    public async Task<byte[]> ContentAsByteArray(CancellationToken ct = default)
    {
        await using var ms = new MemoryStream();
        var writer = PipeWriter.Create(ms);
        await Write(writer, ct);
        await writer.FlushAsync(ct);
        return ms.ToArray();
    }
}
