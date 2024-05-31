// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.MockedData;

public static class CountingCircleMockedData
{
    public const string IdBund = "eea024ee-7f93-4a58-9e7d-6e6b0c11b723";
    public const string IdStGallen = "1e2e062e-288d-4347-9f22-224e69b95bed";
    public const string IdGossau = "0bb8d89c-d387-40d0-83cf-84e118a8d524";
    public const string IdStGallenStFiden = "44A75721-F192-461C-816F-9F46A00D274A";
    public const string IdStGallenHaggen = "46ADAB98-9114-4503-9000-B02842640650";
    public const string IdStGallenAuslandschweizer = "35C7F490-ABF1-4D6E-A9B1-4AE4DE5248AB";
    public const string IdUzwil = "ca7be031-d4ce-4bb6-9538-b5e5d19e5a3e";
    public const string IdRorschach = "eae2cfaf-c787-48b9-a108-c975b0a580dc";
    public const string IdUzwilKirche = "ca7be031-d4ce-4bb6-9538-b5e5d19e5a4e";

    public const string CountingCircleUzwilKircheSecureConnectId = "kirche-uzwil-010000";

    public static readonly Guid GuidBund = Guid.Parse(IdBund);
    public static readonly Guid GuidStGallen = Guid.Parse(IdStGallen);
    public static readonly Guid GuidGossau = Guid.Parse(IdGossau);
    public static readonly Guid GuidStGallenStFiden = Guid.Parse(IdStGallenStFiden);
    public static readonly Guid GuidStGallenHaggen = Guid.Parse(IdStGallenHaggen);
    public static readonly Guid GuidStGallenAuslandschweizer = Guid.Parse(IdStGallenAuslandschweizer);
    public static readonly Guid GuidUzwil = Guid.Parse(IdUzwil);
    public static readonly Guid GuidRorschach = Guid.Parse(IdRorschach);
    public static readonly Guid GuidUzwilKirche = Guid.Parse(IdUzwilKirche);

    public static CountingCircle Bund
        => new CountingCircle
        {
            Name = "Bund",
            NameForProtocol = "Bund",
            Id = Guid.Parse(IdBund),
            BasisCountingCircleId = Guid.Parse(IdBund),
            Bfs = "1",
            SortNumber = 99999,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = SecureConnectTestDefaults.MockedTenantBund.Id,
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
        };

