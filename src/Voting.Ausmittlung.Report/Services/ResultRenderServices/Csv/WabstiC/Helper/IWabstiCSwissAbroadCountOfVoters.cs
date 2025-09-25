// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;

internal interface IWabstiCSwissAbroadCountOfVoters
{
    Guid CountingCircleId { get; }

    DomainOfInfluenceType DomainOfInfluenceType { get; }

    int? CountOfVotersTotalSwissAbroad { set; }
}
