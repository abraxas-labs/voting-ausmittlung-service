// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.ModelBuilders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }

    // The language used in this DbContext. Used to filter some translations/queries.
    public string? Language { get; set; }

    // nullables see https://docs.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types
    public DbSet<EventProcessingState> EventProcessingStates { get; set; } = null!;

    public DbSet<CountingCircle> CountingCircles { get; set; } = null!;

    public DbSet<Authority> Authorities { get; set; } = null!;

    public DbSet<CountingCircleContactPerson> CountingCircleContactPersons { get; set; } = null!;

    public DbSet<CountingCircleElectorate> CountingCircleElectorates { get; set; } = null!;

    public DbSet<DomainOfInfluence> DomainOfInfluences { get; set; } = null!;

    public DbSet<DomainOfInfluenceCountingCircle> DomainOfInfluenceCountingCircles { get; set; } = null!;

    public DbSet<DomainOfInfluencePermissionEntry> DomainOfInfluencePermissions { get; set; } = null!;

    public DbSet<Vote> Votes { get; set; } = null!;

    public DbSet<VoteTranslation> VoteTranslations { get; set; } = null!;

    public DbSet<VoteResult> VoteResults { get; set; } = null!;

    public DbSet<Ballot> Ballots { get; set; } = null!;

    public DbSet<BallotResult> BallotResults { get; set; } = null!;

    public DbSet<BallotQuestion> BallotQuestions { get; set; } = null!;

    public DbSet<BallotQuestionTranslation> BallotQuestionTranslations { get; set; } = null!;

    public DbSet<BallotQuestionResult> BallotQuestionResults { get; set; } = null!;

    public DbSet<TieBreakQuestion> TieBreakQuestions { get; set; } = null!;

    public DbSet<TieBreakQuestionTranslation> TieBreakQuestionTranslations { get; set; } = null!;

    public DbSet<TieBreakQuestionResult> TieBreakQuestionResults { get; set; } = null!;

    public DbSet<VoteEndResult> VoteEndResults { get; set; } = null!;

    public DbSet<BallotEndResult> BallotEndResults { get; set; } = null!;

    public DbSet<BallotQuestionEndResult> BallotQuestionEndResults { get; set; } = null!;

    public DbSet<TieBreakQuestionEndResult> TieBreakQuestionEndResults { get; set; } = null!;

    public DbSet<Contest> Contests { get; set; } = null!;

    public DbSet<ContestTranslation> ContestTranslations { get; set; } = null!;

    public DbSet<ContestCountingCircleElectorate> ContestCountingCircleElectorates { get; set; } = null!;

    public DbSet<ContestCountingCircleDetails> ContestCountingCircleDetails { get; set; } = null!;

    public DbSet<VotingCardResultDetail> VotingCardResultDetails { get; set; } = null!;

    public DbSet<CountOfVotersInformationSubTotal> CountOfVotersInformationSubTotals { get; set; } = null!;

    public DbSet<ContestDetails> ContestDetails { get; set; } = null!;

    public DbSet<ContestVotingCardResultDetail> ContestVotingCardResultDetails { get; set; } = null!;

    public DbSet<ContestCountOfVotersInformationSubTotal> ContestCountOfVotersInformationSubTotals { get; set; } = null!;

    public DbSet<ProportionalElection> ProportionalElections { get; set; } = null!;

    public DbSet<ProportionalElectionTranslation> ProportionalElectionTranslations { get; set; } = null!;

    public DbSet<ProportionalElectionUnion> ProportionalElectionUnions { get; set; } = null!;

    public DbSet<ProportionalElectionUnionEntry> ProportionalElectionUnionEntries { get; set; } = null!;

    public DbSet<ProportionalElectionList> ProportionalElectionLists { get; set; } = null!;

    public DbSet<ProportionalElectionListTranslation> ProportionalElectionListTranslations { get; set; } = null!;

    public DbSet<ProportionalElectionListUnion> ProportionalElectionListUnions { get; set; } = null!;

    public DbSet<ProportionalElectionListUnionTranslation> ProportionalElectionListUnionTranslations { get; set; } = null!;

    public DbSet<ProportionalElectionListUnionEntry> ProportionalElectionListUnionEntries { get; set; } = null!;

    public DbSet<ProportionalElectionCandidate> ProportionalElectionCandidates { get; set; } = null!;

    public DbSet<ProportionalElectionCandidateTranslation> ProportionalElectionCandidateTranslations { get; set; } = null!;

    public DbSet<ProportionalElectionResult> ProportionalElectionResults { get; set; } = null!;

    public DbSet<ProportionalElectionEndResult> ProportionalElectionEndResult { get; set; } = null!;

    public DbSet<ProportionalElectionUnionEndResult> ProportionalElectionUnionEndResults { get; set; } = null!;

    public DbSet<DoubleProportionalResult> DoubleProportionalResults { get; set; } = null!;

    public DbSet<DoubleProportionalResultRow> DoubleProportionalResultRows { get; set; } = null!;

    public DbSet<DoubleProportionalResultCell> DoubleProportionalResultCells { get; set; } = null!;

    public DbSet<DoubleProportionalResultColumn> DoubleProportionalResultColumns { get; set; } = null!;

    public DbSet<ProportionalElectionListEndResult> ProportionalElectionListEndResult { get; set; } = null!;

    public DbSet<ProportionalElectionCandidateEndResult> ProportionalElectionCandidateEndResult { get; set; } = null!;

    public DbSet<ProportionalElectionUnmodifiedListResult> ProportionalElectionUnmodifiedListResults { get; set; } = null!;

    public DbSet<ProportionalElectionListResult> ProportionalElectionListResults { get; set; } = null!;

    public DbSet<ProportionalElectionCandidateResult> ProportionalElectionCandidateResults { get; set; } = null!;

    // Due to a an ef core limitation
    // the name ProportionalElectionResultBundles does not work.
    // Since FK names are shortened for long names
    // this would lead to the same name for a key as for the table ProportionalElectionResults
    // which is not allowed by ef core.
    public DbSet<ProportionalElectionResultBundle> ProportionalElectionBundles { get; set; } = null!;

    public DbSet<ProportionalElectionResultBallot> ProportionalElectionResultBallots { get; set; } = null!;

    public DbSet<ProportionalElectionResultBallotCandidate> ProportionalElectionResultBallotCandidates { get; set; } = null!;

    public DbSet<ProportionalElectionUnionList> ProportionalElectionUnionLists { get; set; } = null!;

    public DbSet<ProportionalElectionUnionListTranslation> ProportionalElectionUnionListTranslations { get; set; } = null!;

    public DbSet<ProportionalElectionUnionListEntry> ProportionalElectionUnionListEntries { get; set; } = null!;

    public DbSet<MajorityElection> MajorityElections { get; set; } = null!;

    public DbSet<MajorityElectionTranslation> MajorityElectionTranslations { get; set; } = null!;

    public DbSet<MajorityElectionUnion> MajorityElectionUnions { get; set; } = null!;

    public DbSet<MajorityElectionUnionEntry> MajorityElectionUnionEntries { get; set; } = null!;

    public DbSet<MajorityElectionCandidate> MajorityElectionCandidates { get; set; } = null!;

    public DbSet<MajorityElectionCandidateTranslation> MajorityElectionCandidateTranslations { get; set; } = null!;

    public DbSet<SecondaryMajorityElection> SecondaryMajorityElections { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionTranslation> SecondaryMajorityElectionTranslations { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionCandidate> SecondaryMajorityElectionCandidates { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionCandidateTranslation> SecondaryMajorityElectionCandidateTranslations { get; set; } = null!;

    public DbSet<MajorityElectionBallotGroup> MajorityElectionBallotGroups { get; set; } = null!;

    public DbSet<MajorityElectionBallotGroupEntry> MajorityElectionBallotGroupEntries { get; set; } = null!;

    public DbSet<MajorityElectionBallotGroupEntryCandidate> MajorityElectionBallotGroupEntryCandidates { get; set; } = null!;

    public DbSet<MajorityElectionResult> MajorityElectionResults { get; set; } = null!;

    public DbSet<MajorityElectionWriteInMapping> MajorityElectionWriteInMappings { get; set; } = null!;

    public DbSet<MajorityElectionWriteInBallot> MajorityElectionWriteInBallots { get; set; } = null!;

    public DbSet<MajorityElectionWriteInBallotPosition> MajorityElectionWriteInBallotPositions { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionResult> SecondaryMajorityElectionResults { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionWriteInMapping> SecondaryMajorityElectionWriteInMappings { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionWriteInBallot> SecondaryMajorityElectionWriteInBallots { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionWriteInBallotPosition> SecondaryMajorityElectionWriteInBallotPositions { get; set; } = null!;

    public DbSet<MajorityElectionBallotGroupResult> MajorityElectionBallotGroupResults { get; set; } = null!;

    public DbSet<MajorityElectionCandidateResult> MajorityElectionCandidateResults { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionCandidateResult> SecondaryMajorityElectionCandidateResults { get; set; } = null!;

    public DbSet<MajorityElectionResultBundle> MajorityElectionResultBundles { get; set; } = null!;

    public DbSet<MajorityElectionResultBallot> MajorityElectionResultBallots { get; set; } = null!;

    public DbSet<MajorityElectionResultBallotCandidate> MajorityElectionResultBallotCandidates { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionResultBallot> SecondaryMajorityElectionResultBallots { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionResultBallotCandidate> SecondaryMajorityElectionResultBallotCandidates { get; set; } = null!;

    public DbSet<MajorityElectionEndResult> MajorityElectionEndResults { get; set; } = null!;

    public DbSet<MajorityElectionCandidateEndResult> MajorityElectionCandidateEndResults { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionEndResult> SecondaryMajorityElectionEndResults { get; set; } = null!;

    public DbSet<SecondaryMajorityElectionCandidateEndResult> SecondaryMajorityElectionCandidateEndResults { get; set; } = null!;

    public DbSet<ElectionGroup> ElectionGroups { get; set; } = null!;

    public DbSet<SimplePoliticalBusiness> SimplePoliticalBusinesses { get; set; } = null!;

    public DbSet<SimplePoliticalBusinessTranslation> SimplePoliticalBusinessTranslations { get; set; } = null!;

    public DbSet<SimpleCountingCircleResult> SimpleCountingCircleResults { get; set; } = null!;

    public DbSet<CountingCircleResultComment> CountingCircleResultComments { get; set; } = null!;

    public DbSet<ResultImport> ResultImports { get; set; } = null!;

    public DbSet<IgnoredImportCountingCircle> IgnoredImportCountingCircles { get; set; } = null!;

    public DbSet<CantonSettings> CantonSettings { get; set; } = null!;

    public DbSet<CantonSettingsVotingCardChannel> CantonSettingsVotingCardChannels { get; set; } = null!;

    public DbSet<CountingCircleResultStateDescription> CountingCircleResultStateDescriptions { get; set; } = null!;

    public DbSet<VoteResultBundle> VoteResultBundles { get; set; } = null!;

    public DbSet<VoteResultBallot> VoteResultBallots { get; set; } = null!;

    public DbSet<VoteResultBallotQuestionAnswer> VoteResultBallotQuestionAnswers { get; set; } = null!;

    public DbSet<VoteResultBallotTieBreakQuestionAnswer> VoteResultBallotTieBreakQuestionAnswers { get; set; } = null!;

    public DbSet<ExportConfiguration> ExportConfigurations { get; set; } = null!;

    public DbSet<ResultExportConfiguration> ResultExportConfigurations { get; set; } = null!;

    public DbSet<ResultExportConfigurationPoliticalBusiness> ResultExportConfigurationPoliticalBusinesses { get; set; } = null!;

    public DbSet<ResultExportConfigurationPoliticalBusinessMetadata> ResultExportConfigurationPoliticalBusinessMetadata { get; set; } = null!;

    public DbSet<PlausibilisationConfiguration> PlausibilisationConfigurations { get; set; } = null!;

    public DbSet<ComparisonVoterParticipationConfiguration> ComparisonVoterParticipationConfigurations { get; set; } = null!;

    public DbSet<ComparisonCountOfVotersConfiguration> ComparisonCountOfVotersConfigurations { get; set; } = null!;

    public DbSet<ComparisonVotingChannelConfiguration> ComparisonVotingChannelConfigurations { get; set; } = null!;

    public DbSet<DomainOfInfluenceCantonDefaultsVotingCardChannel> DomainOfInfluenceCantonDefaultsVotingCardChannels { get; set; } = null!;

    public DbSet<ContestCantonDefaultsCountingCircleResultStateDescription> ContestCantonDefaultsCountingCircleResultStateDescriptions { get; set; } = null!;

    public DbSet<ContestCantonDefaults> ContestCantonDefaults { get; set; } = null!;

    public DbSet<DomainOfInfluenceParty> DomainOfInfluenceParties { get; set; } = null!;

    public DbSet<DomainOfInfluencePartyTranslation> DomainOfInfluencePartyTranslations { get; set; } = null!;

    public DbSet<ContestDomainOfInfluenceDetails> ContestDomainOfInfluenceDetails { get; set; } = null!;

    public DbSet<DomainOfInfluenceVotingCardResultDetail> DomainOfInfluenceVotingCardResultDetails { get; set; } = null!;

    public DbSet<DomainOfInfluenceCountOfVotersInformationSubTotal> DomainOfInfluenceCountOfVotersInformationSubTotals { get; set; } = null!;

    public DbSet<ProtocolExport> ProtocolExports { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Workaround to access the DbContext instance in the model builder classes
        DbContextAccessor.DbContext = this;
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}
