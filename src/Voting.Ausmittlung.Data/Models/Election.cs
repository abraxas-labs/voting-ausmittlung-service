// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public abstract class Election : PoliticalBusiness
{
    public int NumberOfMandates { get; set; }
}
