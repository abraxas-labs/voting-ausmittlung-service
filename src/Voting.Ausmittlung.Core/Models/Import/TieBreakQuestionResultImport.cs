// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Models.Import;

public class TieBreakQuestionResultImport
{
    public TieBreakQuestionResultImport(int questionNumber)
    {
        QuestionNumber = questionNumber;
    }

    public int CountQ1 { get; internal set; }

    public int CountQ2 { get; internal set; }

    public int CountUnspecified { get; internal set; }

    public int QuestionNumber { get; }
}
