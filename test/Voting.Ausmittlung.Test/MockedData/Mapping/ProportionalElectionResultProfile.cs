// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.MockedData.Mapping;

public class ProportionalElectionResultProfile : Profile
{
    public ProportionalElectionResultProfile()
    {
        CreateMap<ProportionalElectionResultBallotCandidate, DomainModels.ProportionalElectionResultBallotCandidate>();
    }
}