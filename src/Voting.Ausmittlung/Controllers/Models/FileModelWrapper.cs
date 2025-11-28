// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.Common.Files;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Controllers.Models;

public class FileModelWrapper : IFile
{
    private readonly FileModel _fileModel;

    public FileModelWrapper(FileModel fileModel)
    {
        _fileModel = fileModel;
    }

    public string FileName => _fileModel.Filename;

    public string MimeType => _fileModel.Format.GetMimeType();

    public Task Write(PipeWriter writer, CancellationToken ct = default) => _fileModel.Write(writer, ct);
}
