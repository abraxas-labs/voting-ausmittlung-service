// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionResultBallotRepo : DbRepository<DataContext, ProportionalElectionResultBallot>
{
    private static string? _delimitedTableName;
    private static string? _idColumnName;
    private static string? _bundleIdColumnName;
    private static string? _numberColumnName;
    private static string? _emptyVoteCountColumnName;
    private static string? _markedForReviewColumnName;
    private static string? _indexColumnName;

    public ProportionalElectionResultBallotRepo(DataContext context)
        : base(context)
    {
    }

    public string DelimitedTableName => _delimitedTableName ??= DelimitedSchemaAndTableName;

    public string IdColumnName => _idColumnName ??= GetDelimitedColumnName(x => x.Id);

    public string BundleIdColumnName => _bundleIdColumnName ??= GetDelimitedColumnName(x => x.BundleId);

    public string NumberColumnName => _numberColumnName ??= GetDelimitedColumnName(x => x.Number);

    public string EmptyVoteCountColumnName => _emptyVoteCountColumnName ??= GetDelimitedColumnName(x => x.EmptyVoteCount);

    public string MarkedForReviewColumnName => _markedForReviewColumnName ??= GetDelimitedColumnName(x => x.MarkedForReview);

    public string IndexColumnName => _indexColumnName ??= GetDelimitedColumnName(x => x.Index);
}
