// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Core.Utils.Snapshot;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ZhMockedData
{
    public const string DomainOfInfluenceIdBund = "e786bbc9-9f10-4ed4-86f8-2bd40d77250c";
    public const string DomainOfInfluenceIdKantonZh = "2e9d1987-5800-40cd-80e7-3ee866d3474b";
    public const string DomainOfInfluenceIdDietikon = "84611d92-3cf5-48ce-b536-cf277302b105";
    public const string DomainOfInfluenceIdMeilen = "2b285260-6d4b-43cd-917d-174fe43ddd7e";
    public const string DomainOfInfluenceIdWinterthur = "0711e821-6526-4809-a355-e7da47ab5bc8";

    public const string CountingCircleIdDietikon = "1dc92723-28ba-4c64-a66f-db58f98be8aa";
    public const string CountingCircleIdMeilen = "54c0aa86-bc2a-4630-af17-af7af0c3442c";
    public const string CountingCircleIdWinterthur = "3f63fa44-a0da-4ffc-850d-6c4ce3abde97";

    public const string ContestIdBund = "a5486b9c-c005-4648-b97c-3d00559b2471";

    public const string ProportionalElectionUnionIdKtrat = "ea79e680-1848-43c6-91a3-a3f507010d12";
    public const string ProportionalElectionUnionIdSuperLot = "2402df69-09a3-489a-8bc2-1a809ea2c148";
    public const string ProportionalElectionUnionIdSubLot = "8194c3a3-d891-4bac-936a-e9e09c46f125";

    public static readonly Guid DomainOfInfluenceGuidBund = Guid.Parse(DomainOfInfluenceIdBund);
    public static readonly Guid DomainOfInfluenceGuidKantonZh = Guid.Parse(DomainOfInfluenceIdKantonZh);
    public static readonly Guid DomainOfInfluenceGuidDietikon = Guid.Parse(DomainOfInfluenceIdDietikon);
    public static readonly Guid DomainOfInfluenceGuidMeilen = Guid.Parse(DomainOfInfluenceIdMeilen);
    public static readonly Guid DomainOfInfluenceGuidWinterthur = Guid.Parse(DomainOfInfluenceIdWinterthur);

    public static readonly Guid PartyGuidSvp = Guid.Parse("91b8f013-7cd8-4473-ac71-d6153532cc7a");
    public static readonly Guid PartyGuidSp = Guid.Parse("114ecb52-94fe-4955-9a12-af63b12c651e");
    public static readonly Guid PartyGuidFdp = Guid.Parse("2299f46e-8911-4334-8c40-cf24fd4c45f2");
    public static readonly Guid PartyGuidGlp = Guid.Parse("51a0944f-12ef-413b-973b-98e9316d3501");
    public static readonly Guid PartyGuidGruene = Guid.Parse("9a464b52-aa19-45b5-a1ae-ec051d5816e2");
    public static readonly Guid PartyGuidMitte = Guid.Parse("37e03afb-87da-48b2-953b-66a343b4bb07");
    public static readonly Guid PartyGuidEvp = Guid.Parse("9b755883-b92c-47ff-afef-b0bf45b8a1e2");
    public static readonly Guid PartyGuidAl = Guid.Parse("839f6a7c-bfd9-455b-a866-ba6455f2694c");
    public static readonly Guid PartyGuidEdu = Guid.Parse("4800749b-1a03-46ec-9ca4-e9c042c9540b");
    public static readonly Guid PartyGuidAufl = Guid.Parse("4e6db190-7636-4e8d-addc-a08ea64c6f7e");
    public static readonly Guid PartyGuidPda = Guid.Parse("b6f3c5f0-6171-42ff-a129-4e08051ae6f1");
    public static readonly Guid PartyGuidStopp = Guid.Parse("07fbe67a-74ce-4ede-bf07-415e9b41beef");
    public static readonly Guid PartyGuidSapapo = Guid.Parse("a577448f-14cb-470e-a090-0cfe9972b984");

    public static readonly Guid CountingCircleGuidDietikon = Guid.Parse(CountingCircleIdDietikon);
    public static readonly Guid CountingCircleGuidMeilen = Guid.Parse(CountingCircleIdMeilen);
    public static readonly Guid CountingCircleGuidWinterthur = Guid.Parse(CountingCircleIdWinterthur);

    public static readonly Guid ContestGuidBund = Guid.Parse(ContestIdBund);

    public static readonly Guid ProportionalElectionGuidKtratDietikon = Guid.Parse("a72a6e07-6560-4247-8987-bac9ca458bc3");
    public static readonly Guid ProportionalElectionGuidKtratMeilen = Guid.Parse("c3c7b0c5-5562-48b1-97e9-694151a93d68");
    public static readonly Guid ProportionalElectionGuidKtratWinterthur = Guid.Parse("69395ab1-de75-4165-b383-13b6cbc53e96");

    public static readonly Guid ProportionalElectionGuidSuperLotDietikon = Guid.Parse("a216ef4b-639a-4026-ae70-61f0ef7ca530");
    public static readonly Guid ProportionalElectionGuidSuperLotMeilen = Guid.Parse("e4f5c712-991f-409b-99ce-6f1455c54047");
    public static readonly Guid ProportionalElectionGuidSuperLotWinterthur = Guid.Parse("e52320fa-aa58-421e-a077-c2def68d0427");

    public static readonly Guid ProportionalElectionGuidSubLotDietikon = Guid.Parse("55fadd45-f426-4626-9c38-fafbfdd8ffe1");
    public static readonly Guid ProportionalElectionGuidSubLotMeilen = Guid.Parse("e24ea995-7d39-496c-8e8b-406a87b09e78");
    public static readonly Guid ProportionalElectionGuidSubLotWinterthur = Guid.Parse("dd6d8a1b-8d48-49e1-b950-d0db1727096b");

    public static readonly Guid ProportionalElectionUnionGuidKtrat = Guid.Parse(ProportionalElectionUnionIdKtrat);
    public static readonly Guid ProportionalElectionUnionGuidSuperLot = Guid.Parse(ProportionalElectionUnionIdSuperLot);
    public static readonly Guid ProportionalElectionUnionGuidSubLot = Guid.Parse(ProportionalElectionUnionIdSubLot);

    public static readonly Guid ProportionalElectionGuidSingleDoiSuperLot = Guid.Parse("446708c6-9ee2-4034-88c0-a7dbb026c60b");

    public static readonly Dictionary<Guid, Dictionary<Guid, List<(Guid, string, int)>>> ListsByElectionIdByDpResultOwnerId = new()
    {
        {
            ProportionalElectionUnionGuidKtrat,
            new()
            {
                {
                    ProportionalElectionGuidKtratDietikon,
                    new()
                    {
                        (Guid.Parse("595b7588-2af3-4686-b4d4-f97a64679103"), "SVP", 47_079),
                        (Guid.Parse("ad4ddd39-ba6d-4a31-8a36-1f6fdd6452e6"), "SP", 24_473),
                        (Guid.Parse("c90826ef-759c-4c8c-af99-de77a28214a7"), "FDP", 27_419),
                    }
                },
                {
                    ProportionalElectionGuidKtratMeilen,
                    new()
                    {
                        (Guid.Parse("7f7808f7-79b5-4b3e-a434-eb7344131ce2"), "SVP", 89_540),
                        (Guid.Parse("89dfd5be-ad83-4e1e-b575-ded6ce513305"), "SP", 44_480),
                        (Guid.Parse("e3598805-c52c-4ff4-9049-2c5647972e8c"), "FDP", 79_917),
                        (Guid.Parse("2cd8651a-33d2-451c-8a70-1eaaa47deb92"), "SaPaPo", 1_724),
                    }
                },
                {
                    ProportionalElectionGuidKtratWinterthur,
                    new()
                    {
                        (Guid.Parse("64672e72-1502-4829-a05f-b96821364552"), "SVP", 55_855),
                        (Guid.Parse("c9a3491c-0774-4952-a487-e3087a1bd955"), "SP", 81_675),
                        (Guid.Parse("f31e9c04-eea8-41b5-b560-f34f97c01c26"), "FDP", 39_210),
                    }
                },
            }
        },
        {
            ProportionalElectionUnionGuidSuperLot,
            new()
            {
                {
                    ProportionalElectionGuidSuperLotDietikon,
                    new()
                    {
                        (Guid.Parse("007b391d-166b-44c7-a4c2-76ef1c587099"), "SVP", 3_000),
                        (Guid.Parse("89483005-5c89-469b-8449-326c9ecfab72"), "SP", 400),
                        (Guid.Parse("f4c8a921-ec24-4db6-850a-f551691121e8"), "FDP", 0),
                    }
                },
                {
                    ProportionalElectionGuidSuperLotMeilen,
                    new()
                    {
                        (Guid.Parse("8f06f2ea-c545-4f25-b255-245a92238be0"), "SVP", 2_000),
                        (Guid.Parse("4210a835-fc52-4078-94aa-9de41093e2b8"), "SP", 100),
                        (Guid.Parse("83d9f40a-30c4-49a2-87ac-0dc8d48f5f2a"), "FDP", 1_000),
                    }
                },
                {
                    ProportionalElectionGuidSuperLotWinterthur,
                    new()
                    {
                        (Guid.Parse("59e9278a-76bc-4c4c-912e-f8f0f59704a3"), "SVP", 1_000),
                        (Guid.Parse("49ca17ce-9558-43d9-ae68-abf682a2fcbf"), "SP", 250),
                        (Guid.Parse("89edc341-317f-4bb8-9efe-3c24ac32fbd2"), "FDP", 500),
                    }
                },
            }
        },
        {
            ProportionalElectionUnionGuidSubLot,
            new()
            {
                {
                    ProportionalElectionGuidSubLotDietikon,
                    new()
                    {
                        (Guid.Parse("e6ec6731-3b7f-42bd-8b63-2a2360c6f377"), "SVP", 20_000),
                        (Guid.Parse("10cac6e2-361a-46ff-a812-3ea973852f10"), "SP", 10_000),
                        (Guid.Parse("221db64d-3749-4067-aa74-a57bd09db711"), "FDP", 20_000),
                    }
                },
                {
                    ProportionalElectionGuidSubLotMeilen,
                    new()
                    {
                        (Guid.Parse("c84f1d13-24e0-4d3b-94cd-d6322353a59f"), "SVP", 15_560),
                        (Guid.Parse("4ca79f3c-992c-4996-96ce-0a1ae95440a1"), "SP", 7_780),
                        (Guid.Parse("ab2522d1-a42b-4968-a527-3113dbde6cba"), "FDP", 10_000),
                    }
                },
                {
                    ProportionalElectionGuidSubLotWinterthur,
                    new()
                    {
                        (Guid.Parse("ba90a7ac-bb78-49a3-a99b-21be502c4894"), "SVP", 15_560),
                        (Guid.Parse("4eb8d15e-f773-4105-94ed-564a76d53115"), "SP", 7_780),
                        (Guid.Parse("0fceb08d-b834-4343-964a-d016bfccd84f"), "FDP", 10_000),
                    }
                },
            }
        },
        {
            ProportionalElectionGuidSingleDoiSuperLot,
            new()
            {
                {
                    ProportionalElectionGuidSingleDoiSuperLot,
                    new()
                    {
                        (Guid.Parse("3b083853-cd08-4b46-b81b-da849a979bfc"), "SVP", 2_250),
                        (Guid.Parse("868cd951-5faf-4799-81ad-1ebd65874e52"), "SP", 2_250),
                        (Guid.Parse("a3e87eb2-d4c3-469e-82e9-d0605942c04f"), "FDP", 500),
                        (Guid.Parse("45653f1a-ec71-48f7-8fde-f8c2772f0fa6"), "GLP", 20_000),
                    }
                },
            }
        },
    };

    public static IEnumerable<Guid> AllUnionIds()
    {
        yield return ProportionalElectionUnionGuidKtrat;
        yield return ProportionalElectionUnionGuidSuperLot;
        yield return ProportionalElectionUnionGuidSubLot;
    }

    public static IEnumerable<Guid> AllElectionIdsWithoutUnion()
    {
        yield return ProportionalElectionGuidSingleDoiSuperLot;
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped, bool seedDpResult = false)
    {
        // Creates a ZH UnionEndResult in the state, where all elections have finished the counting.
        await SeedBasisData(runScoped);
        await SeedEndResults(runScoped);

        if (seedDpResult)
        {
            await SeedDpResults(runScoped);
        }
    }

    public static async Task SeedBasisData(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        var ccs = new List<CountingCircle>
        {
           GenerateCountingCircle(CountingCircleGuidDietikon, "Dietikon", "101", SecureConnectTestDefaults.MockedTenantBund.Id),
           GenerateCountingCircle(CountingCircleGuidMeilen, "Meilen", "102", SecureConnectTestDefaults.MockedTenantBund.Id),
           GenerateCountingCircle(CountingCircleGuidWinterthur, "Winterthur", "103", SecureConnectTestDefaults.MockedTenantBund.Id),
        };

        var dois = new List<DomainOfInfluence>
        {
            GenerateDomainOfInfluence(DomainOfInfluenceGuidBund, "Bund", "0001", DomainOfInfluenceType.Ch, SecureConnectTestDefaults.MockedTenantBund.Id, action:
                doi =>
                {
                    doi.Parties = new List<DomainOfInfluenceParty>()
                    {
                        new()
                        {
                            Id = PartyGuidSvp,
                            BaseDomainOfInfluencePartyId = PartyGuidSvp,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "SVP",
                                (t, o) => t.ShortDescription = o,
                                "SVP"),
                        },
                        new()
                        {
                            Id = PartyGuidSp,
                            BaseDomainOfInfluencePartyId = PartyGuidSp,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "SP",
                                (t, o) => t.ShortDescription = o,
                                "SP"),
                        },
                        new()
                        {
                            Id = PartyGuidFdp,
                            BaseDomainOfInfluencePartyId = PartyGuidFdp,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "FDP",
                                (t, o) => t.ShortDescription = o,
                                "FDP"),
                        },
                        new()
                        {
                            Id = PartyGuidGlp,
                            BaseDomainOfInfluencePartyId = PartyGuidGlp,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "GLP",
                                (t, o) => t.ShortDescription = o,
                                "GLP"),
                        },
                        new()
                        {
                            Id = PartyGuidGruene,
                            BaseDomainOfInfluencePartyId = PartyGuidGruene,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "GRÜNE",
                                (t, o) => t.ShortDescription = o,
                                "GRÜNE"),
                        },
                        new()
                        {
                            Id = PartyGuidMitte,
                            BaseDomainOfInfluencePartyId = PartyGuidMitte,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "Die Mitte",
                                (t, o) => t.ShortDescription = o,
                                "Die Mitte"),
                        },
                        new()
                        {
                            Id = PartyGuidEvp,
                            BaseDomainOfInfluencePartyId = PartyGuidEvp,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "EVP",
                                (t, o) => t.ShortDescription = o,
                                "EVP"),
                        },
                        new()
                        {
                            Id = PartyGuidAl,
                            BaseDomainOfInfluencePartyId = PartyGuidAl,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "AL",
                                (t, o) => t.ShortDescription = o,
                                "AL"),
                        },
                        new()
                        {
                            Id = PartyGuidEdu,
                            BaseDomainOfInfluencePartyId = PartyGuidEdu,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "EDU",
                                (t, o) => t.ShortDescription = o,
                                "EDU"),
                        },
                        new()
                        {
                            Id = PartyGuidAufl,
                            BaseDomainOfInfluencePartyId = PartyGuidAufl,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "AuFL",
                                (t, o) => t.ShortDescription = o,
                                "AuFL"),
                        },
                        new()
                        {
                            Id = PartyGuidPda,
                            BaseDomainOfInfluencePartyId = PartyGuidPda,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "PdA",
                                (t, o) => t.ShortDescription = o,
                                "PdA"),
                        },
                        new()
                        {
                            Id = PartyGuidStopp,
                            BaseDomainOfInfluencePartyId = PartyGuidStopp,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "Stopp",
                                (t, o) => t.ShortDescription = o,
                                "Stopp"),
                        },
                        new()
                        {
                            Id = PartyGuidSapapo,
                            BaseDomainOfInfluencePartyId = PartyGuidSapapo,
                            Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                                (t, o) => t.Name = o,
                                "SaPaPo",
                                (t, o) => t.ShortDescription = o,
                                "SaPaPo"),
                        },
                    };
                    doi.CountingCircles = new List<DomainOfInfluenceCountingCircle>
                    {
                        new()
                        {
                            Id = Guid.Parse("3b672264-80b7-4ced-ade2-cdd3c86be455"),
                            CountingCircleId = CountingCircleGuidDietikon,
                            ComparisonCountOfVotersCategory = ComparisonCountOfVotersCategory.A,
                            Inherited = true,
                        },
                        new()
                        {
                            Id = Guid.Parse("722b5f31-3940-4f16-99fe-e292bb861de7"),
                            CountingCircleId = CountingCircleGuidMeilen,
                            ComparisonCountOfVotersCategory = ComparisonCountOfVotersCategory.B,
                            Inherited = true,
                        },
                        new()
                        {
                            Id = Guid.Parse("bd081d98-b909-407b-8850-67c6c1e768df"),
                            CountingCircleId = CountingCircleGuidWinterthur,
                            ComparisonCountOfVotersCategory = ComparisonCountOfVotersCategory.C,
                            Inherited = true,
                        },
                    };
                }),
            GenerateDomainOfInfluence(DomainOfInfluenceGuidKantonZh, "Kanton Zürich", "0010", DomainOfInfluenceType.Ct, SecureConnectTestDefaults.MockedTenantBund.Id, parentId: DomainOfInfluenceGuidBund, action:
                doi =>
                {
                    doi.CountingCircles = new List<DomainOfInfluenceCountingCircle>
                    {
                        new()
                        {
                            Id = Guid.Parse("b7f0313f-ab94-41ea-b5f2-a18258115693"),
                            CountingCircleId = CountingCircleGuidDietikon,
                            ComparisonCountOfVotersCategory = ComparisonCountOfVotersCategory.A,
                            Inherited = true,
                        },
                        new()
                        {
                            Id = Guid.Parse("6b5551e9-b144-439d-b74e-bdc892109232"),
                            CountingCircleId = CountingCircleGuidMeilen,
                            ComparisonCountOfVotersCategory = ComparisonCountOfVotersCategory.B,
                            Inherited = true,
                        },
                        new()
                        {
                            Id = Guid.Parse("e5f72f04-0315-4df4-bb21-9d1198f006e1"),
                            CountingCircleId = CountingCircleGuidWinterthur,
                            ComparisonCountOfVotersCategory = ComparisonCountOfVotersCategory.C,
                            Inherited = true,
                        },
                    };
                }),
            GenerateDomainOfInfluence(DomainOfInfluenceGuidDietikon, "Bezirk Dietikon", "101", DomainOfInfluenceType.Bz, SecureConnectTestDefaults.MockedTenantBund.Id, CountingCircleGuidDietikon, parentId: DomainOfInfluenceGuidKantonZh),
            GenerateDomainOfInfluence(DomainOfInfluenceGuidMeilen, "Bezirk Meilen", "102", DomainOfInfluenceType.Bz, SecureConnectTestDefaults.MockedTenantBund.Id, CountingCircleGuidMeilen, parentId: DomainOfInfluenceGuidKantonZh),
            GenerateDomainOfInfluence(DomainOfInfluenceGuidWinterthur, "Bezirk Winterthur", "103", DomainOfInfluenceType.Bz, SecureConnectTestDefaults.MockedTenantBund.Id, CountingCircleGuidWinterthur, parentId: DomainOfInfluenceGuidKantonZh),
        };

        var contest = new Contest
        {
            Id = ContestGuidBund,
            Date = new DateTime(2029, 2, 12, 0, 0, 0, DateTimeKind.Utc),
            Translations = TranslationUtil.CreateTranslations<ContestTranslation>(
                (t, o) => t.Description = o,
                "Urnengang"),
            DomainOfInfluenceId = DomainOfInfluenceGuidBund,
            EndOfTestingPhase = new DateTime(2028, 12, 20, 0, 0, 0, DateTimeKind.Utc),
            State = ContestState.TestingPhase,
            CantonDefaults = new ContestCantonDefaults
            {
                NewZhFeaturesEnabled = true,
            },
        };

        var proportionalElections = GenerateProportionalElections();

        var proportionalElectionUnions = GenerateProportionalElectionUnions();

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.CountingCircles.AddRange(ccs);
            db.DomainOfInfluences.AddRange(dois);
            db.Contests.Add(contest);
            await db.SaveChangesAsync();
        });

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            contest = await db.Contests
                .AsSplitQuery()
                .Include(x => x.CantonDefaults)
                .SingleAsync(x => x.Id == contest.Id);

            // contest
            await sp.GetRequiredService<ContestSnapshotBuilder>().CreateSnapshotForContest(contest);
            await sp.GetRequiredService<ContestCountingCircleDetailsBuilder>().SyncAndResetEVoting(contest);
            await sp.GetRequiredService<DomainOfInfluencePermissionBuilder>().RebuildPermissionTree();

            // contest cache
            var cache = sp.GetRequiredService<ContestCache>();
            cache.Clear();
            cache.Add(new()
            {
                Date = contest.Date,
                PastLockedPer = contest.Date.NextUtcDate(true),
                State = contest.State,
                Id = contest.Id,
            });

            foreach (var election in proportionalElections)
            {
                election.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(election.ContestId, election.DomainOfInfluenceId);
            }

            db.ProportionalElections.AddRange(proportionalElections);
            db.ProportionalElectionUnions.AddRange(proportionalElectionUnions);
            await db.SaveChangesAsync();

            // election results, end results and simple pb
            var simplePbBuilder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<ProportionalElection>>();
            var resultBuilder = sp.GetRequiredService<ProportionalElectionResultBuilder>();
            var endResultInitializer = sp.GetRequiredService<ProportionalElectionEndResultInitializer>();
            var unionEndResultInitializer = sp.GetRequiredService<ProportionalElectionUnionEndResultInitializer>();
            var unionListBuilder = sp.GetRequiredService<ProportionalElectionUnionListBuilder>();

            foreach (var election in proportionalElections)
            {
                await simplePbBuilder.Create(election);
                await resultBuilder.RebuildForElection(election.Id, election.DomainOfInfluenceId, false);
                await endResultInitializer.RebuildForElection(election.Id, false);
            }

            foreach (var proportionalElectionUnion in proportionalElectionUnions)
            {
                await unionListBuilder.RebuildLists(proportionalElectionUnion.Id, proportionalElectionUnion.ProportionalElectionUnionEntries.Select(x => x.ProportionalElectionId).ToList());
                await unionEndResultInitializer.RebuildForUnion(proportionalElectionUnion.Id, false);
            }
        });
    }

    public static async Task SeedEndResults(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var allUnionIds = AllUnionIds().ToList();
            var allElectionIdsWithoutUnion = AllElectionIdsWithoutUnion().ToList();
            var db = sp.GetRequiredService<DataContext>();
            var endResults = await db.ProportionalElectionEndResult
                .AsSplitQuery()
                .AsTracking()
                .Include(x => x.ProportionalElection.ProportionalElectionUnionEntries)
                .Include(x => x.VotingCards)
                .Include(x => x.ListEndResults)
                .ThenInclude(x => x.List)
                .Where(x => x.ProportionalElection.ProportionalElectionUnionEntries.Any(y => allUnionIds.Contains(y.ProportionalElectionUnionId))
                    || allElectionIdsWithoutUnion.Contains(x.ProportionalElectionId))
                .ToListAsync();

            foreach (var endResult in endResults)
            {
                var listData = ListsByElectionIdByDpResultOwnerId.Values.Single(v => v.ContainsKey(endResult.ProportionalElectionId))[endResult.ProportionalElectionId];
                var listVoteCountSum = listData.Sum(e => e.Item3);

                endResult.CountOfDoneCountingCircles = 1;
                endResult.TotalCountOfVoters = (int)(listVoteCountSum * 0.5);
                endResult.CountOfVoters.ConventionalReceivedBallots = (int)(listVoteCountSum * 0.25);
                endResult.CountOfVoters.ConventionalAccountedBallots = (int)(listVoteCountSum * 0.2);
                endResult.CountOfVoters.ConventionalBlankBallots = (int)(listVoteCountSum * 0.025);
                endResult.CountOfVoters.ConventionalInvalidBallots = endResult.CountOfVoters.ConventionalReceivedBallots - endResult.CountOfVoters.ConventionalAccountedBallots - endResult.CountOfVoters.ConventionalBlankBallots;
                endResult.CountOfVoters.UpdateVoterParticipation(endResult.TotalCountOfVoters);

                foreach (var listEndResult in endResult.ListEndResults)
                {
                    var listDataEntry = listData[listEndResult.List.Position - 1];
                    listEndResult.ConventionalSubTotal.UnmodifiedListVotesCount = listDataEntry.Item3;
                }
            }

            var unionEndResults = await db.ProportionalElectionUnionEndResults
                .AsTracking()
                .Where(x => allUnionIds.Contains(x.ProportionalElectionUnionId))
                .ToListAsync();

            foreach (var unionEndResult in unionEndResults)
            {
                var relEndResults = endResults.Where(e => e.ProportionalElection.ProportionalElectionUnionEntries
                    .Any(x => x.ProportionalElectionUnionId == unionEndResult.ProportionalElectionUnionId))
                    .ToList();

                unionEndResult.CountOfDoneElections = relEndResults.Count;
                unionEndResult.TotalCountOfElections = relEndResults.Count;
            }

            await db.SaveChangesAsync();
        });
    }

    public static async Task SeedDpResults(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var dpResultBuilder = sp.GetRequiredService<DoubleProportionalResultBuilder>();
            await dpResultBuilder.BuildForUnion(ProportionalElectionUnionGuidKtrat);
        });
    }

    private static CountingCircle GenerateCountingCircle(Guid id, string name, string bfs, string secureConnectId)
    {
        return new CountingCircle
        {
            Id = id,
            BasisCountingCircleId = id,
            Name = name,
            NameForProtocol = name,
            Bfs = bfs,
            ResponsibleAuthority = new Authority
            {
                Name = "Abraxas",
                Email = "sg@abraxas.ch",
                Phone = "071 123 12 20",
                Street = "WerkstrasseX",
                City = "MyCityX",
                Zip = "9000",
                SecureConnectId = secureConnectId,
            },
        };
    }

    private static DomainOfInfluence GenerateDomainOfInfluence(
        Guid id,
        string name,
        string bfs,
        DomainOfInfluenceType type,
        string secureConnectId,
        Guid? countingCircleId = null,
        Guid? parentId = null,
        Action<DomainOfInfluence>? action = null)
    {
        var doi = new DomainOfInfluence
        {
            Id = id,
            BasisDomainOfInfluenceId = id,
            Name = name,
            NameForProtocol = name,
            ShortName = name,
            SecureConnectId = secureConnectId,
            Type = type,
            ParentId = parentId,
            Canton = DomainOfInfluenceCanton.Zh,
            Bfs = bfs,
        };

        if (countingCircleId.HasValue)
        {
            doi.CountingCircles = new List<DomainOfInfluenceCountingCircle>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    CountingCircleId = countingCircleId.Value!,
                },
            };
        }

        action?.Invoke(doi);
        return doi;
    }

    private static List<ProportionalElectionUnion> GenerateProportionalElectionUnions()
    {
        return new()
        {
            new ProportionalElectionUnion
            {
                Id = ProportionalElectionUnionGuidKtrat,
                ContestId = ContestGuidBund,
                SecureConnectId = SecureConnectTestDefaults.MockedTenantBund.Id,
                Description = "Kantonratswahl 2023",
                ProportionalElectionUnionEntries = new List<ProportionalElectionUnionEntry>
                {
                    new() { ProportionalElectionId = ProportionalElectionGuidKtratDietikon },
                    new() { ProportionalElectionId = ProportionalElectionGuidKtratMeilen },
                    new() { ProportionalElectionId = ProportionalElectionGuidKtratWinterthur },
                },
            },
            new ProportionalElectionUnion()
            {
                Id = ProportionalElectionUnionGuidSuperLot,
                ContestId = ContestGuidBund,
                SecureConnectId = SecureConnectTestDefaults.MockedTenantBund.Id,
                Description = "Oberzuteilung mit Losentscheid",
                ProportionalElectionUnionEntries = new List<ProportionalElectionUnionEntry>
                {
                    new() { ProportionalElectionId = ProportionalElectionGuidSuperLotDietikon },
                    new() { ProportionalElectionId = ProportionalElectionGuidSuperLotMeilen },
                    new() { ProportionalElectionId = ProportionalElectionGuidSuperLotWinterthur },
                },
            },
            new ProportionalElectionUnion()
            {
                Id = ProportionalElectionUnionGuidSubLot,
                ContestId = ContestGuidBund,
                SecureConnectId = SecureConnectTestDefaults.MockedTenantBund.Id,
                Description = "Unterzuteilung mit Losentscheid",
                ProportionalElectionUnionEntries = new List<ProportionalElectionUnionEntry>
                {
                    new() { ProportionalElectionId = ProportionalElectionGuidSubLotDietikon },
                    new() { ProportionalElectionId = ProportionalElectionGuidSubLotMeilen },
                    new() { ProportionalElectionId = ProportionalElectionGuidSubLotWinterthur },
                },
            },
        };
    }

    private static ProportionalElection GenerateProportionalElection(
        Guid id,
        Guid doiId,
        ProportionalElectionMandateAlgorithm mandateAlgo,
        int numberOfMandates,
        string pbNumber,
        string description)
    {
        return new ProportionalElection
        {
            Id = id,
            DomainOfInfluenceId = doiId,
            ContestId = ContestGuidBund,
            MandateAlgorithm = mandateAlgo,
            NumberOfMandates = numberOfMandates,
            PoliticalBusinessNumber = pbNumber,
            Active = true,
            BallotBundleSize = 5,
            Translations = TranslationUtil.CreateTranslations<ProportionalElectionTranslation>(
                (t, o) => t.OfficialDescription = o,
                description,
                (t, s) => t.ShortDescription = s,
                description),
            ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            ProportionalElectionLists = GenerateLists(id, numberOfMandates),
        };
    }

    private static List<ProportionalElectionList> GenerateLists(Guid electionId, int numberOfMandates)
    {
        return ListsByElectionIdByDpResultOwnerId.Values.Single(e => e.ContainsKey(electionId))[electionId]
            .Select((list, i) => new ProportionalElectionList
            {
                Id = list.Item1,
                Translations = TranslationUtil.CreateTranslations<ProportionalElectionListTranslation>(
                    (t, o) => t.Description = o,
                    $"Liste {(i + 1).ToString("00", CultureInfo.InvariantCulture)} ({list.Item1})",
                    (t, s) => t.ShortDescription = s,
                    list.Item2),
                ProportionalElectionCandidates = Enumerable.Range(1, numberOfMandates)
                    .Select(j => new ProportionalElectionCandidate
                    {
                        FirstName = $"FN {i + 1}.{j}",
                        LastName = $"LN {i + 1}.{j}",
                    }).ToList(),
                Position = i + 1,
                OrderNumber = (i + 1).ToString("00", CultureInfo.InvariantCulture),
            })
            .ToList();
    }

    private static List<ProportionalElection> GenerateProportionalElections()
    {
        var electionKtratDietikon = GenerateProportionalElection(
            ProportionalElectionGuidKtratDietikon,
            DomainOfInfluenceGuidDietikon,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            11,
            "KtRat007",
            "Kantonratswahl");

        var electionKtratMeilen = GenerateProportionalElection(
            ProportionalElectionGuidKtratMeilen,
            DomainOfInfluenceGuidMeilen,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            12,
            "KtRat010",
            "Kantonratswahl");

        var electionKtratWinterthur = GenerateProportionalElection(
            ProportionalElectionGuidKtratWinterthur,
            DomainOfInfluenceGuidWinterthur,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            13,
            "KtRat014",
            "Kantonratswahl");

        var electionSuperLotDietikon = GenerateProportionalElection(
            ProportionalElectionGuidSuperLotDietikon,
            DomainOfInfluenceGuidDietikon,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            2,
            "SuperLot001",
            "SuperLot001");

        var electionSuperLotMeilen = GenerateProportionalElection(
            ProportionalElectionGuidSuperLotMeilen,
            DomainOfInfluenceGuidMeilen,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            2,
            "SuperLot002",
            "SuperLot002");

        var electionSuperLotWinterthur = GenerateProportionalElection(
            ProportionalElectionGuidSuperLotWinterthur,
            DomainOfInfluenceGuidWinterthur,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            1,
            "SuperLot003",
            "SuperLot003");

        var electionSubLotDietikon = GenerateProportionalElection(
            ProportionalElectionGuidSubLotDietikon,
            DomainOfInfluenceGuidDietikon,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            20,
            "SubLot001",
            "SubLot001");

        var electionSubLotMeilen = GenerateProportionalElection(
            ProportionalElectionGuidSubLotMeilen,
            DomainOfInfluenceGuidMeilen,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            20,
            "SubLot002",
            "SubLot002");

        var electionSubLotWinterthur = GenerateProportionalElection(
            ProportionalElectionGuidSubLotWinterthur,
            DomainOfInfluenceGuidWinterthur,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
            20,
            "SubLot003",
            "SubLot003");

        var electionSingleDoiSuperLot = GenerateProportionalElection(
            ProportionalElectionGuidSingleDoiSuperLot,
            DomainOfInfluenceGuidMeilen,
            ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum,
            5,
            "SingleDoiSuperLot",
            "SingleDoiSuperLot");

        return new List<ProportionalElection>
        {
            electionKtratDietikon,
            electionKtratMeilen,
            electionKtratWinterthur,
            electionSuperLotDietikon,
            electionSuperLotMeilen,
            electionSuperLotWinterthur,
            electionSubLotDietikon,
            electionSubLotMeilen,
            electionSubLotWinterthur,
            electionSingleDoiSuperLot,
        };
    }
}
