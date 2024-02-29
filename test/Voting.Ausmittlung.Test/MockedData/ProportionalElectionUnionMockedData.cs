// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ProportionalElectionUnionMockedData
{
    public const string IdBund = "10c37ad2-4d62-4662-8aff-57a07112621c";
    public const string IdBundDifferentTenant = "b0389a7d-77cf-4032-b1b2-0fa2d7cde028";
    public const string IdStGallen1 = "cfdc428a-2a75-4f41-972f-28cfa8a923d5";
    public const string IdStGallen2NoEntries = "6dceaae8-ce5f-4609-8b63-b97b8c323376";
    public const string IdStGallenDifferentTenant = "3d8f3164-c094-4c17-b314-b9d41d037515";
    public const string IdKirche = "4aa33161-4032-496e-997d-fac2b29e14b5";
    public const string IdBundPast = "91f62cc8-fbae-446d-a4b9-0e594594cb92";

    public const string UnionListId1Bund = "9d66a5ed-66c0-4ce6-a63a-63af49432f3f";
    public const string UnionListId2Bund = "67d2553e-3cab-4f22-a350-b4e0ef7f5e9d";
    public const string UnionListId1StGallen1 = "2399ba4e-9bfd-4cf9-80fe-402c0e3a55d2";
    public const string UnionListId2StGallen1 = "8ce79d7d-57fe-4e56-9d57-10696ff3d243";
    public const string UnionListId3StGallen1 = "6e7227d6-9b32-4eab-a1d1-85ef4521f93d";

    public static ProportionalElectionUnion BundUnion
        => new ProportionalElectionUnion
        {
            Id = Guid.Parse(IdBund),
            Description = "Bund Union",
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            ProportionalElectionUnionEntries =
            {
                    new ProportionalElectionUnionEntry
                    {
                        Id = Guid.Parse("5db66de4-4f0a-4c36-9e12-6d5edc785c63"),
                        ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund),
                    },
            },
            ProportionalElectionUnionLists =
            {
                    new ProportionalElectionUnionList
                    {
                        Id = Guid.Parse(UnionListId1Bund),
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionUnionListTranslation>(
                            (t, o) => t.ShortDescription = o,
                            "Liste 1"),
                        ProportionalElectionUnionListEntries =
                        {
                            new ProportionalElectionUnionListEntry
                            {
                                Id = Guid.Parse("2e1d9994-5155-42f2-b6d4-d7fa66d97903"),
                                ProportionalElectionListId = Guid.Parse(ProportionalElectionMockedData.List1IdStGallenProportionalElectionInContestBund),
                            },
                        },
                    },
                    new ProportionalElectionUnionList
                    {
                        Id = Guid.Parse(UnionListId2Bund),
                        OrderNumber = "2a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionUnionListTranslation>(
                            (t, o) => t.ShortDescription = o,
                            "Liste 2"),
                        ProportionalElectionUnionListEntries =
                        {
                            new ProportionalElectionUnionListEntry
                            {
                                Id = Guid.Parse("e3f39410-ec87-481b-96fe-58ff6da9b90a"),
                                ProportionalElectionListId = Guid.Parse(ProportionalElectionMockedData.List2IdStGallenProportionalElectionInContestBund),
                            },
                        },
                    },
            },
        };

    public static ProportionalElectionUnion BundUnionDifferentTenant
        => new ProportionalElectionUnion
        {
            Id = Guid.Parse(IdBundDifferentTenant),
            Description = "Bund Union different Tenant",
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            ProportionalElectionUnionEntries =
            {
                    new ProportionalElectionUnionEntry
                    {
                        Id = Guid.Parse("9c308558-f2c8-4a5a-9077-b2d7a37aa24a"),
                        ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds),
                    },
            },
        };

    public static ProportionalElectionUnion StGallen1
        => new ProportionalElectionUnion
        {
            Id = Guid.Parse(IdStGallen1),
            Description = "St. Gallen 1",
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
            ProportionalElectionUnionEntries =
            {
                    new ProportionalElectionUnionEntry
                    {
                        Id = Guid.Parse("2a8d2a36-147c-4078-92fa-bf451d78fc58"),
                        ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen),
                    },
                    new ProportionalElectionUnionEntry
                    {
                        Id = Guid.Parse("d443959e-2311-4e10-a38b-76e64405d4b4"),
                        ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen),
                    },
            },
            ProportionalElectionUnionLists =
            {
                    new ProportionalElectionUnionList
                    {
                        Id = Guid.Parse(UnionListId1StGallen1),
                        OrderNumber = "1a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionUnionListTranslation>(
                            (t, o) => t.ShortDescription = o,
                            "Liste 1"),
                        ProportionalElectionUnionListEntries =
                        {
                            new ProportionalElectionUnionListEntry
                            {
                                Id = Guid.Parse("100fa95a-7a90-498a-bc8f-639c430a2580"),
                                ProportionalElectionListId = Guid.Parse(ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen),
                            },
                            new ProportionalElectionUnionListEntry
                            {
                                Id = Guid.Parse("09f059f4-18a9-40c5-81c7-4f5cf7f09745"),
                                ProportionalElectionListId = Guid.Parse(ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen),
                            },
                        },
                    },
                    new ProportionalElectionUnionList
                    {
                        Id = Guid.Parse(UnionListId2StGallen1),
                        OrderNumber = "2",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionUnionListTranslation>(
                            (t, o) => t.ShortDescription = o,
                            "Liste 2"),
                        ProportionalElectionUnionListEntries =
                        {
                            new ProportionalElectionUnionListEntry
                            {
                                Id = Guid.Parse("7dfba7b7-8da5-4b20-a035-22fefbf64c56"),
                                ProportionalElectionListId = Guid.Parse(ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen),
                            },
                        },
                    },
                    new ProportionalElectionUnionList
                    {
                        Id = Guid.Parse(UnionListId3StGallen1),
                        OrderNumber = "3a",
                        Translations = TranslationUtil.CreateTranslations<ProportionalElectionUnionListTranslation>(
                            (t, o) => t.ShortDescription = o,
                            "Liste 3"),
                        ProportionalElectionUnionListEntries =
                        {
                            new ProportionalElectionUnionListEntry
                            {
                                Id = Guid.Parse("23879afa-bc28-4be8-aa7b-536df0aaa01a"),
                                ProportionalElectionListId = Guid.Parse(ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen),
                            },
                        },
                    },
            },
        };

    public static ProportionalElectionUnion StGallen2NoEntries
        => new ProportionalElectionUnion
        {
            Id = Guid.Parse(IdStGallen2NoEntries),
            Description = "St. Gallen 2",
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
        };

    public static ProportionalElectionUnion StGallenDifferentTenant
        => new ProportionalElectionUnion
        {
            Id = Guid.Parse(IdStGallenDifferentTenant),
            Description = "St. Gallen different Tenant",
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
        };

    public static ProportionalElectionUnion Kirche
        => new ProportionalElectionUnion
        {
            Id = Guid.Parse(IdKirche),
            Description = "Kirche",
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
        };

    public static ProportionalElectionUnion BundPast
        => new ProportionalElectionUnion
        {
            Id = Guid.Parse(IdBundPast),
            Description = "Bund Union Past",
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
        };

    public static IEnumerable<ProportionalElectionUnion> All
    {
        get
        {
            yield return BundUnion;
            yield return BundUnionDifferentTenant;
            yield return StGallen1;
            yield return StGallen2NoEntries;
            yield return StGallenDifferentTenant;
            yield return Kirche;
            yield return BundPast;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.ProportionalElectionUnions.AddRange(All);
            await db.SaveChangesAsync();
        });
    }
}
