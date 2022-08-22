// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Basis.Events.V1.Metadata;
using AutoMapper;
using EventStore.Client;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Ausmittlung.EventSignature.Utils;
using Voting.Ausmittlung.Report.EventLogs.Aggregates;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Cryptography.Testing.Mocks;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Models;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using EventSignatureMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignatureMetadata;
using ProtoBasisEvents = Abraxas.Voting.Basis.Events.V1.Data;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfContestActivityProtocolExportBaseTest : PdfExportBaseTest
{
    protected const string Host1 = "Host1";
    protected const string Host2 = "Host2";

    private ulong _position = 1;
    private ulong _eventNumber = 1;

    protected PdfContestActivityProtocolExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    ~PdfContestActivityProtocolExportBaseTest()
    {
        KeyHost1?.Dispose();
        KeyHost1AfterReboot?.Dispose();
        KeyHost2?.Dispose();
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected EventReaderMockStore EventReaderMockStore => GetService<EventReaderMockStore>();

    protected AggregateRepositoryMockStore AggregateRepositoryMockStore => GetService<AggregateRepositoryMockStore>();

    protected IPkcs11DeviceAdapter Pkcs11DeviceAdapter => GetService<IPkcs11DeviceAdapter>();

    protected AsymmetricAlgorithmAdapterMock AsymmetricAlgorithmAdapter => GetService<AsymmetricAlgorithmAdapterMock>();

    protected IMapper Mapper => GetService<IMapper>();

    protected EventSignatureService EventSignatureService => GetService<EventSignatureService>();

    protected override string NewRequestExpectedFileName => "Aktivitätenprotokoll.pdf";

    protected string MonitoringUserId { get; } = "monitoring-admin";

    protected Guid ContestId { get; } = Guid.Parse(ContestMockedData.IdBundesurnengang);

    protected Guid CountingCircleId { get; } = CountingCircleMockedData.GuidGossau;

    protected EcdsaPrivateKey? KeyHost1 { get; private set; }

    protected EcdsaPrivateKey? KeyHost2 { get; private set; }

    protected EcdsaPrivateKey? KeyHost1AfterReboot { get; private set; }

    public override async Task InitializeAsync()
    {
        AggregateRepositoryMockStore.Clear();
        EventReaderMockStore.Clear();

        AsymmetricAlgorithmAdapter.SetNextKeyIndex(0);
        KeyHost1 = AsymmetricAlgorithmAdapter.CreateRandomPrivateKey();
        AsymmetricAlgorithmAdapter.SetNextKeyIndex(1);
        KeyHost2 = AsymmetricAlgorithmAdapter.CreateRandomPrivateKey();
        AsymmetricAlgorithmAdapter.SetNextKeyIndex(2);
        KeyHost1AfterReboot = AsymmetricAlgorithmAdapter.CreateRandomPrivateKey();
        await base.InitializeAsync();
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ResultExportRequests =
                {
                    new GenerateResultExportRequest
                    {
                        Key = AusmittlungPdfContestTemplates.ActivityProtocol.Key,
                    },
                },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected void SeedPublicKeySignatureEvents()
    {
        var protoContestId = ContestId.ToString();
        var aggregateName = AggregateNames.ContestEventSignature;

        var publicKeySignatureHost1 = EventSignatureService.CreatePublicKeySignature(new PublicKeySignaturePayload(
            EventSignatureVersions.V1,
            ContestId,
            Host1,
            KeyHost1!.Id,
            KeyHost1.PublicKey,
            new DateTime(2020, 7, 17, 10, 8, 20, DateTimeKind.Utc),
            new DateTime(2020, 7, 17, 18, 0, 0, DateTimeKind.Utc)));
        var publicKeySignedHost1 = Mapper.Map<EventSignaturePublicKeySigned>(publicKeySignatureHost1);
        publicKeySignedHost1.EventInfo = GetEventInfo(0);

        PublishAusmittlungEvent(publicKeySignedHost1, protoContestId, Host1, aggregateName: aggregateName);

        var publicKeySignatureHost2 = EventSignatureService.CreatePublicKeySignature(new PublicKeySignaturePayload(
            EventSignatureVersions.V1,
            ContestId,
            Host2,
            KeyHost2!.Id,
            KeyHost2.PublicKey,
            new DateTime(2020, 7, 17, 10, 8, 20, DateTimeKind.Utc),
            new DateTime(2020, 7, 17, 10, 10, 0, DateTimeKind.Utc)));
        var publicKeySignedHost2 = Mapper.Map<EventSignaturePublicKeySigned>(publicKeySignatureHost2);
        publicKeySignedHost2.EventInfo = GetEventInfo(0);
        PublishAusmittlungEvent(publicKeySignedHost2, protoContestId, Host2, aggregateName: aggregateName);

        var publicKeySignatureHost2Delete = new EventSignaturePublicKeyDeleted
        {
            ContestId = ContestId.ToString(),
            HostId = Host2,
            KeyId = KeyHost2!.Id,
            EventInfo = GetEventInfo(40), // 2020-07-17 10:09:00
        };
        PublishAusmittlungEvent(publicKeySignatureHost2Delete, protoContestId, Host2, aggregateName: aggregateName);

        var publicKeySignatureHost1AfterReboot = EventSignatureService.CreatePublicKeySignature(new PublicKeySignaturePayload(
            EventSignatureVersions.V1,
            ContestId,
            Host1,
            KeyHost1AfterReboot!.Id,
            KeyHost1AfterReboot.PublicKey,
            new DateTime(2020, 7, 17, 10, 20, 20, DateTimeKind.Utc),
            new DateTime(2020, 7, 17, 18, 0, 0, DateTimeKind.Utc)));
        var publicKeySignedHost1AfterReboot = Mapper.Map<EventSignaturePublicKeySigned>(publicKeySignatureHost1AfterReboot);
        publicKeySignedHost1AfterReboot.EventInfo = GetEventInfo(0);

        PublishAusmittlungEvent(publicKeySignedHost1AfterReboot, protoContestId, Host1, aggregateName: aggregateName);
    }

    protected void PublishAusmittlungEvent<TEventData>(
        TEventData eventData,
        string protoAggregateId,
        string host = "",
        EcdsaPrivateKey? key = null,
        Guid? contestId = null,
        string? aggregateName = null)
        where TEventData : IMessage<TEventData>
    {
        var eventId = Guid.NewGuid();
        var id = Guid.Parse(protoAggregateId);
        var streamName = AggregateNames.Build(aggregateName ?? string.Empty, id);
        var eventInfo = EventInfoUtils.GetEventInfo(eventData);
        var timestamp = eventInfo.Timestamp.ToDateTime();
        contestId ??= ContestId;

        var eventMetadata = new EventSignatureMetadata { ContestId = contestId.Value.ToString() };

        if (key != null)
        {
            eventMetadata.HostId = host;
            eventMetadata.KeyId = key.Id;
            eventMetadata.SignatureVersion = EventSignatureVersions.V1;
            eventMetadata.Signature = ByteString.CopyFrom(EventSignatureService.CreateEventSignature(
                EventSignatureService.BuildEventSignaturePayload(
                    eventId,
                    eventMetadata.SignatureVersion,
                    streamName,
                    contestId.Value,
                    eventData,
                    host,
                    key.Id,
                    eventInfo.Timestamp.ToDateTime()),
                key));
        }

        var position = GetNextPosition();
        var number = GetNextNumber();

        EventReaderMockStore.AddEvent(
            streamName,
            new EventReaderMockStoreData(new EventWithMetadata(eventData, eventMetadata, eventId), position, number, timestamp));

        AggregateRepositoryMockStore.AddEvent(
            id,
            new ReportingDomainEvent(eventId, id, eventData, eventMetadata, position, timestamp));
    }

    protected void PublishBasisEvent<TEventData>(TEventData eventData, string protoAggregateId, Guid? contestId, string? aggregateName = null)
        where TEventData : IMessage<TEventData>
    {
        var eventId = Guid.NewGuid();
        var id = Guid.Parse(protoAggregateId);
        var eventInfo = EventInfoUtils.GetEventInfo(eventData);
        var eventMetadata = contestId != null ? new EventMetadata { ContestId = contestId.ToString() } : null;
        var stream = AggregateNames.Build(aggregateName ?? string.Empty, id);
        var position = GetNextPosition();
        var number = GetNextNumber();
        var timestamp = eventInfo.Timestamp.ToDateTime();

        EventReaderMockStore.AddEvent(
            stream,
            new EventReaderMockStoreData(new EventWithMetadata(eventData, eventMetadata, eventId), position, number, timestamp));

        AggregateRepositoryMockStore.AddEvent(
            id,
            new ReportingDomainEvent(eventId, id, eventData, eventMetadata, position, timestamp));
    }

    protected EventInfo GetEventInfo(long timestampOffset, bool monitoring = false)
    {
        return new EventInfo
        {
            Timestamp = new Timestamp
            {
                Seconds = 1594980500 + timestampOffset,
            },
            Tenant = (monitoring ? SecureConnectTestDefaults.MockedTenantStGallen : SecureConnectTestDefaults.MockedTenantGossau).ToEventInfoTenant(),
            User = monitoring ? new() { Id = MonitoringUserId, Username = "AXADMIN", FirstName = "Monitoring", LastName = "Admin" } : SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
        };
    }

    protected ProtoBasisEvents.EventInfo GetBasisEventInfo(long timestampOffset, bool monitoring = false)
    {
        return new ProtoBasisEvents.EventInfo
        {
            Timestamp = new Timestamp
            {
                Seconds = 1594980500 + timestampOffset,
            },
            Tenant = ToEventInfoTenant(monitoring ? SecureConnectTestDefaults.MockedTenantStGallen : SecureConnectTestDefaults.MockedTenantGossau),
            User = monitoring ? new ProtoBasisEvents.EventInfoUser { Id = MonitoringUserId } : ToEventInfoUser(SecureConnectTestDefaults.MockedUserDefault),
        };
    }

    private Position GetNextPosition()
    {
        return new Position(_position++, 1);
    }

    private StreamPosition GetNextNumber()
    {
        return new StreamPosition(_eventNumber++);
    }

    private ProtoBasisEvents.EventInfoTenant ToEventInfoTenant(Tenant tenant)
    {
        return new ProtoBasisEvents.EventInfoTenant
        {
            Id = tenant.Id,
            Name = tenant.Name,
        };
    }

    private ProtoBasisEvents.EventInfoUser ToEventInfoUser(User user)
    {
        return new ProtoBasisEvents.EventInfoUser
        {
            Id = user.Loginid,
            FirstName = user.Firstname ?? string.Empty,
            LastName = user.Lastname ?? string.Empty,
            Username = user.Username,
        };
    }
}
