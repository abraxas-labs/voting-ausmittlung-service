// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Text;
using System.Threading.Tasks;
using Voting.Ausmittlung.Report.Services;
using Voting.Lib.DmDoc.Serialization.Xml;

namespace Voting.Ausmittlung.Test.Mocks;

/// <summary>
/// Pdf service mock which returns the documatrix xml for testing purposes.
/// </summary>
public class PdfServiceMock : IPdfService
{
    public Task<byte[]> Render<T>(string templateName, T data)
    {
        return Task.FromResult(Encoding.UTF8.GetBytes(DmDocXmlSerializer.Serialize(data)));
    }
}
