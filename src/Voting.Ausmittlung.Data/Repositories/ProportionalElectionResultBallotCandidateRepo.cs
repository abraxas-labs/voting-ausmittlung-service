// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionResultBallotCandidateRepo : DbRepository<DataContext, ProportionalElectionResultBallotCandidate>
{
    private static string? _delimitedTableName;
    private static string? _idColumnName;
    private static string? _ballotIdColumnName;
    private static string? _positionColumnName;
    private static string? _onListColumnName;
    private static string? _removedFromListColumnName;
    private static string? _candidateIdColumnName;

    public ProportionalElectionResultBallotCandidateRepo(DataContext context)
        : base(context)
    {
    }

    public string DelimitedTableName => _delimitedTableName ??= DelimitedSchemaAndTableName;

    public string IdColumnName => _idColumnName ??= GetDelimitedColumnName(x => x.Id);

    public string BallotIdColumnName => _ballotIdColumnName ??= GetDelimitedColumnName(x => x.BallotId);

    public string PositionColumnName => _positionColumnName ??= GetDelimitedColumnName(x => x.Position);

    public string OnListVoteCountColumnName => _onListColumnName ??= GetDelimitedColumnName(x => x.OnList);

    public string RemovedFromListColumnName => _removedFromListColumnName ??= GetDelimitedColumnName(x => x.RemovedFromList);

    public string CandidateIdColumnName => _candidateIdColumnName ??= GetDelimitedColumnName(x => x.CandidateId);
}
