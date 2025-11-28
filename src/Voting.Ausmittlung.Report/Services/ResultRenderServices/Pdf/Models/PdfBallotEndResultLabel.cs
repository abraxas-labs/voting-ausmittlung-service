// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

/// <summary>
/// Bitmask to set result label.
/// 6 bits are used.
/// bits 0-2 is the question count.
/// bits 3-5 is whether the question is accepted.
/// Eg. if there are 3 questions and question 1 and 3 are accepted
/// the bitmask is as follows:
/// 101100 => 44 => <see cref="QuestionCount3Question1And3Accepted"/>.
/// bit 5: Question 3 is accepted (<see cref="Question3Accepted"/>).
/// bit 3: Question 1 is accepted (<see cref="Question1Accepted"/>).
/// bit 2: Question count is 3 (<see cref="QuestionCount3NoneAccepted"/>).
/// </summary>
[Flags]
public enum PdfBallotEndResultLabel
{
    None = 0,

    QuestionCount1NotAccepted = 1 << 0,
    QuestionCount2NoneAccepted = 1 << 1,
    QuestionCount3NoneAccepted = 1 << 2,

    Question1Accepted = 1 << 3,
    Question2Accepted = 1 << 4,
    Question3Accepted = 1 << 5,

    QuestionCount1Accepted = QuestionCount1NotAccepted | Question1Accepted,

    QuestionCount2Question1Accepted = QuestionCount2NoneAccepted | Question1Accepted,
    QuestionCount2Question2Accepted = QuestionCount2NoneAccepted | Question2Accepted,
    QuestionCount2BothAccepted = QuestionCount2Question1Accepted | QuestionCount2Question2Accepted,

    QuestionCount3Question1Accepted = QuestionCount3NoneAccepted | Question1Accepted,
    QuestionCount3Question2Accepted = QuestionCount3NoneAccepted | Question2Accepted,
    QuestionCount3Question3Accepted = QuestionCount3NoneAccepted | Question3Accepted,
    QuestionCount3Question1And2Accepted = QuestionCount3Question1Accepted | QuestionCount3Question2Accepted,
    QuestionCount3Question1And3Accepted = QuestionCount3Question1Accepted | QuestionCount3Question3Accepted,
    QuestionCount3Question2And3Accepted = QuestionCount3Question2Accepted | QuestionCount3Question3Accepted,
    QuestionCount3AllAccepted = QuestionCount3Question1Accepted | QuestionCount3Question2Accepted | QuestionCount3Question3Accepted,
}
