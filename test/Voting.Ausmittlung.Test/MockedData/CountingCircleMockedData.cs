// (c) Copyright 2022 by Abraxas Informatik AG
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
            Id = Guid.Parse(IdBund),
            BasisCountingCircleId = Guid.Parse(IdBund),
            Bfs = "1",
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
            Bfs = "5500",
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
            Id = Guid.Parse(IdStGallenStFiden),
            BasisCountingCircleId = Guid.Parse(IdStGallenStFiden),
            Bfs = "5500-1",
            Code = "C5500-1",
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
            Id = Guid.Parse(IdGossau),
            BasisCountingCircleId = Guid.Parse(IdGossau),
            Bfs = "3443",
            Code = "3443-GOSSAU",
            ResponsibleAuthority = new Authority
            {
                SecureConnectId = SecureConnectTestDefaults.MockedTenantGossau.Id,
            },
            ContactPersonAfterEvent = new CountingCircleContactPerson(),
        };

    public static CountingCircle Uzwil
        => new CountingCircle
        {
            Id = Guid.Parse(IdUzwil),
            BasisCountingCircleId = Guid.Parse(IdUzwil),
            Name = "Uzwil",
            Bfs = "1234",
            Code = "1234-UZWIL",
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
        };

    public static CountingCircle Rorschach
        => new CountingCircle
        {
            Id = Guid.Parse(IdRorschach),
            BasisCountingCircleId = Guid.Parse(IdRorschach),
            Name = "Rorschach",
            Bfs = "5600",
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
