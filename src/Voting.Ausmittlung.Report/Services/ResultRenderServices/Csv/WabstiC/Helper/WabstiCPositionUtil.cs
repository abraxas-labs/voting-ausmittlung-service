// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;

public static class WabstiCPositionUtil
{
    public static string BuildPosition(int position, VoteType voteType)
    {
        return voteType == VoteType.QuestionsOnSingleBallot ? string.Empty : position switch
        {
            1 => "a",
            2 => "b",
            3 => "c",
            4 => "d",
            5 => "e",
            6 => "f",
            _ => string.Empty,
        };
    }
}
