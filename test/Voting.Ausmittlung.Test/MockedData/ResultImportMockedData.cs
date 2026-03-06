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
    public static Task<long> SeedEVoting(
        Func<Func<IServiceProvider, Task>, Task> runScoped,
        Func<bool, string, string, string[], HttpClient> createClient,
        long eventNrCounter = 0)
    {
        var uri = new Uri(
            $"api/result_import/e-voting/{ContestMockedData.IdStGallenEvoting}",
            UriKind.RelativeOrAbsolute);

        var ech0222File = "ImportTests/ExampleFiles/ech0222_import_ok.xml";
        var ech0110File = "ImportTests/ExampleFiles/ech0110_import_ok.xml";
        return Seed(eventNrCounter, runScoped, createClient, RolesMockedData.MonitoringElectionAdmin, SecureConnectTestDefaults.MockedTenantStGallen.Id, uri, ech0222File, ech0110File);
    }

    public static Task<long> SeedECounting(
        Func<Func<IServiceProvider, Task>, Task> runScoped,
        Func<bool, string, string, string[], HttpClient> createClient,
        long eventNrCounter = 0)
    {
        var uri = new Uri(
            $"api/result_import/e-counting/{ContestMockedData.IdStGallenEvoting}/{CountingCircleMockedData.IdUzwil}",
            UriKind.RelativeOrAbsolute);

        var ech0222File = "ImportTests/ExampleFiles/ech0222_e_counting_import_ok.xml";
        return Seed(eventNrCounter, runScoped, createClient, RolesMockedData.ErfassungElectionAdmin, SecureConnectTestDefaults.MockedTenantUzwil.Id, uri, ech0222File);
    }

    private static async Task<long> Seed(
        long eventNrCounter,
        Func<Func<IServiceProvider, Task>, Task> runScoped,
        Func<bool, string, string, string[], HttpClient> createClient,
        string role,
        string tenantId,
        Uri endpoint,
        string ech0222File,
        string? ech0110File = null)
    {
        await runScoped(async sp =>
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
            await testPublisher.Publish(eventNrCounter++, publisherMock.GetSinglePublishedEvent<ResultImportCreated>());
            await testPublisher.Publish(eventNrCounter++, publisherMock.GetSinglePublishedEvent<ResultImportStarted>());

            var propImportEvents = publisherMock.GetPublishedEvents<ProportionalElectionResultImported>().ToArray();
            await testPublisher.Publish(eventNrCounter, propImportEvents);
            eventNrCounter += propImportEvents.Length;

            var majorityImportEvents = publisherMock.GetPublishedEvents<MajorityElectionResultImported>().ToArray();
            await testPublisher.Publish(eventNrCounter, majorityImportEvents);
            eventNrCounter += majorityImportEvents.Length;

            var majorityElectionWriteInBallotEvents =
                publisherMock.GetPublishedEvents<MajorityElectionWriteInBallotImported>().ToArray();
            await testPublisher.Publish(eventNrCounter, majorityElectionWriteInBallotEvents);
            eventNrCounter += majorityElectionWriteInBallotEvents.Length;

            var secMajorityImportEvents =
                publisherMock.GetPublishedEvents<SecondaryMajorityElectionResultImported>().ToArray();
            await testPublisher.Publish(eventNrCounter, secMajorityImportEvents);
            eventNrCounter += secMajorityImportEvents.Length;

            var secMajorityElectionWriteInBallotEvents = publisherMock
                .GetPublishedEvents<SecondaryMajorityElectionWriteInBallotImported>().ToArray();
            await testPublisher.Publish(eventNrCounter, secMajorityElectionWriteInBallotEvents);
            eventNrCounter += secMajorityElectionWriteInBallotEvents.Length;

            var voteImportEvents = publisherMock.GetPublishedEvents<VoteResultImported>().ToArray();
            await testPublisher.Publish(eventNrCounter, voteImportEvents);
            eventNrCounter += voteImportEvents.Length;

            await testPublisher.Publish(eventNrCounter, publisherMock.GetSinglePublishedEvent<ResultImportCompleted>());

            publisherMock.Clear();

            // reset event counter
            var db = sp.GetRequiredService<DataContext>();
            db.EventProcessingStates.Remove(await db.EventProcessingStates.SingleAsync());
            await db.SaveChangesAsync();
        });

        return eventNrCounter;
    }
}
