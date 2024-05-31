// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Queries;

public static class ProportionalElectionQueryExtensions
{
    public static IQueryable<ProportionalElection> WhereIsDoubleProportional(this IQueryable<ProportionalElection> query) =>
        query.Where(pe => pe.MandateAlgorithm == ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum
            || pe.MandateAlgorithm == ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum
            || pe.MandateAlgorithm == ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum);
}
