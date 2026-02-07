// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Auth;

public static class RolesMockedData
{
    public const string ErfassungCreator = "STA-VOTING-AUSMITTLUNG-ERFASSUNG::Erfasser";
    public const string ErfassungCreatorWithoutBundleControl = "STA-VOTING-AUSMITTLUNG-ERFASSUNG::Erfasser ohne Bundkontrolle";
    public const string ErfassungBundleController = "STA-VOTING-AUSMITTLUNG-ERFASSUNG::Bundkontrolleur";
    public const string ErfassungRestrictedBundleController = "STA-VOTING-AUSMITTLUNG-ERFASSUNG::Bundkontrolleur ohne Abschluss";
    public const string ErfassungElectionAdmin = "STA-VOTING-AUSMITTLUNG-ERFASSUNG::Wahlverwalter";
    public const string ErfassungElectionSupporter = "STA-VOTING-AUSMITTLUNG-ERFASSUNG::Wahlunterstützer";
    public const string MonitoringElectionAdmin = "STA-VOTING-AUSMITTLUNG-MONITORING::Wahlverwalter";
    public const string MonitoringElectionSupporter = "STA-VOTING-AUSMITTLUNG-MONITORING::Wahlunterstützer";
    public const string ReportExporterApi = "STA-VOTING-AUSMITTLUNG-ERFASSUNG::ReportExporterApi";

    public static IEnumerable<string> All()
    {
        yield return ErfassungCreator;
        yield return ErfassungCreatorWithoutBundleControl;
        yield return ErfassungBundleController;
        yield return ErfassungRestrictedBundleController;
        yield return ErfassungElectionAdmin;
        yield return ErfassungElectionSupporter;
        yield return MonitoringElectionAdmin;
        yield return MonitoringElectionSupporter;
        yield return ReportExporterApi;
    }
}
