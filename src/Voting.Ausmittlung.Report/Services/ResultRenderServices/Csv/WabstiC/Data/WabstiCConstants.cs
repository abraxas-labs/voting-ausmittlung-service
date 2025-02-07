// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;

internal static class WabstiCConstants
{
    internal const int MaxSecondaryMajorityElectionsSupported = 2;

    internal const string IndividualMajorityCandidateNumber = "999";

    internal const string IndividualMajorityCandidateLastName = "Vereinzelte";

    internal const string EmptyMajorityCandidateNumber = "1000";

    internal const string EmptyMajorityCandidateLastName = "Leere Zeilen";

    internal const string InvalidMajorityCandidateNumber = "1001";

    internal const string InvalidMajorityCandidateLastName = "Ungültige Stimmen";

    internal const int IndividualMajorityCandidateYearOfBirth = 1950;

    internal const string IncumbentText = "(bisher)";

    internal const string ListWithoutPartyOrderNumber = "99";

    internal const string ListWithoutPartyShortDescription = "WoP";

    internal const string ListWithoutPartyNumberAndDescription = ListWithoutPartyOrderNumber + "." + ListWithoutPartyShortDescription;

    internal const int CandidateDefaultBirthYear = 1900;
}
