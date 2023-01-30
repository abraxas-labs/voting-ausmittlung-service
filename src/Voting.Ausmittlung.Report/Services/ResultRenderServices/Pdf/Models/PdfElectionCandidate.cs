// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfElectionCandidate
{
    public string Description { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PoliticalFirstName { get; set; } = string.Empty;

    public string PoliticalLastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public SexType Sex { get; set; }

    public string Title { get; set; } = string.Empty;

    public string OccupationTitle { get; set; } = string.Empty;

    public string Occupation { get; set; } = string.Empty;

    public bool Incumbent { get; set; }

    public string ZipCode { get; set; } = string.Empty;

    public string Locality { get; set; } = string.Empty;

    public int Position { get; set; }

    public string Origin { get; set; } = string.Empty;
}
