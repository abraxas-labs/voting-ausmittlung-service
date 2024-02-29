// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfVoteUtilTest
{
    [Theory]
    [InlineData(PdfBallotEndResultLabel.QuestionCount1Accepted, true)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount1NotAccepted, false)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount2BothAccepted, true, true)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount2Question1Accepted, true, false)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount2Question2Accepted, false, true)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount2NoneAccepted, false, false)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount3AllAccepted, true, true, true)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount3Question1And2Accepted, true, true, false)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount3Question1And3Accepted, true, false, true)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount3Question2And3Accepted, false, true, true)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount3Question1Accepted, true, false, false)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount3Question2Accepted, false, true, false)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount3Question3Accepted, false, false, true)]
    [InlineData(PdfBallotEndResultLabel.QuestionCount3NoneAccepted, false, false, false)]
    public void SetResultLabelShouldSetCorrectLabel(PdfBallotEndResultLabel expectedLabel, params bool[] accepted)
    {
        var ballotEndResult = new PdfBallotEndResult
        {
            QuestionEndResults = accepted.Select(x => new PdfBallotQuestionEndResult { Accepted = x }).ToList(),
        };

        var vote = new PdfVote
        {
            EndResult = new()
            {
                BallotEndResults = new()
                {
                    ballotEndResult,
                },
            },
        };

        PdfVoteUtil.SetResultLabel(vote);
        ballotEndResult.QuestionEndResultLabel
            .Should()
            .Be(expectedLabel);
    }
}
