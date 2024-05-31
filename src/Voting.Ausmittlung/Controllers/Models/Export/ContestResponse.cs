// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class ContestResponse
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime EndOfTestingPhase { get; set; }

    public bool TestingPhaseEnded { get; set; }

    public Guid DomainOfInfluenceId { get; set; }

    public bool EVoting { get; set; }

    public DateTime? EVotingFrom { get; set; }

    public DateTime? EVotingTo { get; set; }

    public ContestState State { get; set; }

    public bool EVotingResultsImported { get; set; }
}
