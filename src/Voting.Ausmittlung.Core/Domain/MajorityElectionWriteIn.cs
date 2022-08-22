// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

/// <summary>
/// Entry for a "write in" (candidate manually written on the ballot by the voter, in german: manuell erfasster Kandidat).
/// </summary>
public class MajorityElectionWriteIn
{
    public Guid WriteInId { get; set; }

    public MajorityElectionWriteInMappingTarget Target { get; set; }

    public Guid? CandidateId { get; set; }
}
