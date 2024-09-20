// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Ausmittlung.Report.EventLogs.Aggregates;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;
using Voting.Lib.Cryptography.Asymmetric;

namespace Voting.Ausmittlung.Report.EventLogs;

public class EventLogBuilderContext : IDisposable
{
    private readonly PublicKeySignatureVerifier _publicKeySignatureVerifier;
    private readonly IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> _asymmetricAlgorithmAdapter;
    private readonly Dictionary<string, PublicKeySignatureValidationResult> _publicKeySignatureValidationResultsByKeyId = new();
    private readonly ContestEventSignatureAggregate _contestEventSignatureAggregate;
    private readonly Dictionary<string, long> _signedEventCountByKeyId = new();

    public EventLogBuilderContext(
        Guid contestId,
        bool testingPhaseEnded,
        IEnumerable<Guid> politicalBusinessIdsFilter,
        ContestEventSignatureAggregate contestEventSignatureAggregate,
        Position basisSnapshotPosition,
        DateTime startTimestampInStream,
        IServiceProvider sp)
    {
        _publicKeySignatureVerifier = sp.GetRequiredService<PublicKeySignatureVerifier>();
        _asymmetricAlgorithmAdapter = sp.GetRequiredService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();
        _contestEventSignatureAggregate = contestEventSignatureAggregate;

        CountingCircleAggregateSet = sp.GetRequiredService<AggregateSet<CountingCircleAggregate>>();
        VoteAggregateSet = sp.GetRequiredService<AggregateSet<VoteAggregate>>();
        ProportionalElectionAggregateSet = sp.GetRequiredService<AggregateSet<ProportionalElectionAggregate>>();
        MajorityElectionAggregateSet = sp.GetRequiredService<MajorityElectionAggregateSet>();

        ContestId = contestId;
        TestingPhaseEnded = testingPhaseEnded;
        PoliticalBusinessIdsFilter = politicalBusinessIdsFilter.ToList();
        StartPosition = basisSnapshotPosition;

        StartTimestampInStream = startTimestampInStream;
        CurrentTimestampInStream = startTimestampInStream;
    }

    public Guid ContestId { get; }

    public bool TestingPhaseEnded { get; }

    /// <summary>
    /// Gets the position of the contest created event if testing phase has not ended yet or the position of the contest testing phase ended event.
    /// </summary>
    public Position StartPosition { get; }

    // TODO: can be removed with https://jira.abraxas-tools.ch/jira/browse/VOTING-1856.
    public DateTime StartTimestampInStream { get; }

    /// <summary>
    /// Gets the current event created timestamp. The event at this position is not necessarily already processed.
    /// </summary>
    // TODO: can be removed with https://jira.abraxas-tools.ch/jira/browse/VOTING-1856.
    public DateTime CurrentTimestampInStream { get; internal set; }

    public IReadOnlyCollection<Guid> PoliticalBusinessIdsFilter { get; }

    public Dictionary<Guid, (Guid PoliticalBusinessId, Guid CountingCircleId)> PoliticalBusinessIdAndCountingCircleIdByResultId { get; } = new();

    public AggregateSet<CountingCircleAggregate> CountingCircleAggregateSet { get; }

    public AggregateSet<VoteAggregate> VoteAggregateSet { get; }

    public AggregateSet<ProportionalElectionAggregate> ProportionalElectionAggregateSet { get; }

    public MajorityElectionAggregateSet MajorityElectionAggregateSet { get; }

    public Dictionary<Guid, PoliticalBusinessResultBundleAggregate> PoliticalBusinessResultBundles { get; } = new();

    public Dictionary<string, EventLogUser> EventLogUsers { get; } = new();

    public Dictionary<string, EventLogTenant> EventLogTenants { get; } = new();

    public bool IsPoliticalBusinessIncluded(Guid politicalBusinessId)
    {
        return PoliticalBusinessIdsFilter.Count == 0
            || PoliticalBusinessIdsFilter.Contains(politicalBusinessId)
            || MajorityElectionAggregateSet.GetBySecondaryMajorityElectionId(politicalBusinessId) != null;
    }

    public EventSignaturePublicKeyAggregateData? GetPublicKeyAggregateData(string keyId)
    {
        return _contestEventSignatureAggregate.GetPublicKeyAggregateData(keyId);
    }

    public long? GetReadAtGenerationSignedEventCount(string keyId)
    {
        var signedEventCount = _signedEventCountByKeyId.GetValueOrDefault(keyId);
        return signedEventCount != 0 ? signedEventCount : null;
    }

    public void IncrementSignedEventCount(string keyId)
    {
        if (_signedEventCountByKeyId.TryGetValue(keyId, out var signedEventCount))
        {
            _signedEventCountByKeyId[keyId] = ++signedEventCount;
            return;
        }

        _signedEventCountByKeyId.Add(keyId, 1);
    }

    public PublicKeySignatureValidationResult? GetPublicKeySignatureValidationResult(string keyId)
    {
        if (_publicKeySignatureValidationResultsByKeyId.TryGetValue(keyId, out var publicKeyValidationResult))
        {
            return publicKeyValidationResult;
        }

        var publicKeyAggregateData = GetPublicKeyAggregateData(keyId);
        if (publicKeyAggregateData == null)
        {
            return null;
        }

        var publicKey = _asymmetricAlgorithmAdapter.CreatePublicKey(publicKeyAggregateData.CreateData.PublicKey, keyId);

        var publicKeyData = new PublicKeyData(
            publicKey,
            publicKeyAggregateData.CreateData.ValidFrom,
            publicKeyAggregateData.CreateData.ValidTo,
            publicKeyAggregateData.DeleteData?.DeletedAt);

        var signatureCreateData = new PublicKeySignatureCreateData(
            publicKey.Id,
            publicKeyAggregateData.CreateData.SignatureVersion,
            publicKeyAggregateData.CreateData.ContestId,
            publicKeyAggregateData.CreateData.HostId,
            publicKeyAggregateData.CreateData.AuthenticationTag,
            publicKeyAggregateData.CreateData.HsmSignature);

        var signatureDeleteData = publicKeyAggregateData.DeleteData == null ? null : new PublicKeySignatureDeleteData(
            publicKey.Id,
            publicKeyAggregateData.DeleteData.SignatureVersion,
            publicKeyAggregateData.DeleteData.ContestId,
            publicKeyAggregateData.DeleteData.HostId,
            publicKeyAggregateData.DeleteData.SignedEventCount,
            publicKeyAggregateData.DeleteData.AuthenticationTag,
            publicKeyAggregateData.DeleteData.HsmSignature);

        var validationResult = _publicKeySignatureVerifier.VerifySignature(signatureCreateData, signatureDeleteData, publicKeyData);

        _publicKeySignatureValidationResultsByKeyId.Add(publicKey.Id, validationResult);
        return validationResult;
    }

    public IReadOnlyCollection<PublicKeySignatureValidationResult> GetPublicKeySignatureValidationResults()
    {
        return _publicKeySignatureValidationResultsByKeyId.Values.ToList();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // a validation result contains key with an ECDsa instance which has to be disposed.
            foreach (var publicKeySignatureValidationResult in _publicKeySignatureValidationResultsByKeyId)
            {
                publicKeySignatureValidationResult.Value.KeyData.Key.Dispose();
            }
        }
    }
}
