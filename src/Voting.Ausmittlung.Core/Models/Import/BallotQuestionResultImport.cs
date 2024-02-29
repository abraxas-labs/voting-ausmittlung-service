// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Models.Import;

public class BallotQuestionResultImport
{
    public BallotQuestionResultImport(int questionNumber)
    {
        QuestionNumber = questionNumber;
    }

    public int CountYes { get; internal set; }

    public int CountNo { get; internal set; }

    public int CountUnspecified { get; internal set; }

    public int QuestionNumber { get; }
}
