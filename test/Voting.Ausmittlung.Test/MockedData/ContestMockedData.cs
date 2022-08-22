// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Utils.Snapshot;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Cryptography.Asymmetric;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ContestMockedData
{
    public const string IdArchivedBundesurnengang = "40e66d0c-ebab-44a9-9567-5cd9b83f960b";
    public const string IdVergangenerBundesurnengang = "4b1513ce-89fd-4ce8-bf71-7b55a50b8943";
    public const string IdBundesurnengang = "20361fdf-7d18-47b9-bdef-fd82efe8a6a7";
    public const string IdStGallenEvoting = "95825eb0-0f52-461a-a5f8-23fb35fa69e1";
    public const string IdStGallenStadt = "df660313-ab05-4b17-9826-492c34432098";
    public const string IdGossau = "bfd88d88-77f2-4172-a73d-56b1ca5442b3";
    public const string IdUzwilEvoting = "cc70fe43-8f4e-4bc6-a461-b808907bc996";
    public const string IdKirche = "a091a5cc-735b-4bf4-a30d-f4c907c9fc10";
    public const string IdThurgauNoPoliticalBusinesses = "dc0f4940-10c2-4a33-b752-3e5d761c0009";

    private static readonly IReadOnlyDictionary<Guid, Contest> _contestById = All.ToDictionary(x => x.Id, x => x);

    public static Contest ArchivedBundesurnengang
        => new Contest
        {
            Id = Guid.Parse(IdArchivedBundesurnengang),
            Date = new DateTime(2019, 11, 24, 0, 0, 0, DateTimeKind.Utc),
            Translations = TranslationUtil.CreateTranslations<ContestTranslation>(
                                (t, o) => t.Description = o,
                                "Nationalratswahlen in der Verangenheit und archiviert"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            EndOfTestingPhase = new DateTime(2019, 10, 23, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.Archived,
        };

    public static Contest VergangenerBundesurnengang
        => new Contest
        {
            Id = Guid.Parse(IdVergangenerBundesurnengang),
            Date = new DateTime(2019, 11, 24, 0, 0, 0, DateTimeKind.Utc),
            Translations = TranslationUtil.CreateTranslations<ContestTranslation>(
                                (t, o) => t.Description = o,
                                "Nationalratswahlen in der Verangenheit"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            EndOfTestingPhase = new DateTime(2019, 10, 23, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.PastLocked,
        };

    public static Contest Bundesurnengang
        => new Contest
        {
            Id = Guid.Parse(IdBundesurnengang),
            Date = new DateTime(2029, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            Translations = TranslationUtil.CreateTranslations<ContestTranslation>(
                                (t, o) => t.Description = o,
                                "Nationalratswahlen in der Zukunft"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            EndOfTestingPhase = new DateTime(2028, 12, 20, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.TestingPhase,
            PreviousContestId = Guid.Parse(IdVergangenerBundesurnengang),
        };

    public static Contest StGallenEvotingUrnengang
        => new Contest
        {
            Id = Guid.Parse(IdStGallenEvoting),
            Date = new DateTime(2020, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            Translations = TranslationUtil.CreateTranslations<ContestTranslation>(
                                (t, o) => t.Description = o,
                                "Kantonsurnengang"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            EndOfTestingPhase = new DateTime(2018, 12, 20, 0, 0, 0, DateTimeKind.Utc),
            EVoting = true,
            EVotingFrom = new DateTime(2020, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            EVotingTo = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.TestingPhase,
        };

    public static Contest StGallenStadtUrnengang
        => new Contest
        {
            Id = Guid.Parse(IdStGallenStadt),
            Date = new DateTime(2020, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            Translations = TranslationUtil.CreateTranslations<ContestTranslation>(
                (t, o) => t.Description = o,
                "Stadturnengang"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallenStadt),
            EndOfTestingPhase = new DateTime(2018, 12, 20, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.TestingPhase,
        };

    public static Contest GossauUrnengang
        => new Contest
        {
            Id = Guid.Parse(IdGossau),
            Date = new DateTime(2020, 2, 29, 0, 0, 0, DateTimeKind.Utc),
            Translations = TranslationUtil.CreateTranslations<ContestTranslation>(
                                (t, o) => t.Description = o,
                                "Gossau Urnengang"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
            EndOfTestingPhase = new DateTime(2020, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.TestingPhase,
        };

    public static Contest UzwilEvotingUrnengang
        => new Contest
        {
            Id = Guid.Parse(IdUzwilEvoting),
            Date = new DateTime(2020, 3, 2, 0, 0, 0, DateTimeKind.Utc),
            Translations = TranslationUtil.CreateTranslations<ContestTranslation>(
                                (t, o) => t.Description = o,
                                "Urnengang in Uzwi"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            EndOfTestingPhase = new DateTime(2020, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            EVoting = true,
            EVotingFrom = new DateTime(2020, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            EVotingTo = new DateTime(2020, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.TestingPhase,
        };

    public static Contest KirchenUrnengang
        => new Contest
        {
            Id = Guid.Parse(IdKirche),
            Date = new DateTime(2020, 2, 29, 0, 0, 0, DateTimeKind.Utc),
            Translations = TranslationUtil.CreateTranslations<ContestTranslation>(
                                (t, o) => t.Description = o,
                                "Kirchen-Urnengang"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdKirchgemeinde),
            EndOfTestingPhase = new DateTime(2020, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.TestingPhase,
        };

    public static Contest ThurgauNoPoliticalBusinessesUrnengang
        => new Contest
        {
            Id = Guid.Parse(IdThurgauNoPoliticalBusinesses),
            Date = new DateTime(2020, 2, 29, 0, 0, 0, DateTimeKind.Utc),
            Translations = TranslationUtil.CreateTranslations<ContestTranslation>(
                                (t, o) => t.Description = o,
                                "Thurgau-Urnengang"),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdThurgau),
            EndOfTestingPhase = new DateTime(2020, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.TestingPhase,
        };

    public static IEnumerable<Contest> All
    {
        get
        {
            yield return ArchivedBundesurnengang;
            yield return VergangenerBundesurnengang;
            yield return Bundesurnengang;
            yield return StGallenEvotingUrnengang;
            yield return GossauUrnengang;
            yield return UzwilEvotingUrnengang;
            yield return KirchenUrnengang;
            yield return ThurgauNoPoliticalBusinessesUrnengang;
            yield return StGallenStadtUrnengang;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await DomainOfInfluenceMockedData.Seed(runScoped);

        await runScoped(async sp =>
        {
            var contests = All.ToList();

            var db = sp.GetRequiredService<DataContext>();
            db.Contests.AddRange(contests);
            await db.SaveChangesAsync();

            var snapshotBuilder = sp.GetRequiredService<ContestSnapshotBuilder>();
            var cache = sp.GetRequiredService<ContestCache>();
            var asymmetricAlgo = sp.GetRequiredService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();

            cache.Clear();
            foreach (var contest in contests)
            {
                await snapshotBuilder.CreateSnapshotForContest(contest);
                var pastLockedPer = contest.Date.NextUtcDate(true);

                cache.Add(new()
                {
                    Date = contest.Date,
                    PastLockedPer = pastLockedPer,
                    State = contest.State,
                    Id = contest.Id,
                    KeyData = contest.State.IsActiveOrUnlocked()
                        ? new ContestCacheEntryKeyData(asymmetricAlgo.CreateRandomPrivateKey(), contest.Date, pastLockedPer)
                        : null,
                });
            }
        });

        await CantonSettingsMockedData.Seed(runScoped);
        await ContestCountingCircleDetailsMockData.Seed(runScoped);
        await ContestDetailsMockedData.Seed(runScoped);
        await ContestDomainOfInfluenceDetailsMockedData.Seed(runScoped);
    }

    public static bool TestingPhaseEnded(Guid contestId)
    {
        return _contestById[contestId].TestingPhaseEnded;
    }
}
