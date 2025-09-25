// (c) Copyright by Abraxas Informatik AG
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
        var erfassungCreatorWithoutBundleControl = $"{appConfig.SecureConnect.AppShortNameErfassung}::Erfasser ohne Bundkontrolle";
        var erfassungBundleController = $"{appConfig.SecureConnect.AppShortNameErfassung}::Bundkontrolleur";
        var erfassungElectionSupporter = $"{appConfig.SecureConnect.AppShortNameErfassung}::Wahlunterstützer";
        var erfassungElectionAdmin = $"{appConfig.SecureConnect.AppShortNameErfassung}::Wahlverwalter";

        var monitoringElectionAdmin = $"{appConfig.SecureConnect.AppShortNameMonitoring}::Wahlverwalter";
        var monitoringElectionSupporter = $"{appConfig.SecureConnect.AppShortNameMonitoring}::Wahlunterstützer";

        var reportExporterApi = $"{appConfig.SecureConnect.AppShortNameErfassung}::ReportExporterApi";

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

            Permissions.EventLog.Watch,
        };

        _permissionsPerRole[erfassungCreatorWithoutBundleControl] = new HashSet<string>
        {
            Permissions.Contest.Read,

            Permissions.PoliticalBusiness.ReadAccessible,

            Permissions.PoliticalBusinessResult.Read,
            Permissions.PoliticalBusinessResult.ReadComments,

            Permissions.PoliticalBusinessResultBundle.Read,
            Permissions.PoliticalBusinessResultBundle.Create,
            Permissions.PoliticalBusinessResultBundle.FinishSubmission,

            Permissions.PoliticalBusinessResultBallot.Read,
            Permissions.PoliticalBusinessResultBallot.Create,
            Permissions.PoliticalBusinessResultBallot.Update,
            Permissions.PoliticalBusinessResultBallot.Delete,

            Permissions.VoteBallotResult.Read,

            Permissions.MajorityElectionCandidate.Read,

            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionList.Read,

            Permissions.Export.ExportData,

            Permissions.EventLog.Watch,
        };

        _permissionsPerRole[erfassungBundleController] = new HashSet<string>
        {
            Permissions.Contest.Read,

            Permissions.PoliticalBusiness.ReadAccessible,

            Permissions.PoliticalBusinessResult.Read,
            Permissions.PoliticalBusinessResult.ReadComments,

            Permissions.PoliticalBusinessResultBundle.Read,
            Permissions.PoliticalBusinessResultBundle.Review,
            Permissions.PoliticalBusinessResultBallot.Update, // needs update permissions to perform reviews

            Permissions.PoliticalBusinessResultBallot.Read,

            Permissions.VoteBallotResult.Read,

            Permissions.MajorityElectionCandidate.Read,

            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionList.Read,

            Permissions.Export.ExportData,

            Permissions.EventLog.Watch,
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
            Permissions.PoliticalBusinessResult.FinishSubmissionAndAudit,

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

            Permissions.Import.ReadECounting,
            Permissions.Import.DeleteECounting,
            Permissions.Import.ImportECounting,

            Permissions.Export.ExportData,

            Permissions.EventLog.Watch,
        };

        _permissionsPerRole[erfassungElectionSupporter] = new HashSet<string>
        {
            Permissions.Contest.Read,

            Permissions.PoliticalBusiness.ReadAccessible,

            Permissions.PoliticalBusinessResult.Read,
            Permissions.PoliticalBusinessResult.ReadComments,
            Permissions.PoliticalBusinessResult.EnterResults,
            Permissions.PoliticalBusinessResult.ResetResults,

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

            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionList.Read,
            Permissions.ProportionalElectionListResult.Read,

            Permissions.CountingCircleContactPerson.Create,
            Permissions.CountingCircleContactPerson.Update,

            Permissions.ContestCountingCircleDetails.Update,

            Permissions.Import.ReadECounting,

            Permissions.Export.ExportData,

            Permissions.EventLog.Watch,
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

            Permissions.PoliticalBusinessUnionEndResult.Read,
            Permissions.PoliticalBusinessUnionEndResult.Finalize,

            Permissions.PoliticalBusinessUnionEndResultLotDecision.Read,
            Permissions.PoliticalBusinessUnionEndResultLotDecision.Update,

            Permissions.VoteBallotResult.Read,

            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionList.Read,
            Permissions.ProportionalElectionListResult.Read,

            Permissions.ProportionalElectionEndResult.EnterManualResults,
            Permissions.ProportionalElectionEndResult.TriggerMandateDistribution,

            Permissions.MajorityElectionCandidate.Read,
            Permissions.MajorityElectionBallotGroupResult.Read,

            Permissions.Import.ImportEVoting,
            Permissions.Import.ReadEVoting,
            Permissions.Import.DeleteEVoting,

            Permissions.Import.ReadECounting,

            Permissions.Export.ExportData,
            Permissions.Export.ExportMonitoringData,
            Permissions.Export.ExportActivityProtocol,
            Permissions.Export.ExportEch0252ProportionalElectionWithCandidateListResultsInfo,

            Permissions.ExportConfiguration.Read,
            Permissions.ExportConfiguration.Update,
            Permissions.ExportConfiguration.Trigger,

            Permissions.EventLog.Watch,
        };

        _permissionsPerRole[monitoringElectionSupporter] = new HashSet<string>
        {
            Permissions.Contest.Read,

            Permissions.PoliticalBusiness.ReadOwned,

            Permissions.PoliticalBusinessResult.Read,
            Permissions.PoliticalBusinessResult.ReadComments,
            Permissions.PoliticalBusinessResult.ReadOverview,

            Permissions.PoliticalBusinessResultBundle.Read,

            Permissions.PoliticalBusinessResultBallot.Read,
            Permissions.PoliticalBusinessResultBallot.ReadAll,

            Permissions.PoliticalBusinessEndResult.Read,

            Permissions.PoliticalBusinessUnionEndResult.Read,

            Permissions.PoliticalBusinessEndResultLotDecision.Read,
            Permissions.PoliticalBusinessUnionEndResultLotDecision.Read,

            Permissions.VoteBallotResult.Read,

            Permissions.ProportionalElectionCandidate.Read,
            Permissions.ProportionalElectionList.Read,
            Permissions.ProportionalElectionListResult.Read,

            Permissions.MajorityElectionCandidate.Read,
            Permissions.MajorityElectionBallotGroupResult.Read,

            Permissions.Import.ReadECounting,
            Permissions.Import.ReadEVoting,

            Permissions.Export.ExportData,
            Permissions.Export.ExportMonitoringData,
            Permissions.Export.ExportActivityProtocol,
            Permissions.Export.ExportEch0252ProportionalElectionWithCandidateListResultsInfo,

            Permissions.ExportConfiguration.Read,
            Permissions.ExportConfiguration.Update,
            Permissions.ExportConfiguration.Trigger,

            Permissions.EventLog.Watch,
        };

        _permissionsPerRole[reportExporterApi] = new HashSet<string>
        {
            Permissions.ReportExportApi.ExportData,
            Permissions.ReportExportApi.ExportProtocol,
            Permissions.ReportExportApi.ExportEch0252,

            Permissions.Export.ExportMonitoringData,
            Permissions.Export.ExportActivityProtocol,
            Permissions.Export.ExportEch0252ProportionalElectionWithCandidateListResultsInfo,

            Permissions.PoliticalBusiness.ReadAccessible,
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
