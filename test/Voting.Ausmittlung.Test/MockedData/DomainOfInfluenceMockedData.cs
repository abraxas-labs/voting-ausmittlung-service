// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1.Data;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.MockedData;

public static class DomainOfInfluenceMockedData
{
    public const string IdBund = "eae2cfaf-c787-48b9-a108-c975b0a580da";
    public const string IdStGallen = "0e9ae002-dfa1-4eb6-920b-5d837a33ff79";
    public const string IdStGallenStadt = "87718A25-11E2-4AB1-B3A0-112BAA11676E";
    public const string IdGossau = "a18b1c6a-6b66-43e4-a7d2-a440a7025dbd";
    public const string IdUzwil = "d9f5e161-37f8-4c76-921d-c0237c54dc5b";
    public const string IdKirchgemeinde = "22949a1e-fb09-47ec-9e9f-d26675676565";
    public const string IdThurgau = "6bb6e372-ffa2-45fd-9329-6c6a0bc8a752";
    public const string IdGenf = "ade30989-5e28-40c7-918c-2c9408f853ba";

    public const string PartyIdBundAndere = "c952f414-a81a-4cb0-acd6-2b5c2c422009";
    public const string PartyIdBundSvp = "f124e0ed-118d-4cfd-9663-c6b12a3b94bd";
    public const string PartyIdBundFdp = "0e46965b-d04b-48a4-87dd-cda8f3d04de2";
    public const string PartyIdBundCvp = "9ec80205-08f6-4468-9055-19b1e7266df1";
    public const string PartyIdBundSp = "dc80249c-9c63-4169-b2a8-c5bb1f0f458b";
    public const string PartyIdGossauFLiG = "1fcc8aee-497e-4598-b725-da04bad771db";

