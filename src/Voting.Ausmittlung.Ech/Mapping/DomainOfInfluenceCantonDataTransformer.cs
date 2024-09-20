// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

public static class DomainOfInfluenceCantonDataTransformer
{
    internal static string EchCandidateDateOfBirthText(DomainOfInfluenceCanton canton, DateTime dateOfBirth)
    {
        return canton switch
        {
            DomainOfInfluenceCanton.Sg => $"{dateOfBirth:yyyy}",
            _ => $"{dateOfBirth:dd.MM.yyyy}",
        };
    }

    internal static string? EchCandidatePartyText(DomainOfInfluenceCanton canton, PoliticalBusinessType politicalBusinessType, string? party)
    {
        if (politicalBusinessType == PoliticalBusinessType.MajorityElection ||
            politicalBusinessType == PoliticalBusinessType.SecondaryMajorityElection)
        {
            return party;
        }
        else
        {
            return null;
        }
    }
}
