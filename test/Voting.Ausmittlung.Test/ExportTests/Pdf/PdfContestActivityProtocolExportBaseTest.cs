﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
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
using Voting.Ausmittlung.Test.MockedData.Mapping;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Cryptography.Testing.Mocks;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Models;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using EventSignatureBusinessMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignatureBusinessMetadata;
using EventSignaturePublicKeyMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignaturePublicKeyMetadata;
using ProtoBasisEventMetadata = Abraxas.Voting.Basis.Events.V1.Metadata;
using ProtoBasisEvents = Abraxas.Voting.Basis.Events.V1;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfContestActivityProtocolExportBaseTest : PdfExportBaseTest<GenerateResultExportsRequest>
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
        AusmittlungKeyHost1?.Dispose();
        AusmittlungKeyHost1AfterReboot?.Dispose();
        AusmittlungKeyHost2?.Dispose();
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected EventReaderMockStore EventReaderMockStore => GetService<EventReaderMockStore>();

    protected AggregateRepositoryMockStore AggregateRepositoryMockStore => GetService<AggregateRepositoryMockStore>();

    protected IPkcs11DeviceAdapter Pkcs11DeviceAdapter => GetService<IPkcs11DeviceAdapter>();

    protected AsymmetricAlgorithmAdapterMock AsymmetricAlgorithmAdapter => GetService<AsymmetricAlgorithmAdapterMock>();

    protected TestMapper Mapper => GetService<TestMapper>();

    protected EventSignatureService EventSignatureService => GetService<EventSignatureService>();

    protected override string NewRequestExpectedFileName => "Aktivitätenprotokoll.pdf";

    protected string MonitoringUserId { get; } = "monitoring-admin";

    protected override string ContestId => ContestMockedData.IdBundesurnengang;

    protected Guid ContestIdGuid { get; } = Guid.Parse(ContestMockedData.IdBundesurnengang);

    protected Guid CountingCircleId { get; } = CountingCircleMockedData.GuidGossau;

    protected EcdsaPrivateKey? AusmittlungKeyHost1 { get; private set; }

    protected EcdsaPrivateKey? AusmittlungKeyHost2 { get; private set; }

    protected EcdsaPrivateKey? AusmittlungKeyHost1AfterReboot { get; private set; }

    protected EcdsaPrivateKey? BasisKeyHost1 { get; private set; }

    public override async Task InitializeAsync()
    {
        AggregateRepositoryMockStore.Clear();
        EventReaderMockStore.Clear();

        AsymmetricAlgorithmAdapter.SetNextKeyIndex(0);
        AusmittlungKeyHost1 = AsymmetricAlgorithmAdapter.CreateRandomPrivateKey();
        AsymmetricAlgorithmAdapter.SetNextKeyIndex(1);
        AusmittlungKeyHost2 = AsymmetricAlgorithmAdapter.CreateRandomPrivateKey();
        AsymmetricAlgorithmAdapter.SetNextKeyIndex(2);
        AusmittlungKeyHost1AfterReboot = AsymmetricAlgorithmAdapter.CreateRandomPrivateKey();
        AsymmetricAlgorithmAdapter.SetNextKeyIndex(3);
        BasisKeyHost1 = AsymmetricAlgorithmAdapter.CreateRandomPrivateKey();
        await base.InitializeAsync();
    }

    public override Task TestPdfAfterTestingPhaseEnded()
    {
        // The contest activity protocol is a special case, no need to test it immediately after the testing phase has ended
        return Task.CompletedTask;
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

    protected void SeedAusmittlungPublicKeySignatureEvents()
    {
        // Create public key for "Host1"
        var publicKeySignatureHost1CreateAuthTagPayload = new PublicKeySignatureCreateAuthenticationTagPayload(
            EventSignatureVersions.V1,
            ContestIdGuid,
            Host1,
            AusmittlungKeyHost1!.Id,
            AusmittlungKeyHost1.PublicKey,
            new DateTime(2020, 7, 17, 10, 8, 20, DateTimeKind.Utc),
            new DateTime(2020, 7, 17, 18, 0, 0, DateTimeKind.Utc));

        var publicKeyHost1Create = EventSignatureService.BuildPublicKeyCreate(new PublicKeySignatureCreateHsmPayload(
            publicKeySignatureHost1CreateAuthTagPayload,
            AsymmetricAlgorithmAdapter.CreateSignature(publicKeySignatureHost1CreateAuthTagPayload.ConvertToBytesToSign(), AusmittlungKeyHost1)));

        var publicKeyHost1Created = Mapper.Map<EventSignaturePublicKeyCreated>(publicKeyHost1Create);
        publicKeyHost1Created.EventInfo = GetEventInfo(0);
        PublishAusmittlungPublicKeyEvent(publicKeyHost1Created, publicKeyHost1Create.HsmSignature);

        // Create public key for "Host2"
        var publicKeySignatureHost2CreateAuthTagPayload = new PublicKeySignatureCreateAuthenticationTagPayload(
            EventSignatureVersions.V1,
            ContestIdGuid,
            Host2,
            AusmittlungKeyHost2!.Id,
            AusmittlungKeyHost2.PublicKey,
            new DateTime(2020, 7, 17, 10, 8, 20, DateTimeKind.Utc),
            new DateTime(2020, 7, 17, 10, 10, 0, DateTimeKind.Utc));

        var publicKeyHost2Create = EventSignatureService.BuildPublicKeyCreate(new PublicKeySignatureCreateHsmPayload(
            publicKeySignatureHost2CreateAuthTagPayload,
            AsymmetricAlgorithmAdapter.CreateSignature(publicKeySignatureHost2CreateAuthTagPayload.ConvertToBytesToSign(), AusmittlungKeyHost2)));

        var publicKeyHost2Created = Mapper.Map<EventSignaturePublicKeyCreated>(publicKeyHost2Create);
        publicKeyHost2Created.EventInfo = GetEventInfo(0);
        PublishAusmittlungPublicKeyEvent(publicKeyHost2Created, publicKeyHost2Create.HsmSignature);

        var publicKeyHost2DeleteAuthTagPayload = new PublicKeySignatureDeleteAuthenticationTagPayload(
            EventSignatureVersions.V1,
            ContestIdGuid,
            Host2,
            AusmittlungKeyHost2.Id,
            new DateTime(2020, 7, 17, 10, 9, 0, DateTimeKind.Utc),
            0);

        var publicKeyHost2Delete = EventSignatureService.BuildPublicKeyDelete(new PublicKeySignatureDeleteHsmPayload(
            publicKeyHost2DeleteAuthTagPayload,
            AsymmetricAlgorithmAdapter.CreateSignature(publicKeyHost2DeleteAuthTagPayload.ConvertToBytesToSign(), AusmittlungKeyHost2)));

        var publicKeyHost2Deleted = Mapper.Map<EventSignaturePublicKeyDeleted>(publicKeyHost2Delete);
        publicKeyHost2Deleted.EventInfo = GetEventInfo(40);
        PublishAusmittlungPublicKeyEvent(publicKeyHost2Deleted, publicKeyHost2Delete.HsmSignature);

        // Create public key for "Host1 After Reboot"
        var publicKeyHost1AfterRebootCreateAuthTagPayload = new PublicKeySignatureCreateAuthenticationTagPayload(
            EventSignatureVersions.V1,
            ContestIdGuid,
            Host1,
            AusmittlungKeyHost1AfterReboot!.Id,
            AusmittlungKeyHost1AfterReboot.PublicKey,
            new DateTime(2020, 7, 17, 10, 20, 20, DateTimeKind.Utc),
            new DateTime(2020, 7, 17, 18, 0, 0, DateTimeKind.Utc));

        var publicKeyHost1AfterRebootCreate = EventSignatureService.BuildPublicKeyCreate(new PublicKeySignatureCreateHsmPayload(
            publicKeyHost1AfterRebootCreateAuthTagPayload,
            AsymmetricAlgorithmAdapter.CreateSignature(publicKeyHost1AfterRebootCreateAuthTagPayload.ConvertToBytesToSign(), AusmittlungKeyHost1AfterReboot)));

        var publicKeyHost1AfterRebootCreated = Mapper.Map<EventSignaturePublicKeyCreated>(publicKeyHost1AfterRebootCreate);
        publicKeyHost1AfterRebootCreated.EventInfo = GetEventInfo(0);

        PublishAusmittlungPublicKeyEvent(publicKeyHost1AfterRebootCreated, publicKeyHost1AfterRebootCreate.HsmSignature);
    }

    protected void SeedBasisPublicKeySignatureEvents()
    {
        // Create public key for "Host1".
        var publicKeyHost1CreateAuthTagPayload = new PublicKeySignatureCreateAuthenticationTagPayload(
            EventSignatureVersions.V1,
            ContestIdGuid,
            Host1,
            BasisKeyHost1!.Id,
            BasisKeyHost1.PublicKey,
            new DateTime(2020, 7, 17, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2020, 7, 17, 23, 0, 0, DateTimeKind.Utc));

        var publicKeyHost1Create = EventSignatureService.BuildPublicKeyCreate(new PublicKeySignatureCreateHsmPayload(
            publicKeyHost1CreateAuthTagPayload,
            AsymmetricAlgorithmAdapter.CreateSignature(publicKeyHost1CreateAuthTagPayload.ConvertToBytesToSign(), BasisKeyHost1)));

        var publicKeyHost1Created = Mapper.Map<ProtoBasisEvents.EventSignaturePublicKeyCreated>(publicKeyHost1Create);
        publicKeyHost1Created.EventInfo = GetBasisEventInfo(0);
        PublishBasisPublicKeyEvent(publicKeyHost1Created, publicKeyHost1Create.HsmSignature);

        var publicKeyHost1DeleteAuthTagPayload = new PublicKeySignatureDeleteAuthenticationTagPayload(
            EventSignatureVersions.V1,
            ContestIdGuid,
            Host1,
            BasisKeyHost1!.Id,
            new DateTime(2020, 7, 17, 10, 45, 24, DateTimeKind.Utc),
            0);

        var publicKeyHost1Delete = EventSignatureService.BuildPublicKeyDelete(new PublicKeySignatureDeleteHsmPayload(
            publicKeyHost1DeleteAuthTagPayload,
            AsymmetricAlgorithmAdapter.CreateSignature(publicKeyHost1DeleteAuthTagPayload.ConvertToBytesToSign(), BasisKeyHost1)));

        var publicKeyHost1Deleted = Mapper.Map<ProtoBasisEvents.EventSignaturePublicKeyDeleted>(publicKeyHost1Delete);
        publicKeyHost1Deleted.EventInfo = GetBasisEventInfo(40);
        PublishBasisPublicKeyEvent(publicKeyHost1Deleted, publicKeyHost1Delete.HsmSignature);
    }

    protected void PublishAusmittlungBusinessEvent<TEventData>(
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
        contestId ??= ContestIdGuid;

        var eventMetadata = new EventSignatureBusinessMetadata { ContestId = contestId.Value.ToString() };

        if (key != null)
        {
            eventMetadata.HostId = host;
            eventMetadata.KeyId = key.Id;
            eventMetadata.SignatureVersion = EventSignatureVersions.V1;
            eventMetadata.Signature = ByteString.CopyFrom(EventSignatureService.CreateBusinessSignature(
                EventSignatureService.BuildBusinessPayload(
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

    protected void PublishAusmittlungPublicKeyEvent<TEventData>(
        TEventData eventData,
        byte[] hsmSignature,
        Guid? contestId = null)
        where TEventData : IMessage<TEventData>
    {
        var id = contestId ?? ContestIdGuid;
        var eventId = Guid.NewGuid();

        var streamName = AggregateNames.Build(AggregateNames.ContestEventSignatureAusmittlung, id);
        var eventMetadata = new EventSignaturePublicKeyMetadata { HsmSignature = ByteString.CopyFrom(hsmSignature) };

        var position = GetNextPosition();
        var number = GetNextNumber();
        var eventInfo = EventInfoUtils.GetEventInfo(eventData);
        var timestamp = eventInfo.Timestamp.ToDateTime();

        EventReaderMockStore.AddEvent(
            streamName,
            new EventReaderMockStoreData(new EventWithMetadata(eventData, eventMetadata), position, number, timestamp));

        AggregateRepositoryMockStore.AddEvent(
            id,
            new ReportingDomainEvent(eventId, id, eventData, eventMetadata, position, timestamp));
    }

    protected void PublishBasisPublicKeyEvent<TEventData>(
        TEventData eventData,
        byte[] hsmSignature,
        Guid? contestId = null)
        where TEventData : IMessage<TEventData>
    {
        var id = contestId ?? ContestIdGuid;
        var eventId = Guid.NewGuid();

        var streamName = AggregateNames.Build(AggregateNames.ContestEventSignatureBasis, id);
        var eventMetadata = new ProtoBasisEventMetadata.EventSignaturePublicKeyMetadata { HsmSignature = ByteString.CopyFrom(hsmSignature) };

        var position = GetNextPosition();
        var number = GetNextNumber();
        var eventInfo = EventInfoUtils.GetEventInfo(eventData);
        var timestamp = eventInfo.Timestamp.ToDateTime();

        EventReaderMockStore.AddEvent(
            streamName,
            new EventReaderMockStoreData(new EventWithMetadata(eventData, eventMetadata), position, number, timestamp));

        AggregateRepositoryMockStore.AddEvent(
            id,
            new ReportingDomainEvent(eventId, id, eventData, eventMetadata, position, timestamp));
    }

    protected void PublishBasisBusinessEvent<TEventData>(
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
        contestId ??= ContestIdGuid;

        var eventMetadata = new ProtoBasisEventMetadata.EventSignatureBusinessMetadata { ContestId = contestId.Value.ToString() };

        if (key != null)
        {
            eventMetadata.HostId = host;
            eventMetadata.KeyId = key.Id;
            eventMetadata.SignatureVersion = EventSignatureVersions.V1;
            eventMetadata.Signature = ByteString.CopyFrom(EventSignatureService.CreateBusinessSignature(
                EventSignatureService.BuildBusinessPayload(
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

    protected ProtoBasisEvents.Data.EventInfo GetBasisEventInfo(long timestampOffset, bool monitoring = false)
    {
        return new ProtoBasisEvents.Data.EventInfo
        {
            Timestamp = new Timestamp
            {
                Seconds = 1594980500 + timestampOffset,
            },
            Tenant = ToEventInfoTenant(monitoring ? SecureConnectTestDefaults.MockedTenantStGallen : SecureConnectTestDefaults.MockedTenantGossau),
            User = monitoring ? new ProtoBasisEvents.Data.EventInfoUser { Id = MonitoringUserId } : ToEventInfoUser(SecureConnectTestDefaults.MockedUserDefault),
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

    private ProtoBasisEvents.Data.EventInfoTenant ToEventInfoTenant(Tenant tenant)
    {
        return new ProtoBasisEvents.Data.EventInfoTenant
        {
            Id = tenant.Id,
            Name = tenant.Name,
        };
    }

    private ProtoBasisEvents.Data.EventInfoUser ToEventInfoUser(User user)
    {
        return new ProtoBasisEvents.Data.EventInfoUser
        {
            Id = user.Loginid,
            FirstName = user.Firstname ?? string.Empty,
            LastName = user.Lastname ?? string.Empty,
            Username = user.Username,
        };
    }
}