    public static DomainOfInfluence Bund
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdBund),
            BasisDomainOfInfluenceId = Guid.Parse(IdBund),
            Name = "Bund",
            NameForProtocol = "Bund",
            ShortName = "Bund",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantBund.Id,
            Type = DomainOfInfluenceType.Ch,
            Canton = DomainOfInfluenceCanton.Sg,
            Bfs = "0001",
            Code = "0001-BUND",
            SortNumber = 99999,
            Parent = null,
            ContactPerson = new ContactPerson()
            {
                FirstName = "Firstname contact",
                FamilyName = "Lastname contact",
                Email = "email@example.com",
                Phone = "0799999999",
            },
            PlausibilisationConfiguration = BuildDataPlausibilisationConfiguration(x =>
            {
                x.ComparisonVoterParticipationConfigurations = new List<ComparisonVoterParticipationConfiguration>
                {
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Ch, ComparisonLevel = DomainOfInfluenceType.Ch, ThresholdPercent = 2 },
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Ct, ComparisonLevel = DomainOfInfluenceType.Ch, ThresholdPercent = 3 },
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Ct, ComparisonLevel = DomainOfInfluenceType.Ct, ThresholdPercent = 4 },
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Sk, ComparisonLevel = DomainOfInfluenceType.Ch, ThresholdPercent = 5 },
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Sk, ComparisonLevel = DomainOfInfluenceType.Ct, ThresholdPercent = 6 },
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Sk, ComparisonLevel = DomainOfInfluenceType.Sk, ThresholdPercent = 7 },
                };
            }),
            Parties = new List<DomainOfInfluenceParty>()
            {
                new()
                {
                    Id = Guid.Parse(PartyIdBundAndere),
                    BaseDomainOfInfluencePartyId = Guid.Parse(PartyIdBundAndere),
                    Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                        (t, o) => t.Name = o,
                        "Andere",
                        (t, o) => t.ShortDescription = o,
                        "AN"),
                },
                new()
                {
                    Id = Guid.Parse(PartyIdBundSvp),
                    BaseDomainOfInfluencePartyId = Guid.Parse(PartyIdBundSvp),
                    Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                        (t, o) => t.Name = o,
                        "Schweizerische Volkspartei",
                        (t, o) => t.ShortDescription = o,
                        "SVP"),
                },
                new()
                {
                    Id = Guid.Parse(PartyIdBundFdp),
                    BaseDomainOfInfluencePartyId = Guid.Parse(PartyIdBundFdp),
                    Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                        (t, o) => t.Name = o,
                        "FDP.Die Lieberalen",
                        (t, o) => t.ShortDescription = o,
                        "FDP"),
                },
                new()
                {
                    Id = Guid.Parse(PartyIdBundCvp),
                    BaseDomainOfInfluencePartyId = Guid.Parse(PartyIdBundCvp),
                    Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                        (t, o) => t.Name = o,
                        "Christliche Volkspartei",
                        (t, o) => t.ShortDescription = o,
                        "CVP"),
                },
                new()
                {
                    Id = Guid.Parse(PartyIdBundSp),
                    BaseDomainOfInfluencePartyId = Guid.Parse(PartyIdBundSp),
                    Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                        (t, o) => t.Name = o,
                        "Sozialdemokratische Partei der Schweiz",
                        (t, o) => t.ShortDescription = o,
                        "SP"),
                },
            },
        };

    public static DomainOfInfluence StGallen
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdStGallen),
            BasisDomainOfInfluenceId = Guid.Parse(IdStGallen),
            Name = "St. Gallen",
            NameForProtocol = "Kanton St. Gallen",
            ShortName = "St. Gallen",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
            Type = DomainOfInfluenceType.Ct,
            ParentId = Guid.Parse(IdBund),
            Canton = DomainOfInfluenceCanton.Sg,
            Bfs = "0002",
            Code = "0002-SG",
            SortNumber = 2,
            ContactPerson = new ContactPerson
            {
                Email = "hans@sg.ch",
                Phone = "071 123 12 12",
                FamilyName = "Muster",
                FirstName = "Hans",
                MobilePhone = "079 123 12 12",
            },
            PlausibilisationConfiguration = BuildDataPlausibilisationConfiguration(x =>
            {
                x.ComparisonVoterParticipationConfigurations.Add(new() { MainLevel = DomainOfInfluenceType.An, ComparisonLevel = DomainOfInfluenceType.An, ThresholdPercent = 5 });
                x.ComparisonCountOfVotersConfigurations.First().ThresholdPercent = 8;
                x.ComparisonVotingChannelConfigurations.First().ThresholdPercent = 8;
            }),
        };

    public static DomainOfInfluence StGallenStadt
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdStGallenStadt),
            BasisDomainOfInfluenceId = Guid.Parse(IdStGallenStadt),
            Name = "St. Gallen (Stadt)",
            NameForProtocol = "Stadt St. Gallen",
            ShortName = "St. Gallen  (Stadt)",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
            Type = DomainOfInfluenceType.Mu,
            ParentId = Guid.Parse(IdStGallen),
            Canton = DomainOfInfluenceCanton.Sg,
            ContactPerson = new ContactPerson
            {
                Email = "hans@sg.ch",
                Phone = "071 123 12 12",
                FamilyName = "Muster",
                FirstName = "Hans",
                MobilePhone = "079 123 12 12",
            },
        };

    public static DomainOfInfluence Uzwil
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdUzwil),
            BasisDomainOfInfluenceId = Guid.Parse(IdUzwil),
            Name = "Uzwil",
            NameForProtocol = "Stadt Uzwil",
            ShortName = "Uzwil",
            Bfs = "3443",
            Code = "C3443",
            SortNumber = 3,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            Type = DomainOfInfluenceType.Sk,
            ParentId = Guid.Parse(IdStGallen),
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static DomainOfInfluence Gossau
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdGossau),
            BasisDomainOfInfluenceId = Guid.Parse(IdGossau),
            Name = "Gossau",
            NameForProtocol = "Stadt Gossau",
            ShortName = "Gossau",
            SecureConnectId = SecureConnectTestDefaults.MockedTenantGossau.Id,
            Type = DomainOfInfluenceType.Sk,
            ParentId = Guid.Parse(IdStGallen),
            Canton = DomainOfInfluenceCanton.Sg,
            Parties = new List<DomainOfInfluenceParty>()
            {
                    new()
                    {
                        Id = Guid.Parse(PartyIdGossauFLiG),
                        BaseDomainOfInfluencePartyId = Guid.Parse(PartyIdGossauFLiG),
                        Translations = TranslationUtil.CreateTranslations<DomainOfInfluencePartyTranslation>(
                            (t, o) => t.Name = o,
                            "Freie Liste Gossau",
                            (t, o) => t.ShortDescription = o,
                            "FLiG"),
                    },
            },
        };

    public static DomainOfInfluence Kirchgemeinde
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdKirchgemeinde),
            BasisDomainOfInfluenceId = Guid.Parse(IdKirchgemeinde),
            Name = "Kirchgemeinde Oberuzwil",
            NameForProtocol = "Kirchgemeinde Oberuzwil",
            ShortName = "Kirchgemeinde Oberuzwil",
            SecureConnectId = CountingCircleMockedData.CountingCircleUzwilKircheSecureConnectId,
            Type = DomainOfInfluenceType.Ki,
            ParentId = null,
        };

    public static DomainOfInfluence Thurgau
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdThurgau),
            BasisDomainOfInfluenceId = Guid.Parse(IdThurgau),
            Name = "Thurgau",
            NameForProtocol = "Kanton Thurgau",
            ShortName = "Thurgau",
            SecureConnectId = "random-2",
            Type = DomainOfInfluenceType.Ct,
            ParentId = Guid.Parse(IdBund),
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static DomainOfInfluence Genf
        => new DomainOfInfluence
        {
            Id = Guid.Parse(IdGenf),
            BasisDomainOfInfluenceId = Guid.Parse(IdGenf),
            Name = "Genf",
            NameForProtocol = "Kanton Genf",
            ShortName = "Genf",
            Bfs = "5345",
            Code = "C5345",
            SortNumber = 5,
            SecureConnectId = SecureConnectTestDefaults.MockedTenantGenf.Id,
            Type = DomainOfInfluenceType.Ct,
            ParentId = Guid.Parse(IdBund),
            Canton = DomainOfInfluenceCanton.Sg,
        };

    public static IEnumerable<DomainOfInfluence> All
    {
        get
        {
            yield return Bund;
            yield return StGallen;
            yield return StGallenStadt;
            yield return Gossau;
            yield return Uzwil;
            yield return Kirchgemeinde;
            yield return Thurgau;
            yield return Genf;
        }
    }

    public static IEnumerable<DomainOfInfluenceCountingCircle> GetAllDomainOfInfluenceCountingCircles()
    {
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("ab32b5c3-48db-4242-90b6-16a13e94f8d9"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = CountingCircleMockedData.GuidBund,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("0FB13047-2EE4-426F-858D-DFFD1EA2207A"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = CountingCircleMockedData.GuidStGallenAuslandschweizer,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("b8d29bd5-a828-4783-807a-a3813e24144c"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = CountingCircleMockedData.GuidStGallen,
            ComparisonCountOfVotersCategory = ComparisonCountOfVotersCategory.B,
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("52e1ae3b-23af-4536-996d-41cc5cedb445"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            ComparisonCountOfVotersCategory = ComparisonCountOfVotersCategory.A,
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("085cdabd-c2d3-4d9e-b558-58706026d374"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("38932947-adea-4377-b92c-c1e5205e0abc"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = CountingCircleMockedData.GuidStGallenStFiden,
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("78ca5534-71a8-4fd5-988c-18efb686073b"),
            DomainOfInfluenceId = Guid.Parse(IdBund),
            CountingCircleId = CountingCircleMockedData.GuidStGallenHaggen,
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("dbf7dea8-3739-4e15-9e67-37f2f2fd6f1f"),
            DomainOfInfluenceId = Guid.Parse(IdStGallen),
            CountingCircleId = CountingCircleMockedData.GuidStGallen,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("b6bd656e-c2d8-4912-bab9-dfdbdbaacd0f"),
            DomainOfInfluenceId = Guid.Parse(IdStGallen),
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("260ca8b9-3967-4d76-b382-e3349b215055"),
            DomainOfInfluenceId = Guid.Parse(IdStGallen),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("5ECE831E-8CD3-41B9-9631-9330FAC72F18"),
            DomainOfInfluenceId = Guid.Parse(IdStGallen),
            CountingCircleId = CountingCircleMockedData.GuidStGallenStFiden,
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("B73C30CF-4AB8-4BA5-B956-648379E736DA"),
            DomainOfInfluenceId = Guid.Parse(IdStGallen),
            CountingCircleId = CountingCircleMockedData.GuidStGallenHaggen,
            Inherited = true,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("5A1E702A-8E41-46A3-9080-06C323B6D1D0"),
            DomainOfInfluenceId = Guid.Parse(IdStGallenStadt),
            CountingCircleId = CountingCircleMockedData.GuidStGallenStFiden,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("5A855BAA-F391-4D3F-9E09-260FB6785E99"),
            DomainOfInfluenceId = Guid.Parse(IdStGallenStadt),
            CountingCircleId = CountingCircleMockedData.GuidStGallenHaggen,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("B26CD185-A373-469F-91FB-8B28BA547F59"),
            DomainOfInfluenceId = Guid.Parse(IdStGallen),
            CountingCircleId = CountingCircleMockedData.GuidStGallenAuslandschweizer,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("ae8b1e0f-7ba0-4ec6-a25a-7acbcb42778f"),
            DomainOfInfluenceId = Guid.Parse(IdGossau),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("458a7b20-3eb3-42ba-bc9c-adeed71527d6"),
            DomainOfInfluenceId = Guid.Parse(IdUzwil),
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
        };
        yield return new DomainOfInfluenceCountingCircle
        {
            Id = Guid.Parse("a23b7b20-3eb3-42ba-bc9c-adeed71555c3"),
            DomainOfInfluenceId = Guid.Parse(IdKirchgemeinde),
            CountingCircleId = CountingCircleMockedData.GuidUzwilKirche,
        };
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await CountingCircleMockedData.Seed(runScoped);

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.DomainOfInfluences.AddRange(All);
            db.DomainOfInfluenceCountingCircles.AddRange(GetAllDomainOfInfluenceCountingCircles());
            await db.SaveChangesAsync();
        });
    }

    public static PlausibilisationConfigurationEventData BuildPlausibilisationConfiguration(Action<PlausibilisationConfigurationEventData>? customizer = null)
    {
        var plausiConfig = new PlausibilisationConfigurationEventData
        {
            ComparisonCountOfVotersConfigurations =
                {
                    new ComparisonCountOfVotersConfigurationEventData { Category = SharedProto.ComparisonCountOfVotersCategory.A, ThresholdPercent = 4 },
                    new ComparisonCountOfVotersConfigurationEventData { Category = SharedProto.ComparisonCountOfVotersCategory.B, ThresholdPercent = null },
                    new ComparisonCountOfVotersConfigurationEventData { Category = SharedProto.ComparisonCountOfVotersCategory.C, ThresholdPercent = 1.25 },
                },
            ComparisonVotingChannelConfigurations =
                {
                    new ComparisonVotingChannelConfigurationEventData { VotingChannel = SharedProto.VotingChannel.BallotBox, ThresholdPercent = 4 },
                    new ComparisonVotingChannelConfigurationEventData { VotingChannel = SharedProto.VotingChannel.ByMail, ThresholdPercent = null },
                    new ComparisonVotingChannelConfigurationEventData { VotingChannel = SharedProto.VotingChannel.EVoting, ThresholdPercent = 1.25 },
                    new ComparisonVotingChannelConfigurationEventData { VotingChannel = SharedProto.VotingChannel.Paper, ThresholdPercent = 7.5 },
                },
            ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = 1.25,
        };

        customizer?.Invoke(plausiConfig);
        return plausiConfig;
    }

    public static PlausibilisationConfiguration BuildDataPlausibilisationConfiguration(Action<PlausibilisationConfiguration>? customizer = null)
    {
        var plausiConfig = new PlausibilisationConfiguration
        {
            ComparisonCountOfVotersConfigurations = new List<ComparisonCountOfVotersConfiguration>()
                {
                    new ComparisonCountOfVotersConfiguration { Category = ComparisonCountOfVotersCategory.A, ThresholdPercent = 3 },
                    new ComparisonCountOfVotersConfiguration { Category = ComparisonCountOfVotersCategory.B, ThresholdPercent = 4 },
                    new ComparisonCountOfVotersConfiguration { Category = ComparisonCountOfVotersCategory.C, ThresholdPercent = null },
                },
            ComparisonVotingChannelConfigurations = new List<ComparisonVotingChannelConfiguration>()
                {
                    new ComparisonVotingChannelConfiguration { VotingChannel = VotingChannel.BallotBox, ThresholdPercent = 5 },
                    new ComparisonVotingChannelConfiguration { VotingChannel = VotingChannel.ByMail, ThresholdPercent = 6.5M },
                    new ComparisonVotingChannelConfiguration { VotingChannel = VotingChannel.EVoting, ThresholdPercent = 15 },
                    new ComparisonVotingChannelConfiguration { VotingChannel = VotingChannel.Paper, ThresholdPercent = null },
                },
            ComparisonVoterParticipationConfigurations = new List<ComparisonVoterParticipationConfiguration>(),
            ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = 5M,
        };
        customizer?.Invoke(plausiConfig);
        return plausiConfig;
    }
}
