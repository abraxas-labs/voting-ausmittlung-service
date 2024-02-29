// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Lib.Iam.Authorization;

namespace Voting.Ausmittlung.Core.Authorization;

public class PermissionProvider : IPermissionProvider
{
    private readonly Dictionary<string, HashSet<string>> _permissionsPerRole = new();

    public PermissionProvider(AppConfig appConfig)
    {
        var erfassungCreator = $"{appConfig.SecureConnect.AppShortNameErfassung}::Erfasser";
        var erfassungElectionAdmin = $"{appConfig.SecureConnect.AppShortNameErfassung}::Wahlverwalter";
        var monitoringElectionAdmin = $"{appConfig.SecureConnect.AppShortNameMonitoring}::Wahlverwalter";

        _permissionsPerRole[erfassungCreator] = new HashSet<string>
        {
            Permissions.Contest.Read,

            Permissions.PoliticalBusiness.ReadAccessible,

            Permissions.PoliticalBusinessResult.Read,
            Permissions.PoliticalBusinessResult.ReadComments,

            Permissions.PoliticalBusinessResultBundle.Read,
            Permissions.PoliticalBusinessResultBundle.Create,
            Permissions.PoliticalBusinessResultBundle.FinishSubmission,
            Permissions.PoliticalBusinessResultBundle.Review,

            Permissions.PoliticalBusinessResultBallot.Read,
            Permissions.PoliticalBusinessResultBallot.Create,
            Permissions.PoliticalBusinessResultBallot.Update,
            Permissions.PoliticalBusinessResultBallot.Delete,

            Permissions.VoteBallotResult.Read,

            Permissions.MajorityElectionCandidate.Read,

            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionList.Read,

            Permissions.Export.ExportData,

            Permissions.CantonDefaults.Read,
        };

        _permissionsPerRole[erfassungElectionAdmin] = new HashSet<string>
        {
            Permissions.Contest.Read,

            Permissions.PoliticalBusiness.ReadAccessible,

            Permissions.PoliticalBusinessResult.Read,
            Permissions.PoliticalBusinessResult.ReadComments,
            Permissions.PoliticalBusinessResult.EnterResults,
            Permissions.PoliticalBusinessResult.ResetResults,
            Permissions.PoliticalBusinessResult.StartSubmission,
            Permissions.PoliticalBusinessResult.FinishSubmission,

            Permissions.PoliticalBusinessResultBundle.Read,
            Permissions.PoliticalBusinessResultBundle.Create,
            Permissions.PoliticalBusinessResultBundle.UpdateAll,
            Permissions.PoliticalBusinessResultBundle.Delete,
            Permissions.PoliticalBusinessResultBundle.FinishSubmission,
            Permissions.PoliticalBusinessResultBundle.Review,

            Permissions.PoliticalBusinessResultBallot.Read,
            Permissions.PoliticalBusinessResultBallot.ReadAll,
            Permissions.PoliticalBusinessResultBallot.Create,
            Permissions.PoliticalBusinessResultBallot.Update,
            Permissions.PoliticalBusinessResultBallot.Delete,

            Permissions.VoteBallotResult.Read,

            Permissions.MajorityElectionCandidate.Read,
            Permissions.MajorityElectionBallotGroupResult.Read,

            Permissions.MajorityElectionWriteIn.Read,
            Permissions.MajorityElectionWriteIn.Update,

            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionList.Read,
            Permissions.ProportionalElectionListResult.Read,

            Permissions.CountingCircleContactPerson.Create,
            Permissions.CountingCircleContactPerson.Update,

            Permissions.ContestCountingCircleDetails.Update,
            Permissions.ContestCountingCircleElectorate.Update,

            Permissions.Import.ListenToImportChanges,

            Permissions.Export.ExportData,

            Permissions.CantonDefaults.Read,
        };

        _permissionsPerRole[monitoringElectionAdmin] = new HashSet<string>
        {
            Permissions.Contest.Read,

            Permissions.PoliticalBusiness.ReadOwned,

            Permissions.PoliticalBusinessResult.Read,
            Permissions.PoliticalBusinessResult.ReadComments,
            Permissions.PoliticalBusinessResult.ReadOverview,
            Permissions.PoliticalBusinessResult.Audit,

            Permissions.PoliticalBusinessResultBundle.Read,

            Permissions.PoliticalBusinessResultBallot.Read,
            Permissions.PoliticalBusinessResultBallot.ReadAll,

            Permissions.PoliticalBusinessEndResult.Read,
            Permissions.PoliticalBusinessEndResult.Finalize,

            Permissions.PoliticalBusinessEndResultLotDecision.Read,
            Permissions.PoliticalBusinessEndResultLotDecision.Update,

            Permissions.VoteBallotResult.Read,

            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionList.Read,
            Permissions.ProportionalElectionListResult.Read,

            Permissions.ProportionalElectionEndResult.EnterManualResults,

            Permissions.MajorityElectionCandidate.Read,
            Permissions.MajorityElectionBallotGroupResult.Read,

            Permissions.PoliticalBusinessUnion.Read,

            Permissions.Import.ImportData,
            Permissions.Import.Read,
            Permissions.Import.Delete,

            Permissions.Export.ExportData,
            Permissions.Export.ExportMonitoringData,
            Permissions.Export.ExportActivityProtocol,

            Permissions.ExportConfiguration.Read,
            Permissions.ExportConfiguration.Update,
            Permissions.ExportConfiguration.Trigger,

            Permissions.CantonDefaults.Read,
        };
    }

    public IReadOnlyCollection<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>();
        foreach (var role in roles)
        {
            if (_permissionsPerRole.TryGetValue(role, out var rolePermissions))
            {
                permissions.UnionWith(rolePermissions);
            }
        }

        return permissions;
    }
}
