// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Authorization;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Core.Services.Permission;

public sealed class PermissionAccessor : IAsyncDisposable
{
    private readonly AsyncLock _lock = new();
    private readonly PermissionService _permissionService;
    private readonly IAuth _auth;
    private readonly ResultExportTemplateReader _templateReader;

    private bool _permissionsLoaded;
    private IReadOnlySet<Guid> _accessibleResultIds = new HashSet<Guid>();
    private IReadOnlySet<Guid> _accessibleBasisCountingCircleIds = new HashSet<Guid>();
    private IReadOnlySet<Guid> _accessibleProtocolIds = new HashSet<Guid>();
    private IReadOnlySet<Guid> _ownedPoliticalBusinessIds = new HashSet<Guid>();
    private IReadOnlyDictionary<Guid, (Guid BasisCcId, Guid PbId)> _resultIdMapping = new Dictionary<Guid, (Guid, Guid)>();
    private Guid _contestId;
    private Guid? _basisCountingCircleId;
    private bool _testingPhaseEnded;

    public PermissionAccessor(PermissionService permissionService, IAuth auth, ResultExportTemplateReader templateReader)
    {
        _permissionService = permissionService;
        _auth = auth;
        _templateReader = templateReader;
    }

    public async ValueTask DisposeAsync() => await _lock.DisposeAsync();

    internal void SetContextIds(Guid? basisCountingCircleId, Guid contestId, bool testingPhaseEnded)
    {
        _basisCountingCircleId = basisCountingCircleId;
        _contestId = contestId;
        _testingPhaseEnded = testingPhaseEnded;
        _permissionsLoaded = false;
    }

    internal async Task<bool> CanRead(EventProcessedMessage msg)
    {
        if (!_permissionsLoaded)
        {
            await Reload();
        }

        SetMissingData(msg);

        if (msg.PoliticalBusinessEndResultId.HasValue
            && msg.PoliticalBusinessId.HasValue
            && _ownedPoliticalBusinessIds.Contains(msg.PoliticalBusinessId.Value))
        {
            return true;
        }

        if (msg.BasisCountingCircleId.HasValue
            && ((_basisCountingCircleId.HasValue && msg.BasisCountingCircleId.Value != _basisCountingCircleId.Value)
                || !_accessibleBasisCountingCircleIds.Contains(msg.BasisCountingCircleId.Value)))
        {
            return false;
        }

        if (msg.PoliticalBusinessResultId.HasValue
            && !_accessibleResultIds.Contains(msg.PoliticalBusinessResultId.Value))
        {
            return false;
        }

        if (msg.ProtocolExportId.HasValue
            && !_accessibleProtocolIds.Contains(msg.ProtocolExportId.Value))
        {
            return false;
        }

        // only counting circles
        // or results related events are supported
        return msg.BasisCountingCircleId.HasValue
            || msg.PoliticalBusinessResultId.HasValue
            || msg.ProtocolExportId.HasValue;
    }

    private void SetMissingData(EventProcessedMessage msg)
    {
        // set result id if not present
        if (msg is { PoliticalBusinessResultId: null, BasisCountingCircleId: not null, PoliticalBusinessId: not null, ContestId: not null })
        {
            msg.PoliticalBusinessResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(
                msg.PoliticalBusinessId.Value,
                msg.BasisCountingCircleId.Value,
                _testingPhaseEnded);
        }

        // set basis counting circle id and political business id if not set
        if (msg.PoliticalBusinessResultId != null
            && (msg.PoliticalBusinessId == null || msg.BasisCountingCircleId == null)
            && _resultIdMapping.TryGetValue(msg.PoliticalBusinessResultId.Value, out var ids))
        {
            (msg.BasisCountingCircleId, msg.PoliticalBusinessId) = ids;
        }
    }

    private async Task Reload()
    {
        if (_permissionsLoaded)
        {
            return;
        }

        using var locker = await _lock.AcquireAsync();
        if (_permissionsLoaded)
        {
            return;
        }

        // only pdf's are async generated
        _accessibleProtocolIds = _auth.HasPermission(_basisCountingCircleId.HasValue ? Permissions.Export.ExportData : Permissions.Export.ExportMonitoringData)
            ? await _templateReader.GetReadableProtocolExportIds(_contestId, _basisCountingCircleId, [ExportFileFormat.Pdf])
            : new HashSet<Guid>();

        // only monitoring needs owned political business ids
        _ownedPoliticalBusinessIds = _auth.HasPermission(Permissions.PoliticalBusinessEndResult.Read)
            ? await _permissionService.GetOwnedPoliticalBusinessIds(_contestId)
            : new HashSet<Guid>();

        var accessibleIds =
            !_auth.HasPermission(Permissions.PoliticalBusinessResult.Read) && !_auth.HasPermission(Permissions.PoliticalBusinessResult.ReadOverview)
            ? new HashSet<PermissionService.PoliticalBusinessResultIds>()
            : await _permissionService.GetReadableResultIds(_contestId);
        _accessibleBasisCountingCircleIds = accessibleIds.Select(x => x.BasisCountingCircleId).ToHashSet();
        _accessibleResultIds = accessibleIds.Select(x => x.PoliticalBusinessResultId).ToHashSet();
        _resultIdMapping = accessibleIds.ToDictionary(
            x => x.PoliticalBusinessResultId,
            x => (x.BasisCountingCircleId, x.PoliticalBusinessId));
        _permissionsLoaded = true;
    }
}
