// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Voting.Lib.DmDoc;

namespace Voting.Ausmittlung.Report.Services;

public class DmDocPdfService : IPdfService
{
    private readonly IDmDocService _dmDoc;

    public DmDocPdfService(IDmDocService dmDoc)
    {
        _dmDoc = dmDoc;
    }

    public Task<byte[]> Render<T>(string templateName, T data)
        => _dmDoc.FinishAsPdf(templateName, data);
}
