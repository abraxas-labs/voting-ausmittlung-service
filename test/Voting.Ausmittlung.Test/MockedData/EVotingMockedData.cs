// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.MockedData;

public static class EVotingMockedData
{
    public static Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped, Func<bool, string, string, string[], HttpClient> createClient)
    {
        return runScoped(async sp =>
        {
            var uri = new Uri(
                $"api/result_import/{ContestMockedData.IdStGallenEvoting}",
                UriKind.RelativeOrAbsolute);

            var client = createClient(
                true,
                SecureConnectTestDefaults.MockedTenantStGallen.Id,
                "default-user-id",
                new[] { RolesMockedData.MonitoringElectionAdmin });

            using var resp = await client.PostFile(uri, "ImportTests/ExampleFiles/ech0222_import_ok.xml");
            resp.EnsureSuccessStatusCode();

            var publisherMock = sp.GetRequiredService<EventPublisherMock>();
            var testPublisher = sp.GetRequiredService<TestEventPublisher>();
            var eventIdCounter = 0;
            await testPublisher.Publish(eventIdCounter++, publisherMock.GetSinglePublishedEvent<ResultImportCreated>());
            await testPublisher.Publish(eventIdCounter++, publisherMock.GetSinglePublishedEvent<ResultImportStarted>());

            var propImportEvents = publisherMock.GetPublishedEvents<ProportionalElectionResultImported>().ToArray();
            await testPublisher.Publish(eventIdCounter, propImportEvents);
            eventIdCounter += propImportEvents.Length;

            var majorityImportEvents = publisherMock.GetPublishedEvents<MajorityElectionResultImported>().ToArray();
            await testPublisher.Publish(eventIdCounter, majorityImportEvents);
            eventIdCounter += majorityImportEvents.Length;

            var secMajorityImportEvents = publisherMock.GetPublishedEvents<SecondaryMajorityElectionResultImported>().ToArray();
            await testPublisher.Publish(eventIdCounter, secMajorityImportEvents);
            eventIdCounter += secMajorityImportEvents.Length;

            var voteImportEvents = publisherMock.GetPublishedEvents<VoteResultImported>().ToArray();
            await testPublisher.Publish(eventIdCounter, voteImportEvents);
            eventIdCounter += voteImportEvents.Length;

            await testPublisher.Publish(eventIdCounter, publisherMock.GetSinglePublishedEvent<ResultImportCompleted>());

            publisherMock.Clear();

            // reset event counter
            var db = sp.GetRequiredService<DataContext>();
            db.EventProcessingStates.Remove(await db.EventProcessingStates.SingleAsync());
            await db.SaveChangesAsync();
        });
    }
}
