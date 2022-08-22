// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;

internal interface IWabstiCContestDetails : IWabstiCSwissAbroadCountOfVoters
{
    int VotingCardsBallotBox { set; }

    int VotingCardsPaper { set; }

    int VotingCardsByMail { set; }

    int VotingCardsByMailNotValid { set; }

    int VotingCardsEVoting { set; }

    DomainOfInfluenceType DomainOfInfluenceType { get; }
}
