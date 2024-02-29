// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Messaging;

public class ProportionalElectionBundleChangedMessageConsumer : LanguageAwareMessageConsumer<ProportionalElectionBundleChanged, ProportionalElectionResultBundle>
{
    private readonly IDbRepository<DataContext, ProportionalElectionResultBundle> _bundleRepo;
    private readonly ILogger<ProportionalElectionBundleChangedMessageConsumer> _logger;

    public ProportionalElectionBundleChangedMessageConsumer(
        IDbRepository<DataContext, ProportionalElectionResultBundle> bundleRepo,
        ILogger<ProportionalElectionBundleChangedMessageConsumer> logger,
        LanguageAwareMessageConsumerHub<ProportionalElectionBundleChanged, ProportionalElectionResultBundle> consumerHub,
        LanguageService languageService)
        : base(consumerHub, languageService)
    {
        _bundleRepo = bundleRepo;
        _logger = logger;
    }

    protected override async Task<ProportionalElectionResultBundle?> Transform(ProportionalElectionBundleChanged message)
    {
        var bundle = await _bundleRepo.Query()
            .Include(x => x.List!.Translations)
            .FirstOrDefaultAsync(x => x.Id == message.Id);
        if (bundle != null)
        {
            return bundle;
        }

        _logger.LogWarning(
            "Received proportional election result bundle changed message but could not find the bundle with id {Id}",
            message.Id);
        return null;
    }
}
