// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models.Mapping;

public class PdfProportionalElectionEndResultProfile : Profile
{
    public PdfProportionalElectionEndResultProfile()
    {
        CreateMap<ProportionalElectionEndResult, PdfProportionalElectionEndResult>()
            .ForMember(dst => dst.Calculation, opts => opts.MapFrom(x => x));
        CreateMap<ProportionalElectionListEndResult, PdfProportionalElectionListEndResult>();
        CreateMap<ProportionalElectionCandidateEndResult, PdfProportionalElectionCandidateEndResult>();
        CreateMap<ProportionalElectionCandidateVoteSourceEndResult, PdfProportionalElectionCandidateVoteSourceResult>();

        CreateMap<ProportionalElectionEndResult, PdfProportionalElectionEndResultCalculation>()
            .ForMember(
                dst => dst.HagenbachBischoffListUnionGroups,
                opts => opts.MapFrom(x => x.HagenbachBischoffRootGroup!
                    .AllGroups
                    .Where(g => g.Type == HagenbachBischoffGroupType.ListUnion)))
            .ForMember(
                dst => dst.HagenbachBischoffSubListUnionGroups,
                opts => opts.MapFrom(x => x.HagenbachBischoffRootGroup!
                    .AllGroups
                    .Where(g => g.Type == HagenbachBischoffGroupType.SubListUnion)));
        CreateMap<HagenbachBischoffCalculationRound, PdfHagenbachBischoffCalculationRound>()
            .ForMember(
                dst => dst.Winner,
                opts => opts.MapFrom(src => src.GroupValues.First(x => x.IsWinner)));
        CreateMap<HagenbachBischoffCalculationRoundGroupValues, PdfHagenbachBischoffSimpleWinnerGroup>()
            .IncludeMembers(dst => dst.Group);
        CreateMap<HagenbachBischoffGroup, PdfHagenbachBischoffSimpleWinnerGroup>();
        CreateMap<HagenbachBischoffGroup, PdfHagenbachBischoffInitialGroupValues>()
            .ForMember(dst => dst.NumberOfMandates, opts => opts.MapFrom(x => x.InitialNumberOfMandates));
        CreateMap<HagenbachBischoffCalculationRoundGroupValues, PdfHagenbachBischoffCalculationRoundGroupValues>();
        CreateMap<HagenbachBischoffGroup, PdfHagenbachBischoffSimpleGroup>();
        CreateMap<HagenbachBischoffGroup, PdfProportionalElectionListUnionEndResultEntry>();
        CreateMap<HagenbachBischoffGroup, PdfHagenbachBischoffGroup>()
            .ForMember(
                dst => dst.InitialDistributionValues,
                opts => opts.MapFrom(src => src.ChildrenOrdered));
    }
}
