// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.Messaging;

public class MajorityElectionBundleChangedMessageConsumer : MessageConsumer<MajorityElectionBundleChanged, MajorityElectionResultBundle>
{
    private readonly IDbRepository<DataContext, MajorityElectionResultBundle> _bundleRepo;
    private readonly ILogger<MajorityElectionBundleChangedMessageConsumer> _logger;

    public MajorityElectionBundleChangedMessageConsumer(
        IDbRepository<DataContext, MajorityElectionResultBundle> bundleRepo,
        ILogger<MajorityElectionBundleChangedMessageConsumer> logger,
        MessageConsumerHub<MajorityElectionBundleChanged, MajorityElectionResultBundle> consumerHub)
        : base(consumerHub)
    {
        _bundleRepo = bundleRepo;
        _logger = logger;
    }

    protected override async Task<MajorityElectionResultBundle?> Transform(MajorityElectionBundleChanged message)
    {
        var bundle = await _bundleRepo.GetByKey(message.Id);
        if (bundle != null)
        {
            return bundle;
        }

        _logger.LogWarning(
            "Received majority election result bundle changed message but could not find the bundle with id {Idd}",
            message.Id);
        return null;
    }
}
