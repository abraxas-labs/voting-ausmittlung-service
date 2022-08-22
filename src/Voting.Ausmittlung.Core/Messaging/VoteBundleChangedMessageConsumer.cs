// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.Messaging;

public class VoteBundleChangedMessageConsumer : MessageConsumer<VoteBundleChanged, VoteResultBundle>
{
    private readonly IDbRepository<DataContext, VoteResultBundle> _bundleRepo;
    private readonly ILogger<VoteBundleChangedMessageConsumer> _logger;

    public VoteBundleChangedMessageConsumer(
        IDbRepository<DataContext, VoteResultBundle> bundleRepo,
        ILogger<VoteBundleChangedMessageConsumer> logger,
        MessageConsumerHub<VoteBundleChanged, VoteResultBundle> consumerHub)
    : base(consumerHub)
    {
        _bundleRepo = bundleRepo;
        _logger = logger;
    }

    protected override async Task<VoteResultBundle?> Transform(VoteBundleChanged message)
    {
        var bundle = await _bundleRepo.GetByKey(message.Id);
        if (bundle != null)
        {
            return bundle;
        }

        _logger.LogWarning("Received vote result bundle changed message but could not find the bundle with id {Id}", message.Id);
        return null;
    }
}
