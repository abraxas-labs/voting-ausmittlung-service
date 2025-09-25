// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;

internal interface IWabstiCPoliticalResultData
{
    CountingCircleResultState ResultState { get; }

    void ResetDataIfSubmissionNotDone();
}
