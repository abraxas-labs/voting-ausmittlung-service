// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;

internal static class WabstiCConstants
{
    internal const int MaxSecondaryMajorityElectionsSupported = 2;

    internal const string IndividualMajorityCandidateNumber = "999";

    internal const string IndividualMajorityCandidateLastName = "Vereinzelte";

    internal const int IndividualMajorityCandidateYearOfBirth = 1950;

    internal const string IncumbentText = "(bisher)";

    internal const string ListWithoutPartyOrderNumber = "99";

    internal const string ListWithoutPartyShortDescription = "WoP";

    internal const string ListWithoutPartyNumberAndDescription = ListWithoutPartyOrderNumber + "." + ListWithoutPartyShortDescription;
}
