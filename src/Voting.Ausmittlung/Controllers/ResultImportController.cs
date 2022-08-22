// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Core.Services.Write.Import;

namespace Voting.Ausmittlung.Controllers;

[ApiController]
[Route("api/result_import")]
[Authorize]
public class ResultImportController
{
    private readonly ResultImportWriter _resultImportWriter;

    public ResultImportController(ResultImportWriter resultImportWriter)
    {
        _resultImportWriter = resultImportWriter;
    }

    /// <summary>
    /// Imports the results.
    /// </summary>
    /// <param name="contestId">The contestId of the contest to which the results should be imported.</param>
    /// <param name="file">The eCH file.</param>
    /// <returns>A task representing the async operation.</returns>
    [HttpPost("{contestId:Guid}")]
    public async Task Import(
        Guid contestId,
        IFormFile file)
    {
        await _resultImportWriter.Import(new ResultImportMeta(
            contestId,
            file.FileName,
            file.OpenReadStream()));
    }
}
