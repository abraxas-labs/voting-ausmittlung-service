// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Models.Import;

public class WriteInMapping
{
    public WriteInMapping(string writeInName, int countOfVotes)
    {
        WriteInName = writeInName;
        CountOfVotes = countOfVotes;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public string WriteInName { get; }

    public int CountOfVotes { get; internal set; }
}
