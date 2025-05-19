// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Common;
using MajorityElectionWriteInMappingTarget = Abraxas.Voting.Ausmittlung.Shared.V1.MajorityElectionWriteInMappingTarget;
using ResultImportType = Voting.Ausmittlung.Data.Models.ResultImportType;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ResultImportAggregate : BaseEventSignatureAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    private readonly ISet<Guid> _importedMajorityElectionIds = new HashSet<Guid>();
    private readonly ISet<Guid> _importedSecondaryMajorityElectionIds = new HashSet<Guid>();
    private readonly ISet<Guid> _importedProportionalElectionIds = new HashSet<Guid>();
    private readonly ISet<Guid> _importedVoteIds = new HashSet<Guid>();
    private readonly ISet<Guid> _importedCountingCircleIds = new HashSet<Guid>();
    private readonly ISet<Guid> _importedCountingCircleIdsWithWriteIns = new HashSet<Guid>();
    private readonly Dictionary<(Guid ElectionId, Guid BasisCountingCircleId), ISet<Guid>> _availableWriteInIdsByElectionId = new();

    private bool _hasSuccessor;

    public ResultImportAggregate(EventInfoProvider eventInfoProvider, IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _mapper = mapper;
    }

    public override string AggregateName => "voting-resultImport";

    public ResultImportType ImportType { get; set; }

    public string FileName { get; private set; } = string.Empty;

    public Guid ContestId { get; private set; }

    public Guid? CountingCircleId { get; private set; }

    public DateTime? Started { get; private set; }

    public DateTime? Deleted { get; private set; }

    public bool Completed { get; private set; }

    public IEnumerable<Guid> ImportedVoteIds => _importedVoteIds;

    public IEnumerable<Guid> ImportedProportionalElectionIds => _importedProportionalElectionIds;

    public IEnumerable<Guid> ImportedMajorityElectionIds => _importedMajorityElectionIds;

    internal void Start(
        string fileName,
        ResultImportType importType,
        Guid contestId,
        Guid? countingCircleId,
        string echMessageId,
        IEnumerable<IgnoredImportCountingCircle> ignoredImportCountingCircles)
    {
        EnsureNotStarted();
        EnsureHasNoSuccessor();

        Id = Guid.NewGuid();
        RaiseEvent(
            new ResultImportStarted
            {
                ContestId = contestId.ToString(),
                CountingCircleId = countingCircleId?.ToString() ?? string.Empty,
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)importType,
                FileName = fileName,
                ImportId = Id.ToString(),
                EchMessageId = echMessageId,
                IgnoredCountingCircles = { _mapper.Map<IEnumerable<ImportIgnoredCountingCircleEventData>>(ignoredImportCountingCircles) },
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    internal void ImportCountingCircleVotingCards(List<VotingImportCountingCircleVotingCards> countingCircleVotingCards)
    {
        EnsureInProgress();
        EnsureHasNoSuccessor();
        EnsureImportType(ResultImportType.EVoting);

        if (CountingCircleId.HasValue &&
            countingCircleVotingCards.Any(c => Guid.Parse(c.BasisCountingCircleId) != CountingCircleId.Value))
        {
            throw new ValidationException("Counting circle voting cards of another counting circle are not allowed.");
        }

        var ev = new CountingCircleVotingCardsImported
        {
            ContestId = ContestId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
            ImportId = Id.ToString(),
            CountingCircleVotingCards =
            {
                countingCircleVotingCards.Select(x => new CountingCircleVotingCardsImportEventData
                {
                    CountingCircleId = x.BasisCountingCircleId,
                    CountOfReceivedVotingCards = x.CountOfVotingCards,
                }),
            },
        };

        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));
    }

    internal void ImportProportionalElectionResult(ProportionalElectionResultImport data)
    {
        EnsureInProgress();
        EnsureHasNoSuccessor();
        EnsureCountingCircleIdMatch(data.BasisCountingCircleId);

        var ev = new ProportionalElectionResultImported
        {
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId?.ToString() ?? string.Empty,
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ImportId = Id.ToString(),
            ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
        };

        _mapper.Map(data, ev);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));
    }

    internal void ImportMajorityElectionResult(MajorityElectionResultImport data)
    {
        EnsureInProgress();
        EnsureHasNoSuccessor();
        EnsureCountingCircleIdMatch(data.BasisCountingCircleId);

        var ev = new MajorityElectionResultImported
        {
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId?.ToString() ?? string.Empty,
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ImportId = Id.ToString(),
            ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
        };

        _mapper.Map(data, ev);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));

        foreach (var writeInBallot in data.WriteInBallots)
        {
            if (CountingCircleId.HasValue && data.BasisCountingCircleId != CountingCircleId.Value)
            {
                throw new ValidationException("Cannot import write ins of another counting circle.");
            }

            var writeInEv = new MajorityElectionWriteInBallotImported
            {
                ContestId = ContestId.ToString(),
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ImportId = Id.ToString(),
                CountingCircleId = data.BasisCountingCircleId.ToString(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
                MajorityElectionId = data.PoliticalBusinessId.ToString(),
                EmptyVoteCount = writeInBallot.EmptyVoteCount,
                InvalidVoteCount = writeInBallot.InvalidVoteCount,
                CandidateIds = { writeInBallot.CandidateIds.Select(id => id.ToString()) },
                WriteInMappingIds = { writeInBallot.WriteInMappingIds.Select(id => id.ToString()) },
            };
            RaiseEvent(writeInEv, new EventSignatureBusinessDomainData(ContestId));
        }
    }

    internal void ImportSecondaryMajorityElectionResult(MajorityElectionResultImport data)
    {
        EnsureInProgress();
        EnsureHasNoSuccessor();
        EnsureCountingCircleIdMatch(data.BasisCountingCircleId);

        var ev = new SecondaryMajorityElectionResultImported
        {
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId?.ToString() ?? string.Empty,
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ImportId = Id.ToString(),
            ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
        };

        _mapper.Map(data, ev);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));

        foreach (var writeInBallot in data.WriteInBallots)
        {
            if (CountingCircleId.HasValue && data.BasisCountingCircleId != CountingCircleId.Value)
            {
                throw new ValidationException("Cannot import write ins of another counting circle.");
            }

            var writeInEv = new SecondaryMajorityElectionWriteInBallotImported
            {
                ContestId = ContestId.ToString(),
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ImportId = Id.ToString(),
                CountingCircleId = data.BasisCountingCircleId.ToString(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
                SecondaryMajorityElectionId = data.PoliticalBusinessId.ToString(),
                EmptyVoteCount = writeInBallot.EmptyVoteCount,
                InvalidVoteCount = writeInBallot.InvalidVoteCount,
                CandidateIds = { writeInBallot.CandidateIds.Select(id => id.ToString()) },
                WriteInMappingIds = { writeInBallot.WriteInMappingIds.Select(id => id.ToString()) },
            };
            RaiseEvent(writeInEv, new EventSignatureBusinessDomainData(ContestId));
        }
    }

    internal void ImportVoteResult(VoteResultImport data)
    {
        EnsureInProgress();
        EnsureHasNoSuccessor();
        EnsureCountingCircleIdMatch(data.BasisCountingCircleId);

        var ev = new VoteResultImported
        {
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId?.ToString() ?? string.Empty,
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ImportId = Id.ToString(),
            ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
        };

        _mapper.Map(data, ev);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));
    }

    internal void MapMajorityElectionWriteIns(
        Guid electionId,
        Guid basisCountingCircleId,
        PoliticalBusinessType politicalBusinessType,
        IEnumerable<MajorityElectionWriteIn> mappings)
    {
        EnsureCompleted();
        EnsureHasNoSuccessor();
        EnsureCountingCircleIdMatch(basisCountingCircleId);

        var writeIns = mappings.Select(m => new MajorityElectionWriteInMappedEventData
        {
            Target = _mapper.Map<MajorityElectionWriteInMappingTarget>(m.Target),
            CandidateId = m.CandidateId?.ToString() ?? string.Empty,
            WriteInMappingId = m.WriteInId.ToString(),
        }).ToList();

        if (writeIns.Any(wi => wi.Target == MajorityElectionWriteInMappingTarget.Unspecified))
        {
            throw new ValidationException("Unspecified write in mapping target is not allowed");
        }

        ValidateAllWriteInsAvailable(electionId, basisCountingCircleId, writeIns);

        IMessage ev = politicalBusinessType switch
        {
            PoliticalBusinessType.MajorityElection => new MajorityElectionWriteInsMapped
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                CountingCircleId = basisCountingCircleId.ToString(),
                MajorityElectionId = electionId.ToString(),
                ImportId = Id.ToString(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
                WriteInMappings =
                    {
                        writeIns,
                    },
            },
            PoliticalBusinessType.SecondaryMajorityElection => new SecondaryMajorityElectionWriteInsMapped
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                CountingCircleId = basisCountingCircleId.ToString(),
                SecondaryMajorityElectionId = electionId.ToString(),
                ImportId = Id.ToString(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
                WriteInMappings =
                    {
                        writeIns,
                    },
            },
            _ => throw new InvalidOperationException(nameof(politicalBusinessType) + " does not support write ins"),
        };

        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));
    }

    internal void ResetMajorityElectionWriteIns(
        Guid electionId,
        Guid basisCountingCircleId,
        PoliticalBusinessType politicalBusinessType)
    {
        EnsureCompleted();
        EnsureHasNoSuccessor();
        EnsureCountingCircleIdMatch(basisCountingCircleId);

        IMessage ev = politicalBusinessType switch
        {
            PoliticalBusinessType.MajorityElection => new MajorityElectionWriteInsReset
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                CountingCircleId = basisCountingCircleId.ToString(),
                MajorityElectionId = electionId.ToString(),
                ImportId = Id.ToString(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
            },
            PoliticalBusinessType.SecondaryMajorityElection => new SecondaryMajorityElectionWriteInsReset
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                CountingCircleId = basisCountingCircleId.ToString(),
                SecondaryMajorityElectionId = electionId.ToString(),
                ImportId = Id.ToString(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
            },
            _ => throw new InvalidOperationException(nameof(politicalBusinessType) + " does not support write ins"),
        };

        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));
    }

    /// <summary>
    /// Deletes all imported data of a contest.
    /// </summary>
    /// <param name="contestId">The id of the contest.</param>
    /// <param name="countingCircleId">The basis id of the counting circle.</param>
    /// <param name="importType">The import type.</param>
    internal void DeleteData(Guid contestId, Guid? countingCircleId, ResultImportType importType)
    {
        EnsureNotStarted();
        EnsureHasNoSuccessor();

        RaiseEvent(
            new ResultImportDataDeleted
            {
                ImportId = Guid.NewGuid().ToString(),
                ContestId = contestId.ToString(),
                CountingCircleId = countingCircleId?.ToString() ?? string.Empty,
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)importType,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    internal void Complete()
    {
        EnsureInProgress();
        EnsureHasNoSuccessor();

        RaiseEvent(
            new ResultImportCompleted
            {
                ContestId = ContestId.ToString(),
                CountingCircleId = CountingCircleId?.ToString() ?? string.Empty,
                EventInfo = _eventInfoProvider.NewEventInfo(),
                FileName = FileName,
                ImportId = Id.ToString(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
                ImportedMajorityElectionIds =
                {
                        _importedMajorityElectionIds.Select(x => x.ToString()),
                },
                ImportedSecondaryMajorityElectionIds =
                {
                        _importedSecondaryMajorityElectionIds.Select(x => x.ToString()),
                },
                ImportedProportionalElectionIds =
                {
                        _importedProportionalElectionIds.Select(x => x.ToString()),
                },
                ImportedVoteIds =
                {
                        _importedVoteIds.Select(x => x.ToString()),
                },
            },
            new EventSignatureBusinessDomainData(ContestId));

        foreach (var ccId in _importedCountingCircleIds)
        {
            RaiseEvent(
                new ResultImportCountingCircleCompleted
                {
                    ContestId = ContestId.ToString(),
                    CountingCircleId = ccId.ToString(),
                    EventInfo = _eventInfoProvider.NewEventInfo(),
                    ImportId = Id.ToString(),
                    ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
                    HasWriteIns = _importedCountingCircleIdsWithWriteIns.Contains(ccId),
                },
                new EventSignatureBusinessDomainData(ContestId));
        }
    }

    internal void SucceedBy(Guid successorImportId, bool allowDeleted)
    {
        EnsureHasNoSuccessor();
        if (!allowDeleted && Deleted.HasValue)
        {
            throw new ValidationException("Cannot delete since no results are currently imported.");
        }

        RaiseEvent(
            new ResultImportSucceeded
            {
                ContestId = ContestId.ToString(),
                CountingCircleId = CountingCircleId?.ToString() ?? string.Empty,
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ImportId = Id.ToString(),
                SuccessorImportId = successorImportId.ToString(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)ImportType,
            },
            new EventSignatureBusinessDomainData(ContestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ResultImportStarted ev:
                Id = Guid.Parse(ev.ImportId);
                FileName = ev.FileName;
                ImportType = (ResultImportType)ev.ImportType;

                ContestId = Guid.Parse(ev.ContestId);
                CountingCircleId = GuidParser.ParseNullable(ev.CountingCircleId);
                Started = ev.EventInfo.Timestamp.ToDateTime();
                break;
            case ResultImportCompleted:
                Completed = true;
                break;
            case MajorityElectionResultImported ev:
                Apply(ev);
                break;
            case SecondaryMajorityElectionResultImported ev:
                Apply(ev);
                break;
            case ProportionalElectionResultImported ev:
                _importedCountingCircleIds.Add(GuidParser.Parse(ev.CountingCircleId));
                _importedProportionalElectionIds.Add(GuidParser.Parse(ev.ProportionalElectionId));
                break;
            case VoteResultImported ev:
                _importedCountingCircleIds.Add(GuidParser.Parse(ev.CountingCircleId));
                _importedVoteIds.Add(GuidParser.Parse(ev.VoteId));
                break;
            case ResultImportDataDeleted ev:
                Id = Guid.Parse(ev.ImportId);
                ContestId = Guid.Parse(ev.ContestId);
                CountingCircleId = GuidParser.ParseNullable(ev.CountingCircleId);
                Deleted = ev.EventInfo.Timestamp.ToDateTime();
                break;
            case ResultImportSucceeded:
                _hasSuccessor = true;
                break;
            case CountingCircleVotingCardsImported:
            case MajorityElectionWriteInsMapped:
            case SecondaryMajorityElectionWriteInsMapped:
            case MajorityElectionWriteInsReset:
            case SecondaryMajorityElectionWriteInsReset:
            case MajorityElectionWriteInBallotImported:
            case SecondaryMajorityElectionWriteInBallotImported:
            case ResultImportCountingCircleCompleted:
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }

        // events before eCounting was introduced are all eVoting
        if (ImportType == ResultImportType.Unspecified)
        {
            ImportType = ResultImportType.EVoting;
        }
    }

    private void Apply(MajorityElectionResultImported ev)
    {
        var ccId = GuidParser.Parse(ev.CountingCircleId);
        _importedCountingCircleIds.Add(ccId);
        _importedMajorityElectionIds.Add(GuidParser.Parse(ev.MajorityElectionId));
        SetAvailableWriteIns(GuidParser.Parse(ev.MajorityElectionId), GuidParser.Parse(ev.CountingCircleId), ev.WriteIns);

        if (ev.WriteIns.Count > 0)
        {
            _importedCountingCircleIdsWithWriteIns.Add(ccId);
        }
    }

    private void Apply(SecondaryMajorityElectionResultImported ev)
    {
        var ccId = GuidParser.Parse(ev.CountingCircleId);
        _importedCountingCircleIds.Add(ccId);
        _importedSecondaryMajorityElectionIds.Add(GuidParser.Parse(ev.SecondaryMajorityElectionId));
        SetAvailableWriteIns(GuidParser.Parse(ev.SecondaryMajorityElectionId), GuidParser.Parse(ev.CountingCircleId), ev.WriteIns);

        if (ev.WriteIns.Count > 0)
        {
            _importedCountingCircleIdsWithWriteIns.Add(ccId);
        }
    }

    private void SetAvailableWriteIns(
        Guid electionId,
        Guid countingCircleId,
        IEnumerable<MajorityElectionWriteInEventData> majorityElectionWriteIns)
    {
        _availableWriteInIdsByElectionId[(electionId, countingCircleId)] = majorityElectionWriteIns
                .Select(x => GuidParser.Parse(x.WriteInMappingId))
                .ToHashSet();
    }

    private void ValidateAllWriteInsAvailable(Guid electionId, Guid basisCountingCircleId, List<MajorityElectionWriteInMappedEventData> mappings)
    {
        if (!_availableWriteInIdsByElectionId.TryGetValue((electionId, basisCountingCircleId), out var availableMappingIds))
        {
            throw new ValidationException("no write in mappings available");
        }

        if (mappings.Any(m => !availableMappingIds.Contains(GuidParser.Parse(m.WriteInMappingId))))
        {
            throw new ValidationException("Invalid write in provided");
        }

        if (mappings.Count != availableMappingIds.Count)
        {
            throw new ValidationException("Invalid write ins provided");
        }
    }

    private void EnsureInProgress()
    {
        if (Completed)
        {
            throw new ValidationException("import is already completed");
        }

        if (!Started.HasValue)
        {
            throw new ValidationException("import has not yet started");
        }
    }

    private void EnsureCompleted()
    {
        if (!Completed)
        {
            throw new ValidationException("import is not completed");
        }
    }

    private void EnsureNotStarted()
    {
        if (Started.HasValue || Deleted.HasValue)
        {
            throw new ValidationException("import has already started");
        }
    }

    private void EnsureHasNoSuccessor()
    {
        if (_hasSuccessor)
        {
            throw new ValidationException("import has a successor already.");
        }
    }

    private void EnsureCountingCircleIdMatch(Guid ccId)
    {
        if (CountingCircleId.HasValue && ccId != CountingCircleId)
        {
            throw new ValidationException($"Counting circle id does not match ({ccId} vs {CountingCircleId})");
        }
    }

    private void EnsureImportType(ResultImportType importType)
    {
        if (importType != ImportType)
        {
            throw new ValidationException($"Only imports of type {importType} are supported.");
        }
    }
}
