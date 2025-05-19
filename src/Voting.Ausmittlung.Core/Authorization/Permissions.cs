// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Authorization;

public static class Permissions
{
    private const string CreateSuffix = ":create";
    private const string UpdateSuffix = ":update";
    private const string ReadSuffix = ":read";
    private const string DeleteSuffix = ":delete";

    // Used when the "normal" permission (ex. 'read') allows access only to specific resources, while the  '-all' allows access to all resources
    private const string ReadAllSuffix = ReadSuffix + "-all";
    private const string UpdateAllSuffix = UpdateSuffix + "-all";

    public static class Contest
    {
        public const string Read = Prefix + ReadSuffix;

        private const string Prefix = "Contest";
    }

    public static class PoliticalBusiness
    {
        // Allowed to read political businesses owned by the tenant
        public const string ReadOwned = Prefix + ":read-owned";

        // Allowed to read political businesses accessible to the user
        public const string ReadAccessible = Prefix + ":read-accessible";

        private const string Prefix = "PoliticalBusiness";
    }

    public static class PoliticalBusinessResult
    {
        public const string Read = Prefix + ReadSuffix;
        public const string ReadComments = Prefix + ":read-comments";
        public const string ReadOverview = Prefix + ":read-overview";
        public const string EnterResults = Prefix + ":enter-results";
        public const string ResetResults = Prefix + ":reset-results";
        public const string StartSubmission = Prefix + ":start-submission";
        public const string FinishSubmission = Prefix + ":finish-submission";
        public const string Audit = Prefix + ":audit";
        public const string FinishSubmissionAndAudit = Prefix + ":finish-submission-and-audit";

        private const string Prefix = "PoliticalBusinessResult";
    }

    public static class PoliticalBusinessResultBundle
    {
        public const string Read = Prefix + ReadSuffix;
        public const string Create = Prefix + CreateSuffix;
        public const string UpdateAll = Prefix + UpdateAllSuffix;
        public const string Delete = Prefix + DeleteSuffix;
        public const string FinishSubmission = Prefix + ":finish-submission";
        public const string Review = Prefix + ":review";

        private const string Prefix = "PoliticalBusinessResultBundle";
    }

    public static class PoliticalBusinessResultBallot
    {
        public const string Read = Prefix + ReadSuffix;
        public const string ReadAll = Prefix + ReadAllSuffix;
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Delete = Prefix + DeleteSuffix;

        private const string Prefix = "PoliticalBusinessResultBallot";
    }

    public static class VoteBallotResult
    {
        public const string Read = Prefix + ReadSuffix;

        private const string Prefix = "VoteBallotResult";
    }

    public static class MajorityElectionBallotGroupResult
    {
        public const string Read = Prefix + ReadSuffix;

        private const string Prefix = "MajorityElectionBallotGroupResult";
    }

    public static class MajorityElectionCandidate
    {
        public const string Read = Prefix + ReadSuffix;

        private const string Prefix = "MajorityElectionCandidate";
    }

    public static class MajorityElectionWriteIn
    {
        public const string Read = Prefix + ReadSuffix;
        public const string Update = Prefix + UpdateSuffix;

        private const string Prefix = "MajorityElectionWriteIn";
    }

    public static class ProportionalElectionCandidate
    {
        public const string Read = Prefix + ReadSuffix;

        private const string Prefix = "ProportionalElectionCandidate";
    }

    public static class ProportionalElectionList
    {
        public const string Read = Prefix + ReadSuffix;

        private const string Prefix = "ProportionalElectionList";
    }

    public static class ProportionalElectionListResult
    {
        public const string Read = Prefix + ReadSuffix;

        private const string Prefix = "ProportionalElectionListResult";
    }

    public static class ProportionalElectionEndResult
    {
        public const string EnterManualResults = Prefix + ":enter-manual-results";
        public const string TriggerMandateDistribution = Prefix + ":trigger-mandate-distribution";

        private const string Prefix = "ProportionalElectionEndResult";
    }

    public static class PoliticalBusinessUnionEndResult
    {
        public const string Read = Prefix + ReadSuffix;
        public const string Finalize = Prefix + ":finalize";

        private const string Prefix = "PoliticalBusinessUnionEndResult";
    }

    public static class PoliticalBusinessUnionEndResultLotDecision
    {
        public const string Read = Prefix + ReadSuffix;
        public const string Update = Prefix + UpdateSuffix;

        private const string Prefix = "PoliticalBusinessUnionEndResultLotDecision";
    }

    public static class PoliticalBusinessEndResult
    {
        public const string Read = Prefix + ReadSuffix;
        public const string Finalize = Prefix + ":finalize";

        private const string Prefix = "PoliticalBusinessEndResult";
    }

    public static class PoliticalBusinessEndResultLotDecision
    {
        public const string Read = Prefix + ReadSuffix;
        public const string Update = Prefix + UpdateSuffix;

        private const string Prefix = "PoliticalBusinessEndResultLotDecision";
    }

    public static class CountingCircleContactPerson
    {
        public const string Create = Prefix + CreateSuffix;
        public const string Update = Prefix + UpdateSuffix;

        private const string Prefix = "CountingCircleContactPerson";
    }

    public static class ContestCountingCircleDetails
    {
        public const string Update = Prefix + UpdateSuffix;

        private const string Prefix = "ContestCountingCircleDetails";
    }

    public static class ContestCountingCircleElectorate
    {
        public const string Update = Prefix + UpdateSuffix;

        private const string Prefix = "ContestCountingCircleElectorate";
    }

    public static class Import
    {
        public const string ImportECounting = Prefix + ":import-ecounting";
        public const string ReadECounting = Prefix + ":read-ecounting";
        public const string DeleteECounting = Prefix + ":delete-ecounting";

        public const string ImportEVoting = Prefix + ":import-evoting";
        public const string ReadEVoting = Prefix + ":read-evoting";
        public const string DeleteEVoting = Prefix + ":delete-evoting";

        private const string Prefix = "Import";
    }

    public static class Export
    {
        public const string ExportData = Prefix + ":export";
        public const string ExportMonitoringData = Prefix + ":export-monitoring";
        public const string ExportActivityProtocol = Prefix + ":export-activity-protocol";

        private const string Prefix = "Export";
    }

    public static class ReportExportApi
    {
        public const string ExportData = Prefix + ":data";
        public const string ExportProtocol = Prefix + ":protocol";
        public const string ExportEch0252 = Prefix + ":ech-0252";

        private const string Prefix = "ReportExportApi";
    }

    public static class ExportConfiguration
    {
        public const string Read = Prefix + ReadSuffix;
        public const string Update = Prefix + UpdateSuffix;
        public const string Trigger = Prefix + ":trigger";

        private const string Prefix = "ExportConfiguration";
    }

    public static class EventLog
    {
        public const string Watch = Prefix + ":watch";

        private const string Prefix = "EventLog";
    }
}
