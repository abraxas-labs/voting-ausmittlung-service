// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;

namespace Voting.Ausmittlung.Report.Services;

public interface IPdfService
{
    Task<byte[]> Render<T>(string templateName, T data);
}
