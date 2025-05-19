// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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

public static class ResultImportMockedData
{
    public static Task SeedEVoting(
        Func<Func<IServiceProvider, Task>, Task> runScoped,
        Func<bool, string, string, string[], HttpClient> createClient)
    {
        var uri = new Uri(
            $"api/result_import/e-voting/{ContestMockedData.IdStGallenEvoting}",
            UriKind.RelativeOrAbsolute);

        var ech0222File = "ImportTests/ExampleFiles/ech0222_import_ok.xml";
        var ech0110File = "ImportTests/ExampleFiles/ech0110_import_ok.xml";
        return Seed(runScoped, createClient, RolesMockedData.MonitoringElectionAdmin, SecureConnectTestDefaults.MockedTenantStGallen.Id, uri, ech0222File, ech0110File);
    }

    public static Task SeedECounting(
        Func<Func<IServiceProvider, Task>, Task> runScoped,
        Func<bool, string, string, string[], HttpClient> createClient)
    {
        var uri = new Uri(
            $"api/result_import/e-counting/{ContestMockedData.IdStGallenEvoting}/{CountingCircleMockedData.IdUzwil}",
            UriKind.RelativeOrAbsolute);

        var ech0222File = "ImportTests/ExampleFiles/ech0222_e_counting_import_ok.xml";
        return Seed(runScoped, createClient, RolesMockedData.ErfassungElectionAdmin, SecureConnectTestDefaults.MockedTenantUzwil.Id, uri, ech0222File);
    }

    private static Task Seed(
        Func<Func<IServiceProvider, Task>, Task> runScoped,
        Func<bool, string, string, string[], HttpClient> createClient,
        string role,
        string tenantId,
        Uri endpoint,
        string ech0222File,
        string? ech0110File = null)
    {
        return runScoped(async sp =>
        {
            var client = createClient(
                true,
                tenantId,
                "default-user-id",
                [role]);

            List<(string, string)> files = [("ech0222File", ech0222File)];
            if (ech0110File != null)
            {
                files.Add(("ech0110File", ech0110File));
            }

            using var resp = await client.PostFiles(endpoint, files.ToArray());
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

            var majorityElectionWriteInBallotEvents =
                publisherMock.GetPublishedEvents<MajorityElectionWriteInBallotImported>().ToArray();
            await testPublisher.Publish(eventIdCounter, majorityElectionWriteInBallotEvents);
            eventIdCounter += majorityElectionWriteInBallotEvents.Length;

            var secMajorityImportEvents =
                publisherMock.GetPublishedEvents<SecondaryMajorityElectionResultImported>().ToArray();
            await testPublisher.Publish(eventIdCounter, secMajorityImportEvents);
            eventIdCounter += secMajorityImportEvents.Length;

            var secMajorityElectionWriteInBallotEvents = publisherMock
                .GetPublishedEvents<SecondaryMajorityElectionWriteInBallotImported>().ToArray();
            await testPublisher.Publish(eventIdCounter, secMajorityElectionWriteInBallotEvents);
            eventIdCounter += secMajorityElectionWriteInBallotEvents.Length;

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
