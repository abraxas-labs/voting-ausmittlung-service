// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Lib.Common;
using MajorityElectionWriteInMappingTarget = Abraxas.Voting.Ausmittlung.Shared.V1.MajorityElectionWriteInMappingTarget;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ResultImportAggregate : BaseEventSignatureAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    private readonly ISet<string> _importedMajorityElectionIds = new HashSet<string>();
    private readonly ISet<string> _importedSecondaryMajorityElectionIds = new HashSet<string>();
    private readonly ISet<string> _importedProportionalElectionIds = new HashSet<string>();
    private readonly ISet<string> _importedVoteIds = new HashSet<string>();
    private readonly Dictionary<(Guid ElectionId, Guid BasisCountingCircleId), ISet<Guid>> _availableWriteInIdsByElectionId = new();

    private bool _hasSuccessor;

    public ResultImportAggregate(EventInfoProvider eventInfoProvider, IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _mapper = mapper;
    }

    public override string AggregateName => "voting-resultImport";

    public string FileName { get; private set; } = string.Empty;

    public Guid ContestId { get; private set; }

    public DateTime? Started { get; private set; }

    public DateTime? Deleted { get; private set; }

    public bool Completed { get; private set; }

    internal void Start(string fileName, Guid contestId, string echMessageId)
    {
        EnsureNotStarted();
        EnsureHasNoSuccessor();

        Id = Guid.NewGuid();
        RaiseEvent(
            new ResultImportStarted
            {
                ContestId = contestId.ToString(),
                EventInfo = _eventInfoProvider.NewEventInfo(),
                FileName = fileName,
                ImportId = Id.ToString(),
                EchMessageId = echMessageId,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    internal void ImportProportionalElectionResult(ProportionalElectionResultImport data)
    {
        EnsureInProgress();
        EnsureHasNoSuccessor();

        var ev = new ProportionalElectionResultImported
        {
            ContestId = ContestId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ImportId = Id.ToString(),
        };

        _mapper.Map(data, ev);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));
    }

    internal void ImportMajorityElectionResult(MajorityElectionResultImport data)
    {
        EnsureInProgress();
        EnsureHasNoSuccessor();

        var ev = new MajorityElectionResultImported
        {
            ContestId = ContestId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ImportId = Id.ToString(),
        };

        _mapper.Map(data, ev);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));
    }

    internal void ImportSecondaryMajorityElectionResult(MajorityElectionResultImport data)
    {
        EnsureInProgress();
        EnsureHasNoSuccessor();

        var ev = new SecondaryMajorityElectionResultImported
        {
            ContestId = ContestId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ImportId = Id.ToString(),
        };

        _mapper.Map(data, ev);
        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));
    }

    internal void ImportVoteResult(VoteResultImport data)
    {
        EnsureInProgress();
        EnsureHasNoSuccessor();

        var ev = new VoteResultImported
        {
            ContestId = ContestId.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
            ImportId = Id.ToString(),
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
                WriteInMappings =
                    {
                        writeIns,
                    },
            },
            _ => throw new InvalidOperationException(nameof(politicalBusinessType) + " does not support write ins"),
        };

        RaiseEvent(ev, new EventSignatureBusinessDomainData(ContestId));
    }

    /// <summary>
    /// Deletes all imported data of a contest.
    /// </summary>
    /// <param name="contestId">The id of the contest.</param>
    internal void DeleteData(Guid contestId)
    {
        EnsureNotStarted();
        EnsureHasNoSuccessor();

        RaiseEvent(
            new ResultImportDataDeleted
            {
                ImportId = Guid.NewGuid().ToString(),
                ContestId = contestId.ToString(),
                EventInfo = _eventInfoProvider.NewEventInfo(),
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
                EventInfo = _eventInfoProvider.NewEventInfo(),
                FileName = FileName,
                ImportId = Id.ToString(),
                ImportedMajorityElectionIds =
                {
                        _importedMajorityElectionIds,
                },
                ImportedSecondaryMajorityElectionIds =
                {
                        _importedSecondaryMajorityElectionIds,
                },
                ImportedProportionalElectionIds =
                {
                        _importedProportionalElectionIds,
                },
                ImportedVoteIds =
                {
                        _importedVoteIds,
                },
            },
            new EventSignatureBusinessDomainData(ContestId));
    }

    internal void SucceedBy(Guid successorImportId)
    {
        EnsureHasNoSuccessor();

        RaiseEvent(
            new ResultImportSucceeded
            {
                ContestId = ContestId.ToString(),
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ImportId = Id.ToString(),
                SuccessorImportId = successorImportId.ToString(),
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
                ContestId = Guid.Parse(ev.ContestId);
                Started = ev.EventInfo.Timestamp.ToDateTime();
                break;
            case ResultImportCompleted _:
                Completed = true;
                break;
            case MajorityElectionResultImported ev:
                _importedMajorityElectionIds.Add(ev.MajorityElectionId);
                SetAvailableWriteIns(GuidParser.Parse(ev.MajorityElectionId), GuidParser.Parse(ev.CountingCircleId), ev.WriteIns);
                break;
            case SecondaryMajorityElectionResultImported ev:
                _importedSecondaryMajorityElectionIds.Add(ev.SecondaryMajorityElectionId);
                SetAvailableWriteIns(GuidParser.Parse(ev.SecondaryMajorityElectionId), GuidParser.Parse(ev.CountingCircleId), ev.WriteIns);
                break;
            case ProportionalElectionResultImported ev:
                _importedProportionalElectionIds.Add(ev.ProportionalElectionId);
                break;
            case VoteResultImported ev:
                _importedVoteIds.Add(ev.VoteId);
                break;
            case ResultImportDataDeleted ev:
                Id = Guid.Parse(ev.ImportId);
                ContestId = Guid.Parse(ev.ContestId);
                Deleted = ev.EventInfo.Timestamp.ToDateTime();
                break;
            case ResultImportSucceeded:
                _hasSuccessor = true;
                break;
            case MajorityElectionWriteInsMapped:
            case SecondaryMajorityElectionWriteInsMapped:
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
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

    private void ValidateAllWriteInsAvailable(Guid electionId, Guid basisCountingCircleId, IEnumerable<MajorityElectionWriteInMappedEventData> mappings)
    {
        if (!_availableWriteInIdsByElectionId.TryGetValue((electionId, basisCountingCircleId), out var availableMappingIds))
        {
            throw new ValidationException("no write in mappings available");
        }

        if (mappings.Any(m => !availableMappingIds.Contains(GuidParser.Parse(m.WriteInMappingId))))
        {
            throw new ValidationException("Invalid write in provided");
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
}
