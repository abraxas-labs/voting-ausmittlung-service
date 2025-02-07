// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using FluentValidation;

namespace Voting.Ausmittlung.Core.Exceptions;

[System.Serializable]
public class SecondaryMajorityElectionCandidateNotSelectedInPrimaryElectionException : ValidationException
{
    public SecondaryMajorityElectionCandidateNotSelectedInPrimaryElectionException()
        : base(
            "Cannot select a referenced candidate in a secondary election if the candidate is not selected in the primary election")
    {
    }
}
