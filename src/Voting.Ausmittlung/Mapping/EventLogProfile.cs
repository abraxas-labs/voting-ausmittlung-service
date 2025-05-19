// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Proto = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class EventLogProfile : Profile
{
    public EventLogProfile()
    {
        CreateMap<EventProcessedMessageDetails, Proto.EventDetails>()
            .ForAllMembers(opts => opts.Condition((_, _, src) => src != null));
        CreateMap<PoliticalBusinessResultBundleLogMessageDetail, Proto.PoliticalBusinessResultBundleLog>();
        CreateMap<ResultImportCountingCircleCompletedMessageDetail, Proto.ResultImportCountingCircleCompletedEventDetails>();
        CreateMap<WriteInsMappedMessageDetail, Proto.WriteInsMappedEventDetails>();
        CreateMap<ProtocolExportStateChangeEventDetail, Proto.ProtocolExportStateChangeEventDetails>();
    }
}