    public static CountingCircle StGallen
        => new CountingCircle
        {
            Id = Guid.Parse(IdStGallen),
            BasisCountingCircleId = Guid.Parse(IdStGallen),
            Name = "St. Gallen",
            NameForProtocol = "Stadt St. Gallen",
            Bfs = "5500",
            SortNumber = 2000,
            ResponsibleAuthority = new Authority
            {
                Name = "St. Gallen",
                Email = "sg@abraxas.ch",
                Phone = "071 123 12 20",
                Street = "WerkstrasseX",
                City = "MyCityX",
                Zip = "9000",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
            },
            ContactPersonSameDuringEventAsAfter = false,
            ContactPersonDuringEvent = new CountingCircleContactPerson
            {
                Email = "sg@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster-sg",
                FirstName = "Hans-sg",
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson
            {
                Email = "sg@abraxas.ch",
                Phone = "071 123 12 22",
                MobilePhone = "071 123 12 33",
                FamilyName = "Wichtig-sg",
                FirstName = "Rudolph-sg",
            },
        };

    public static CountingCircle StGallenStFiden
        => new CountingCircle
        {
            Name = "St. Gallen St. Fiden",
            NameForProtocol = "St. Fiden",
            Id = Guid.Parse(IdStGallenStFiden),
            BasisCountingCircleId = Guid.Parse(IdStGallenStFiden),
            Bfs = "5500-1",
            Code = "C5500-1",
            SortNumber = 2001,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
        };

    public static CountingCircle StGallenHaggen
        => new CountingCircle
        {
            Name = "St. Gallen Haggen",
            Id = Guid.Parse(IdStGallenHaggen),
            BasisCountingCircleId = Guid.Parse(IdStGallenHaggen),
            Bfs = "5500-2",
            SortNumber = 2002,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
        };

    public static CountingCircle StGallenAuslandschweizer
        => new CountingCircle
        {
            Name = "St. Gallen Auslandschweizer",
            Id = Guid.Parse(IdStGallenAuslandschweizer),
            BasisCountingCircleId = Guid.Parse(IdStGallenAuslandschweizer),
            Bfs = "5500-3",
            SortNumber = 2003,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
        };

    public static CountingCircle Gossau
        => new CountingCircle
        {
            Name = "Gossau",
            NameForProtocol = "Stadt Gossau",
            Id = Guid.Parse(IdGossau),
            BasisCountingCircleId = Guid.Parse(IdGossau),
            Bfs = "3443",
            Code = "3443-GOSSAU",
            SortNumber = 9800,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = SecureConnectTestDefaults.MockedTenantGossau.Id,
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
            Electorates = new List<CountingCircleElectorate>()
            {
                new()
                {
                    Id = Guid.Parse("8922095c-7b64-4f0b-aec1-a92c6521abc1"),
                    DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct },
                },
                new()
                {
                    Id = Guid.Parse("4078ba98-0db5-40a6-9869-11e5519f7af7"),
                    DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Bz },
                },
            },
            EVoting = true,
        };

    public static CountingCircle Uzwil
        => new CountingCircle
        {
            Id = Guid.Parse(IdUzwil),
            BasisCountingCircleId = Guid.Parse(IdUzwil),
            Name = "Uzwil",
            NameForProtocol = "Stadt Uzwil",
            Bfs = "1234",
            Code = "1234-UZWIL",
            SortNumber = 2005,
            ResponsibleAuthority = new Authority
            {
                Name = "Uzwil",
                Email = "uzwil-test@abraxas.ch",
                Phone = "071 123 12 20",
                Street = "Werkstrasse",
                City = "MyCity",
                Zip = "9595",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id,
            },
            ContactPersonSameDuringEventAsAfter = false,
            ContactPersonDuringEvent = new CountingCircleContactPerson
            {
                Email = "uzwil-test@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster",
                FirstName = "Hans",
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson
            {
                Email = "uzwil-test2@abraxas.ch",
                Phone = "071 123 12 22",
                MobilePhone = "071 123 12 33",
                FamilyName = "Wichtig",
                FirstName = "Rudolph",
            },
            Electorates = new List<CountingCircleElectorate>()
            {
                new()
                {
                    Id = Guid.Parse("f3a30723-86ce-4d1d-b3c1-28a950f5857e"),
                    DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Ch },
                },
                new()
                {
                    Id = Guid.Parse("4909649e-3386-4d37-890c-071851cb647c"),
                    DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Ct },
                },
                new()
                {
                    Id = Guid.Parse("86d85a46-a1d9-4e63-af85-60641ca371dd"),
                    DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Bz },
                },
            },
            EVoting = true,
        };

    public static CountingCircle Rorschach
        => new CountingCircle
        {
            Id = Guid.Parse(IdRorschach),
            BasisCountingCircleId = Guid.Parse(IdRorschach),
            Name = "Rorschach",
            NameForProtocol = "Stadt Rorschach",
            Bfs = "5600",
            SortNumber = 2006,
            ResponsibleAuthority = new Authority
            {
                Name = "Rorschach",
                Email = "ro@abraxas.ch",
                Phone = "071 123 12 20",
                Street = "WerkstrasseX",
                City = "MyCityX",
                Zip = "9200",
                SecureConnectId = "1234444444",
            },
            ContactPersonSameDuringEventAsAfter = true,
            ContactPersonDuringEvent = new CountingCircleContactPerson
            {
                Email = "ro@abraxas.ch",
                Phone = "071 123 12 21",
                MobilePhone = "071 123 12 31",
                FamilyName = "Muster-sg",
                FirstName = "Hans-sg",
            },
        };

    public static CountingCircle UzwilKirche
        => new CountingCircle
        {
            Name = "Kirche Uzwil",
            Id = Guid.Parse(IdUzwilKirche),
            BasisCountingCircleId = Guid.Parse(IdUzwilKirche),
            Bfs = "none",
            Code = "code-k-uz",
            SortNumber = 2007,
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = CountingCircleUzwilKircheSecureConnectId,
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
        };

    public static IEnumerable<CountingCircle> All
    {
        get
        {
            yield return Bund;
            yield return StGallen;
            yield return StGallenStFiden;
            yield return StGallenHaggen;
            yield return StGallenAuslandschweizer;
            yield return Uzwil;
            yield return Rorschach;
            yield return UzwilKirche;
            yield return Gossau;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.CountingCircles.AddRange(All);
            await db.SaveChangesAsync();
        });
    }
}
