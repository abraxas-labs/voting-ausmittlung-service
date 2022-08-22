// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Models.Import;

public class MissingWriteInMapping
{
    public MissingWriteInMapping(
        string writeInName,
        int countOfVotes)
    {
        WriteInName = writeInName;
        CountOfVotes = countOfVotes;
    }

    public string WriteInName { get; }

    public int CountOfVotes { get; }
}
