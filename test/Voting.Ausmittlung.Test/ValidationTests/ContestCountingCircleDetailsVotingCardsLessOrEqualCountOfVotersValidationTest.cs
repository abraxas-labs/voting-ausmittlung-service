// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ValidationTests;

public class ContestCountingCircleDetailsVotingCardsLessOrEqualCountOfVotersValidationTest : BaseValidationTest<ContestCountingCircleDetailsValidator, ContestCountingCircleDetails>
{
    public ContestCountingCircleDetailsVotingCardsLessOrEqualCountOfVotersValidationTest()
        : base(SharedProto.Validation.ContestCountingCircleDetailsVotingCardsLessOrEqualCountOfVoters)
    {
    }

    [Fact]
    public void Test()
    {
        var context = BuildValidationContext();
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);

        EnsureHasCount(validationResults, 1);
        EnsureIsValid(validationResults, true);
        validationResults.MatchSnapshot();
    }

    [Fact]
    public void ShouldConsiderDomainOfInfluenceVoterTypes()
    {
        var doiWithForeignVoters = new DomainOfInfluence
        {
            Type = DomainOfInfluenceType.An,
            HasForeignerVoters = true,
        };

        var doiWithoutForeignVoters = new DomainOfInfluence
        {
            Type = DomainOfInfluenceType.An,
        };

        // Sub total in details: 850 excl. foreign voters, 870 including foreign voters.
        // We set 870 voting cards, to test that the one which includes foreigners should be valid and the other one is invalid.
        var votingCards = new List<VotingCardResultDetail>
        {
            new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfReceivedVotingCards = 860 },
            new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = false, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfReceivedVotingCards = 10 },
        };

        var contextWithForeignVoters = BuildValidationContext(doiWithForeignVoters, PoliticalBusinessType.Vote);
        contextWithForeignVoters.CurrentContestCountingCircleDetails.VotingCards = votingCards;

        var validationResults = Validate(contextWithForeignVoters.CurrentContestCountingCircleDetails, contextWithForeignVoters);
        EnsureIsValid(validationResults, true);

        var contextWithoutForeignVoters = BuildValidationContext(doiWithoutForeignVoters, PoliticalBusinessType.Vote);
        contextWithoutForeignVoters.CurrentContestCountingCircleDetails.VotingCards = votingCards;
        validationResults = Validate(contextWithoutForeignVoters.CurrentContestCountingCircleDetails, contextWithoutForeignVoters);
        EnsureIsValid(validationResults, false);
    }
}
