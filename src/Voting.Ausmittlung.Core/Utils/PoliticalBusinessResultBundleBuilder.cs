// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class PoliticalBusinessResultBundleBuilder
{
    private readonly IDbRepository<DataContext, ProtocolExport> _protocolExportRepo;
    private readonly PermissionService _permissionService;

    public PoliticalBusinessResultBundleBuilder(
        IDbRepository<DataContext, ProtocolExport> protocolExportRepo,
        PermissionService permissionService)
    {
        _protocolExportRepo = protocolExportRepo;
        _permissionService = permissionService;
    }

    internal async Task AddProtocolExportsToBundles<TBundle>(
        ICollection<TBundle> bundles,
        Guid basisCountingCircleId,
        Guid politicalBusinessId,
        Guid contestId,
        bool testingPhaseEnded,
        string templateKey)
        where TBundle : PoliticalBusinessBundle
    {
        var bundleIdsByProtocolExportIds = new Dictionary<Guid, Guid>();
        foreach (var bundle in bundles)
        {
            var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
                templateKey,
                _permissionService.TenantId,
                basisCountingCircleId,
                politicalBusinessId,
                politicalBusinessResultBundleId: bundle.Id);

            var protocolExportId = AusmittlungUuidV5.BuildProtocolExport(
                contestId,
                testingPhaseEnded,
                exportTemplateId);

            bundleIdsByProtocolExportIds.Add(bundle.Id, protocolExportId);
        }

        var protocolExportsById = await _protocolExportRepo.Query()
            .Where(x => bundleIdsByProtocolExportIds.Values.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        foreach (var bundle in bundles)
        {
            if (!bundleIdsByProtocolExportIds.TryGetValue(bundle.Id, out var protocolExportId))
            {
                continue;
            }

            if (!protocolExportsById.TryGetValue(protocolExportId, out var protocolExport))
            {
                continue;
            }

            bundle.ProtocolExport = protocolExport;
        }
    }
}
