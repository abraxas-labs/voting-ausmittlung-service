// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Services.Export;
using Voting.Ausmittlung.Report.Models;

namespace Voting.Ausmittlung.Test.Mocks;

public class DokConnectorMock : IDokConnector
{
    private static readonly TimeSpan TimeOut = TimeSpan.FromSeconds(10);

    private readonly Channel<Data> _channel
        = Channel.CreateBounded<Data>(1);

    public async Task<string> Save(string eaiMessageType, FileModel file, CancellationToken ct)
    {
        // getString works here, since pdf's are not really rendered
        var data = new Data(eaiMessageType, file, Encoding.UTF8.GetString(await file.ContentAsByteArray(ct)));
        await _channel.Writer.WriteAsync(data, ct).AsTask();
        return $"mock-id-{file.RenderContext.ContestId}-{file.Filename}";
    }

    internal async Task<Data> WaitForNextSave()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeOut);
        return await _channel.Reader.ReadAsync(cts.Token);
    }

    internal class Data
    {
        public Data(string eaiMessageType, FileModel fileModel, string fileContent)
        {
            EaiMessageType = eaiMessageType;
            FileModel = fileModel;
            FileContent = fileContent;
        }

        public string EaiMessageType { get; }

        public FileModel FileModel { get; }

        public string FileContent { get; }
    }
}
