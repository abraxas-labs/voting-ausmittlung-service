// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfContactPerson
{
    public string FirstName { get; set; } = string.Empty;

    public string FamilyName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string MobilePhone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
