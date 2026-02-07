// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Shared.V1;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;
using BallotNumberGeneration = Abraxas.Voting.Basis.Shared.V1.BallotNumberGeneration;
using BasisEvents = Abraxas.Voting.Basis.Events.V1;
using ContestCountingCircleDetailsCreated = Abraxas.Voting.Ausmittlung.Events.V2.ContestCountingCircleDetailsCreated;
using DomainOfInfluenceType = Abraxas.Voting.Basis.Shared.V1.DomainOfInfluenceType;
using ProportionalElectionReviewProcedure = Abraxas.Voting.Basis.Shared.V1.ProportionalElectionReviewProcedure;
using SexType = Abraxas.Voting.Ausmittlung.Shared.V1.SexType;
using VotingChannel = Abraxas.Voting.Basis.Shared.V1.VotingChannel;

namespace Voting.Ausmittlung.Test.E2ETests;

public class ProportionalElectionE2ETest : BaseTest<ProportionalElectionResultService.ProportionalElectionResultServiceClient>
{
    private const string CountingCircleId = "56549b12-6352-48d5-a5d5-7e101be974cb";
    private const string DomainOfInfluenceId = "25066c7c-2fe7-4859-a348-848bb9aa4b56";
    private const string ContestId = "131a5f39-c8f8-4f93-a990-6caca52c102b";
    private const string ProportionalElectionId = "44ae6898-99ae-4e9d-9329-6eaa39bc687e";
    private const int NumberOfMandates = 12;
    private const string EVotingEch0222File = "E2ETests/EVotingFiles/eCH-0222 proportional election.xml";
    private const string EVotingEch0110File = "E2ETests/EVotingFiles/eCH-0110 proportional election.xml";

    #region lists and candidates
    private const string List01aId = "1d3eedb5-24a1-4ad6-a0c1-4e7f9f339ea9";
    private const string List01bId = "2129fba1-fef8-49b5-ae6f-624e0d491c0a";
    private const string List01cId = "a2f1c4bf-bc57-439f-afe3-ec8be2992025";
    private const string List02aId = "42cba616-5996-4dbd-9641-700d5a2539a9";
    private const string List02cId = "ff397f68-2e01-4304-923a-6e7be9305ca6";
    private const string List03bId = "063c9c22-fdc8-48da-9ec5-73562218275f";
    private const string List09Id = "61367732-40e5-4abd-a007-a300ecf342f8";
    private const string List10Id = "0f5c6a1e-96a6-4122-9260-98ab9f8267f3";
    private const string List11Id = "86c4c0d1-c1bb-4bc6-ba22-e82bb5c0941d";

    private const string Candidate01dP11Id = "78417b12-bc49-4556-a450-da5496bb6470";
    private const string Candidate01dP12Id = "1021d4e3-3b8d-4fad-86f5-d1228055a18e";
    private const string Candidate02bP01Id = "6d738ecd-db7e-43fb-9d42-ccad25939a6d";
    private const string Candidate02bP02Id = "a393e2aa-1c9c-4d4d-8472-ae2ca8a605dd";
    private const string Candidate02cP01Id = "baee9bfe-5876-4cbf-bb10-bc0b829b92aa";
    private const string Candidate02cP02Id = "92ab2e8c-02e5-48ad-809f-cf0fc98cdf86";
    private const string Candidate03aP01Id = "f152ac70-a021-4967-83e7-077501a5819f";
    private const string Candidate03aP02Id = "3a16e20e-a5df-49c1-9b28-f09246c8035f";

    private static readonly List<ListTestData> Lists = new()
    {
        new ListTestData(
            List01aId,
            "01a",
            "SVP",
            new ListExpectedResult(48, 0, 0, 0, 48, 4, 0),
            new List<CandidateTestData>
            {
                new("20f65b7a-cb47-46af-8c3c-0c03a2c30c67", 1, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("2d0615ed-3700-452b-b18c-4e993b814b1d", 2, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("1af01aa8-8def-46b1-97d8-45f23a83e1f6", 3, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("ab0dcee5-26d5-4560-9e99-d0a4539dc3e6", 4, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("dc15837b-0e2b-437b-93eb-a0efac3038b4", 5, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("5e86ad6b-226b-4d34-98a3-ecc3225e3785", 6, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("fceb8a13-619f-4528-b1d1-618635c6debd", 7, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("bef3c4d7-2692-41e6-86fc-254c280fd793", 8, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("4dff542d-6c0b-455f-abab-8b9f86d5110c", 9, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("370ff31e-4661-4bed-b123-468d8926d053", 10, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("0b44076e-11f2-4c5e-81be-0b9fbc67d1d5", 11, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
                new("e4904add-9bcd-4f73-9fb5-f3f79f8bf290", 12, null, new CandidateExpectedResult(4, 0, 4, 0, 0)),
            }),
        new ListTestData(
            List01bId,
            "01b",
            "SVP UL",
            new ListExpectedResult(0, 0, 55, 5, 60, 0, 5),
            new List<CandidateTestData>
            {
                new("cdab53e6-7461-4937-b448-680d7092def4", 1, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("b5e0aa20-5d9c-4f1e-8ba1-f50c6356560c", 2, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("75079c76-80ea-4a88-b3e5-112d72cb774f", 3, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("7d04eb6f-f5af-4248-9698-9b755498d0e7", 4, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("c5861172-29fc-412c-8ed2-cf8e3e9d05b4", 5, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("a3792819-b223-47d0-850a-9c009d6684e0", 6, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("5f2eea06-6b75-4b4e-a3f7-f1f8f8075a01", 7, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("4339e578-5582-45c0-8dcb-8584ddf3e7ab", 8, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("3899dc38-5c7a-4af7-845f-b01822e712b1", 9, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("6252b692-1f0f-4ddd-98d3-c28771909e87", 10, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("d902681b-245f-4e78-9894-59d5c8cdf2ce", 11, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("65209c9d-d7b5-44bf-83e8-d7b0542894d1", 12, null, new CandidateExpectedResult(0, 2, 2, 0, 0)),
            }),
        new ListTestData(
            List01cId,
            "01c",
            "SVP SL",
            new ListExpectedResult(0, 0, 33, 0, 33, 0, 3),
            new List<CandidateTestData>
            {
                new("ca693a10-a6b6-4fb1-be57-d682e968e1e6", 1, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("955d3a27-a061-4651-a318-da1d9485aaed", 2, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("65e3a8dc-fc97-4859-b838-08822e2f21ea", 3, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("8a52fc7f-3740-48da-9ee1-f790318b0d6a", 4, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("e0f3103b-ff82-4200-8c43-54da948f192a", 5, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("afca661f-cb6b-478b-b25c-f6be4e56f6bf", 6, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("78513107-8115-4001-868a-0128400abb64", 7, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("bcc1edf5-8574-43d3-bf3f-9e9570efda2f", 8, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("2b32e0a1-e80b-4d1b-8e31-9bdd09d42582", 9, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("941e1991-fdff-4fea-9479-83a70a33dd1a", 10, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("137a9bf6-c96b-485f-9acb-9819763038e9", 11, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("924cd676-cc0d-4b60-a6a7-62b7d0722381", 12, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
            }),
        new ListTestData(
            "82a4e94a-35d4-426b-8256-523dabcae2eb",
            "01d",
            "SVP LL",
            new ListExpectedResult(0, 0, 3, 0, 3, 0, 0),
            new List<CandidateTestData>
            {
                new("dffb4053-e1e3-4809-9618-fbe93877508e", 1, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("5d92bc13-2182-4ded-95dd-e9b167e4bc5a", 2, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("676315ab-2eef-4455-a7a1-47492cf4279a", 3, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("fb77ef15-8833-452b-9599-dfc2401865d5", 4, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("69344893-92bd-410f-b490-28f682df5509", 5, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("984c3d2d-0d7b-4473-a299-af8ee8c764d2", 6, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("980e22d0-b265-4059-bacf-6a1b6a96de3d", 7, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("80ba9825-74fe-48a8-822c-ada8e3b78884", 8, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("fb1cf8fb-f80a-4e73-b93e-f7c1550c0f59", 9, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("9c7a0b89-9b52-40ff-bb16-420835cdebc6", 10, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new(Candidate01dP11Id, 11, null, new CandidateExpectedResult(0, 1, 1, 1, 0)),
                new(Candidate01dP12Id, 12, null, new CandidateExpectedResult(0, 2, 2, 2, 0)),
            }),
        new ListTestData(
            List02aId,
            "02a",
            "CVP SO",
            new ListExpectedResult(0, 0, 30, 0, 30, 0, 3),
            new List<CandidateTestData>
            {
                new("a3dd849e-8cec-4523-aa94-92a5e904eed0", 1, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("8937b35c-ac1d-4c9b-9db6-e641ee90acda", 2, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("d333a039-1630-4f62-93c4-d16f429de682", 3, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("033c6de2-458e-4c3d-99ce-ab148644db0f", 4, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("b8431c90-b556-4c11-a4bd-1c88de0647c1", 5, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("6b99fe08-6a7b-4beb-8b92-7bca8584d44a", 6, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("3f1c3783-119d-44ea-8b1e-466a204f8593", 7, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("f7b68f96-59be-405d-bc4f-d3e8ab088fbc", 8, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("add790a1-fc0e-4d8b-b52b-9abbb95f1da7", 9, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("f821a031-b854-4d9c-85db-211cf81e0d60", 10, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("67bb8167-9da1-4272-b85b-322eb92021c1", 11, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("bdea556f-f5e7-4fcd-a222-243d1d6d6a13", 12, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
            }),
        new ListTestData(
            "cc6435ec-48dc-4ebe-9652-8c5ffec24d00",
            "02b",
            "CVP NW",
            new ListExpectedResult(0, 0, 6, 0, 6, 0, 0),
            new List<CandidateTestData>
            {
                new(Candidate02bP01Id, 1, null, new CandidateExpectedResult(0, 4, 4, 4, 2)),
                new(Candidate02bP02Id, 2, null, new CandidateExpectedResult(0, 2, 2, 2, 1)),
                new("46b74182-0f13-4523-be37-7b7adf0e6677", 3, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("1baba48d-ef5b-41cd-94bc-fdbe91bdb481", 4, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("4ce61517-34ba-4ab3-857d-2913cb04df32", 5, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("5c46d44f-8472-4764-b2f0-f44a17a46bae", 6, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("78186e6f-cace-4d33-bd02-e466b1a94937", 7, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("6cf4b99d-ecdd-4ac1-91db-d45d09f8f4aa", 8, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("845e8fa8-78bc-461e-9a2e-7e8e7185d6f6", 9, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("cf0c4e4b-6f9a-4de7-a1bb-71ae121a9f08", 10, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("72c80bc1-b47d-41e3-ba6d-757c90934231", 11, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("05ac75e2-9def-4500-a366-56e8bfeecea9", 12, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
            }),
        new ListTestData(
            List02cId,
            "02c",
            "JCVP SO",
            new ListExpectedResult(0, 0, 36, 0, 36, 0, 3),
            new List<CandidateTestData>
            {
                new(Candidate02cP01Id, 1, null, new CandidateExpectedResult(0, 6, 6, 0, 3)),
                new(Candidate02cP02Id, 2, null, new CandidateExpectedResult(0, 6, 6, 0, 3)),
                new("ab7d603d-2ba0-4b0b-85e2-10a2ab463def", 3, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("7c30de3b-6132-47ad-9741-e8cd343fe447", 4, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("7b8d247a-0326-4850-9ccd-6bc03cd4a301", 5, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("33393695-77de-4a56-b853-0f5c606defe0", 6, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("2620c721-8d7a-44b7-b6f8-e21c147c4d91", 7, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("48ae1c88-974b-4b65-a1c9-9fe3fb7623b4", 8, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("ff11aa5d-8509-4dff-963c-668b00986817", 9, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("57ede352-ab72-4a75-b31d-786a8c9c287a", 10, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("c3e55a9b-f004-40c7-b581-78c7129a4383", 11, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("9dcb6511-563e-4d3e-9b17-28331f071caf", 12, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
            }),
        new ListTestData(
            "6731ebc8-fa69-468a-b03d-81f608c2fdf7",
            "03a",
            "SP",
            new ListExpectedResult(0, 0, 10, 0, 10, 0, 0),
            new List<CandidateTestData>
            {
                new(Candidate03aP01Id, 1, null, new CandidateExpectedResult(0, 7, 7, 0, 2)),
                new(Candidate03aP02Id, 2, null, new CandidateExpectedResult(0, 3, 3, 0, 0)),
                new("024bdb60-71a8-47c7-81d0-9e3fded4071d", 3, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("78830cf2-6490-4d1f-bdbd-b488c7ab2669", 4, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("af4ebc7b-e104-433b-a4a4-ad825a6bfb20", 5, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("5c6db875-4de3-43dc-9bb4-ca7462fb5524", 6, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("b05a69e0-fbd1-4845-8dab-a3281e62a67c", 7, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("7322c29e-3510-4bb7-9c20-36a3470caa29", 8, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("f97fcccf-0701-4ae8-bee7-6e638733cdaf", 9, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("2127447d-7aaa-4dca-b58b-50855c8cc4d6", 10, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("a29ab7a4-d578-4e97-ab9e-4935d220a903", 11, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("c7569c16-36db-49e0-9981-3d1633bf089b", 12, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
            }),
        new ListTestData(
            List03bId,
            "03b",
            "SP JUSO",
            new ListExpectedResult(0, 0, 50, 0, 50, 0, 0),
            new List<CandidateTestData>
            {
                new("aab0e576-a6e2-4657-acc5-ac0c89f3b360", 1, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("38c23221-24d0-4774-9289-b46e1b0437f1", 2, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("13c08ffc-a412-4969-a738-155b18bae873", 3, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("38a636b4-36ef-4bb3-b3c0-4128091d5771", 4, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("4b117f0f-a358-4975-8232-d838c0964f0d", 5, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("19d6bd9b-83eb-457c-b011-bc7c51b89036", 6, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("cdbe984e-0afb-4852-a9a1-55bb73759bea", 7, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("e59a89a4-9140-45ef-9f85-0c3b66f2ebab", 8, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("1c7e05ea-a686-4af2-b8f0-62933896d861", 9, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("3c74d939-3a48-43b1-8b4c-b8866df1fd35", 10, null, new CandidateExpectedResult(0, 5, 5, 0, 0)),
                new("ba48a691-7256-4f4f-933d-890e8be2b543", 11, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
                new("f82d857a-a3e3-4244-a9b3-b6c892bd2e61", 12, null, new CandidateExpectedResult(0, 0, 0, 0, 0)),
            }),
        new ListTestData(
            List09Id,
            "09",
            "EDU",
            new ListExpectedResult(60, 0, 0, 0, 60, 5, 0),
            new List<CandidateTestData>
            {
                new("050e787b-989f-4423-bb93-9a0a6f64ac2f", 1, 2, new CandidateExpectedResult(10, 0, 10, 0, 5)),
                new("7fda3dcf-b8bb-4117-949a-4f20bc15b61f", 3, 4, new CandidateExpectedResult(10, 0, 10, 0, 5)),
                new("d7e81d70-93e3-44cf-821f-81c2c02be689", 5, 6, new CandidateExpectedResult(10, 0, 10, 0, 5)),
                new("bf9f6bda-d273-449b-8654-bfdabd5719a7", 7, 8, new CandidateExpectedResult(10, 0, 10, 0, 5)),
                new("ddba1744-47f9-437b-8e9d-c71a1e9cf639", 9, 10, new CandidateExpectedResult(10, 0, 10, 0, 5)),
                new("78bca014-8deb-47ad-a44c-d8d4272dcb44", 11, null, new CandidateExpectedResult(5, 0, 5, 0, 0)),
                new("c500a7ef-5e59-4e95-9eef-b98ac94a1731", 12, null, new CandidateExpectedResult(5, 0, 5, 0, 0)),
            }),
        new ListTestData(
            List10Id,
            "10",
            "PFLUG",
            new ListExpectedResult(14, 70, 0, 0, 84, 7, 0),
            new List<CandidateTestData>
            {
                new("63604559-7663-418a-843a-567e484136be", 1, 2, new CandidateExpectedResult(14, 0, 14, 0, 7)),
            }),
        new ListTestData(
            List11Id,
            "11",
            "SD",
            new ListExpectedResult(0, 0, 24, 0, 24, 0, 0),
            new List<CandidateTestData>
        {
            new("328040ff-ca0c-4a85-8a8c-cf27581fc973", 1, 2, new CandidateExpectedResult(0, 6, 6, 0, 3)),
            new("1cade6e2-485b-4c28-a56c-012b334f2c22", 3, 4, new CandidateExpectedResult(0, 6, 6, 0, 3)),
            new("2d5ce7b1-721c-4e12-b7a5-48dfc5778fb5", 5, 6, new CandidateExpectedResult(0, 6, 6, 0, 3)),
            new("a820a8cd-e9b6-4a76-9eed-2101646eb437", 7, 8, new CandidateExpectedResult(0, 6, 6, 0, 3)),
        }),
    };
    #endregion

    private static readonly string ElectionResultId = AusmittlungUuidV5
        .BuildPoliticalBusinessResult(Guid.Parse(ProportionalElectionId), Guid.Parse(CountingCircleId), true).ToString();

    private long _mockedBasisEventInfoSeconds = 1594979476;

    public ProportionalElectionE2ETest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ProportionalElectionShouldWorkEndToEnd()
    {
        await SetupContestAndProportionalElection();
        await SecondFactorTransactionMockedData.Seed(RunScoped);
        await EnterContestDetails();
        await ImportEVotingResults();
        await EnterResults();

        var monitoringResultService = CreateService<ProportionalElectionResultService.ProportionalElectionResultServiceClient>(
            RolesMockedData.MonitoringElectionAdmin);
        var endResult = await monitoringResultService.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionId,
        });

        // Check voting cards ("Stimmrechtsausweise")
        var paperVotingCards = endResult.VotingCards.First(vc =>
            vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.Paper);
        paperVotingCards.CountOfReceivedVotingCards.Should().Be(2);
        var ballotVotingCards = endResult.VotingCards.First(vc =>
            vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.BallotBox);
        ballotVotingCards.CountOfReceivedVotingCards.Should().Be(4);
        var validMailVotingCards = endResult.VotingCards.First(vc =>
            vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail);
        validMailVotingCards.CountOfReceivedVotingCards.Should().Be(16);
        var invalidMailVotingCards = endResult.VotingCards.First(vc =>
            !vc.Valid && vc.Channel == Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail);
        invalidMailVotingCards.CountOfReceivedVotingCards.Should().Be(5);

        var validVotingCards = endResult.VotingCards
            .Where(vc => vc.Valid)
            .Sum(vc => vc.CountOfReceivedVotingCards);
        validVotingCards.Should().Be(42);
        var countOfVotingCards = endResult.VotingCards.Sum(vc => vc.CountOfReceivedVotingCards);
        countOfVotingCards.Should().Be(47);

        // Check ballots ("Wahlzettel")
        endResult.CountOfVoters.ConventionalSubTotal.ReceivedBallots.Should().Be(44);
        endResult.CountOfVoters.ConventionalSubTotal.BlankBallots.Should().Be(17);
        endResult.CountOfVoters.ConventionalSubTotal.InvalidBallots.Should().Be(5);
        endResult.CountOfVoters.ConventionalSubTotal.AccountedBallots.Should().Be(22);

        endResult.CountOfVoters.EVotingSubTotal.ReceivedBallots.Should().Be(20);
        endResult.CountOfVoters.EVotingSubTotal.BlankBallots.Should().Be(3);
        endResult.CountOfVoters.EVotingSubTotal.InvalidBallots.Should().Be(1);
        endResult.CountOfVoters.EVotingSubTotal.AccountedBallots.Should().Be(16);

        endResult.CountOfVoters.TotalReceivedBallots.Should().Be(64);
        endResult.CountOfVoters.TotalBlankBallots.Should().Be(20);
        endResult.CountOfVoters.TotalInvalidBallots.Should().Be(6);
        endResult.CountOfVoters.TotalAccountedBallots.Should().Be(38);

        foreach (var listEndResult in endResult.ListEndResults)
        {
            var matchingList = Lists.First(l => l.Id == listEndResult.List.Id);
            var expectedResult = matchingList.ExpectedResult;

            // Check list result
            listEndResult.UnmodifiedListVotesCount.Should().Be(expectedResult.CandidateVotesFromUnmodifiedLists, matchingList.Id);
            listEndResult.UnmodifiedListBlankRowsCount.Should().Be(expectedResult.VotesFromBlankRowsOfUnmodifiedLists, matchingList.Id);
            listEndResult.ModifiedListVotesCount.Should().Be(expectedResult.CandidateVotesFromModifiedLists, matchingList.Id);
            listEndResult.ModifiedListBlankRowsCount.Should().Be(expectedResult.VotesFromBlankRowsOfModifiedLists, matchingList.Id);
            listEndResult.TotalVoteCount.Should().Be(expectedResult.TotalVoteCount, matchingList.Id);
            listEndResult.UnmodifiedListsCount.Should().Be(expectedResult.CountOfUnmodifiedLists, matchingList.Id);
            listEndResult.ModifiedListsCount.Should().Be(expectedResult.CountOfModifiedLists, matchingList.Id);

            foreach (var candidateResult in listEndResult.CandidateEndResults)
            {
                var candidateId = candidateResult.Candidate.Id;
                var expectedCandidateResult = matchingList.Candidates.First(c => c.Id == candidateId).ExpectedResult;

                // Check candidate result
                var unmodifiedListVotesCount = candidateResult.ConventionalSubTotal.UnmodifiedListVotesCount
                    + candidateResult.EVotingSubTotal.UnmodifiedListVotesCount;
                unmodifiedListVotesCount.Should().Be(expectedCandidateResult.VotesFromUnmodifiedLists, candidateId);

                var modifiedListVotesCount = candidateResult.ConventionalSubTotal.ModifiedListVotesCount
                    + candidateResult.EVotingSubTotal.ModifiedListVotesCount;
                modifiedListVotesCount.Should().Be(expectedCandidateResult.VotesFromModifiedLists, candidateId);

                candidateResult.VoteCount.Should().Be(expectedCandidateResult.TotalVoteCount, candidateId);

                var countOfVotesOnOtherLists = candidateResult.ConventionalSubTotal.CountOfVotesOnOtherLists
                    + candidateResult.EVotingSubTotal.CountOfVotesOnOtherLists;
                countOfVotesOnOtherLists.Should().Be(expectedCandidateResult.VotesFromOtherLists, candidateId);

                var countOfVotesFromAccumulations = candidateResult.ConventionalSubTotal.CountOfVotesFromAccumulations
                    + candidateResult.EVotingSubTotal.CountOfVotesFromAccumulations;
                countOfVotesFromAccumulations.Should().Be(expectedCandidateResult.VotesFromAccumulations, candidateId);
            }
        }

        endResult.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        // Not relevant for this class, but we need to provide one anyway
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .GetEndResultAsync(new GetProportionalElectionEndResultRequest
            {
                ProportionalElectionId = ProportionalElectionId,
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        // Skip this test, not needed here.
        yield break;
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        // Skip this test, not needed here.
        yield break;
    }

    private async Task SetupContestAndProportionalElection()
    {
        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.CantonSettingsCreated
        {
            CantonSettings = new CantonSettingsEventData
            {
                Id = Guid.NewGuid().ToString(),
                Canton = DomainOfInfluenceCanton.Sg,
                AuthorityName = "KT SG",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                EnabledVotingCardChannels =
                {
                    new CantonSettingsVotingCardChannelEventData
                    {
                        Valid = true,
                        VotingChannel = VotingChannel.ByMail,
                    },
                    new CantonSettingsVotingCardChannelEventData
                    {
                        Valid = false,
                        VotingChannel = VotingChannel.ByMail,
                    },
                    new CantonSettingsVotingCardChannelEventData
                    {
                        Valid = true,
                        VotingChannel = VotingChannel.BallotBox,
                    },
                    new CantonSettingsVotingCardChannelEventData
                    {
                        Valid = true,
                        VotingChannel = VotingChannel.Paper,
                    },
                },
                ProportionalElectionMandateAlgorithms = { ProportionalElectionMandateAlgorithm.HagenbachBischoff },
                SwissAbroadVotingRight = SwissAbroadVotingRight.SeparateCountingCircle,
                ProportionalElectionUseCandidateCheckDigit = true,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.CountingCircleCreated
        {
            CountingCircle = new CountingCircleEventData
            {
                Id = CountingCircleId,
                Bfs = "123",
                Code = "123",
                Name = "Auslandschweizer Kanton St.Gallen",
                SortNumber = 1,
                ResponsibleAuthority = new AuthorityEventData
                {
                    Name = "Kanton SG",
                    SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                },
                ContactPersonDuringEvent = new ContactPersonEventData
                {
                    Email = "test@example.com",
                    FirstName = "Toni",
                    FamilyName = "Tester",
                    Phone = "+41799999999",
                    MobilePhone = "41799999988",
                },
                ContactPersonSameDuringEventAsAfter = true,
                EVoting = true,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.DomainOfInfluenceCreated
        {
            DomainOfInfluence = new DomainOfInfluenceEventData
            {
                Id = DomainOfInfluenceId,
                Bfs = "321",
                Code = "321",
                Name = "Kanton St. Gallen",
                ShortName = "KT SG",
                Canton = DomainOfInfluenceCanton.Sg,
                SortNumber = 1,
                SecureConnectId = SecureConnectTestDefaults.MockedTenantDefault.Id,
                AuthorityName = SecureConnectTestDefaults.MockedTenantDefault.Name,
                Type = DomainOfInfluenceType.Ch,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.DomainOfInfluenceCountingCircleEntriesUpdated
        {
            DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
            {
                Id = DomainOfInfluenceId,
                CountingCircleIds = { CountingCircleId },
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.ContestCreated
        {
            Contest = new ContestEventData
            {
                Id = ContestId,
                DomainOfInfluenceId = DomainOfInfluenceId,
                Date = MockedClock.GetDate(2).ToTimestamp(),
                EndOfTestingPhase = MockedClock.GetDate(-1).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("contest desc") },
                State = ContestState.TestingPhase,
                EVoting = true,
                EVotingFrom = MockedClock.GetDate().ToTimestamp(),
                EVotingTo = MockedClock.GetDate(1).ToTimestamp(),
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.ProportionalElectionCreated
        {
            ProportionalElection = new ProportionalElectionEventData
            {
                Id = ProportionalElectionId,
                ContestId = ContestId,
                DomainOfInfluenceId = DomainOfInfluenceId,
                InternalDescription = "internal desc",
                OfficialDescription = { LanguageUtil.MockAllLanguages("official desc") },
                ShortDescription = { LanguageUtil.MockAllLanguages("short desc") },
                MandateAlgorithm = ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                NumberOfMandates = NumberOfMandates,
                PoliticalBusinessNumber = "01",
                AutomaticEmptyVoteCounting = true,
                ReviewProcedure = ProportionalElectionReviewProcedure.Physically,
                BallotBundleSize = 25,
                BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
                BallotBundleSampleSize = 2,
            },
            EventInfo = GetMockedBasisEventInfo(),
        });

        var position = 1;
        foreach (var list in Lists)
        {
            await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.ProportionalElectionListCreated
            {
                ProportionalElectionList = new ProportionalElectionListEventData
                {
                    Id = list.Id,
                    OrderNumber = list.Number,
                    Description = { LanguageUtil.MockAllLanguages(list.Name) },
                    ShortDescription = { LanguageUtil.MockAllLanguages(list.Name) },
                    ProportionalElectionId = ProportionalElectionId,
                    Position = position++,
                    BlankRowCount = NumberOfMandates - list.Candidates.Count - list.Candidates.Count(c => c.AccumulatedPosition.HasValue),
                },
                EventInfo = GetMockedBasisEventInfo(),
            });

            var candidateNumber = 1;
            foreach (var candidate in list.Candidates)
            {
                await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.ProportionalElectionCandidateCreated
                {
                    ProportionalElectionCandidate = new ProportionalElectionCandidateEventData
                    {
                        Id = candidate.Id,
                        ProportionalElectionListId = list.Id,
                        ProportionalElectionId = ProportionalElectionId,
                        Position = candidate.Position,
                        Number = candidateNumber.ToString("00"),
                        Accumulated = candidate.AccumulatedPosition.HasValue,
                        AccumulatedPosition = candidate.AccumulatedPosition ?? 0,
                        DateOfBirth = MockedClock.GetDate(-7000).ToTimestamp(),
                    },
                    EventInfo = GetMockedBasisEventInfo(),
                });

                candidateNumber++;
            }
        }

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.ProportionalElectionActiveStateUpdated
        {
            ProportionalElectionId = ProportionalElectionId,
            Active = true,
            EventInfo = GetMockedBasisEventInfo(),
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new BasisEvents.ContestTestingPhaseEnded
        {
            ContestId = ContestId,
            EventInfo = GetMockedBasisEventInfo(),
        });
    }

    private async Task EnterContestDetails()
    {
        var contestDetailsService = CreateService<ContestCountingCircleDetailsService.ContestCountingCircleDetailsServiceClient>(
            RolesMockedData.ErfassungElectionAdmin);
        await contestDetailsService.UpdateDetailsAsync(new UpdateContestCountingCircleDetailsRequest
        {
            ContestId = ContestId,
            CountingCircleId = CountingCircleId,
            VotingCards =
            {
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = true,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.Paper,
                    CountOfReceivedVotingCards = 2,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = true,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.BallotBox,
                    CountOfReceivedVotingCards = 4,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = true,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail,
                    CountOfReceivedVotingCards = 16,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Valid = false,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                    Channel = Abraxas.Voting.Ausmittlung.Shared.V1.VotingChannel.ByMail,
                    CountOfReceivedVotingCards = 5,
                },
            },
            CountOfVotersInformationSubTotals =
            {
                new UpdateCountOfVotersInformationSubTotalRequest
                {
                    CountOfVoters = 6171,
                    Sex = SexType.Male,
                    VoterType = VoterType.Swiss,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                },
                new UpdateCountOfVotersInformationSubTotalRequest
                {
                    CountOfVoters = 6180,
                    Sex = SexType.Female,
                    VoterType = VoterType.Swiss,
                    DomainOfInfluenceType = Abraxas.Voting.Ausmittlung.Shared.V1.DomainOfInfluenceType.Ch,
                },
            },
        });

        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyCreated>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ContestCountingCircleDetailsCreated>().Should().HaveCount(1);
        await RunAllEvents();
    }

    private async Task ImportEVotingResults()
    {
        var uri = new Uri($"api/result_import/e-voting/{ContestId}", UriKind.RelativeOrAbsolute);
        using var httpClient = CreateHttpClient(RolesMockedData.MonitoringElectionAdmin);
        using var resp = await httpClient.PostFiles(
            uri,
            ("ech0222File", EVotingEch0222File),
            ("ech0110File", EVotingEch0110File));
        resp.EnsureSuccessStatusCode();

        EventPublisherMock.AllPublishedEvents.Should().HaveCount(6);
        EventPublisherMock.GetPublishedEvents<ResultImportCreated>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ResultImportStarted>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<CountingCircleVotingCardsImported>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultImported>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ResultImportCompleted>().Should().HaveCount(1);
        await RunAllEvents();
    }

    private async Task EnterResults()
    {
        // This starts the result submission
        var resultService = CreateService<ResultService.ResultServiceClient>(RolesMockedData.ErfassungElectionAdmin);
        await resultService.GetListAsync(new GetResultListRequest
        {
            ContestId = ContestId,
            CountingCircleId = CountingCircleId,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultSubmissionStarted>().Should().HaveCount(1);
        await RunAllEvents();

        var electionResultService = CreateService<ProportionalElectionResultService.ProportionalElectionResultServiceClient>(
            RolesMockedData.ErfassungElectionAdmin);

        await electionResultService.DefineEntryAsync(new DefineProportionalElectionResultEntryRequest
        {
            ElectionResultId = ElectionResultId,
            ResultEntryParams = new DefineProportionalElectionResultEntryParamsRequest
            {
                BallotNumberGeneration =
                    Abraxas.Voting.Ausmittlung.Shared.V1.BallotNumberGeneration.RestartForEachBundle,
                BallotBundleSize = 25,
                BallotBundleSampleSize = 2,
                AutomaticEmptyVoteCounting = true,
                ReviewProcedure = Abraxas.Voting.Ausmittlung.Shared.V1.ProportionalElectionReviewProcedure.Physically,
                AutomaticBallotBundleNumberGeneration = true,
                AutomaticBallotNumberGeneration = true,
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultEntryDefined>().Should().HaveCount(1);
        await RunAllEvents();

        await electionResultService.EnterCountOfVotersAsync(new EnterProportionalElectionCountOfVotersRequest
        {
            ElectionResultId = ElectionResultId,
            CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
            {
                ConventionalReceivedBallots = 44,
                ConventionalBlankBallots = 17,
                ConventionalInvalidBallots = 5,
                ConventionalAccountedBallots = 22,
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultCountOfVotersEntered>().Should().HaveCount(1);
        await RunAllEvents();

        await electionResultService.EnterUnmodifiedListResultsAsync(new EnterProportionalElectionUnmodifiedListResultsRequest
        {
            ElectionResultId = ElectionResultId,
            Results =
            {
                new EnterProportionalElectionUnmodifiedListResultRequest
                {
                    ListId = List01aId,
                    VoteCount = 3,
                },
                new EnterProportionalElectionUnmodifiedListResultRequest
                {
                    ListId = List09Id,
                    VoteCount = 3,
                },
                new EnterProportionalElectionUnmodifiedListResultRequest
                {
                    ListId = List10Id,
                    VoteCount = 4,
                },
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionUnmodifiedListResultsEntered>().Should().HaveCount(1);
        await RunAllEvents();

        await EnterBundles();

        await electionResultService.SubmissionFinishedAsync(new ProportionalElectionResultSubmissionFinishedRequest
        {
            ElectionResultId = ElectionResultId,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultSubmissionFinished>().Should().HaveCount(1);
        await RunAllEvents();

        var monitoringResultService = CreateService<ProportionalElectionResultService.ProportionalElectionResultServiceClient>(
            RolesMockedData.MonitoringElectionAdmin);
        await monitoringResultService.AuditedTentativelyAsync(new ProportionalElectionResultAuditedTentativelyRequest
        {
            ElectionResultIds = { ElectionResultId },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultAuditedTentatively>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultPublished>().Should().HaveCount(1);
        await RunAllEvents();

        await monitoringResultService.StartEndResultMandateDistributionAsync(new StartProportionalElectionEndResultMandateDistributionRequest
        {
            ProportionalElectionId = ProportionalElectionId,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionEndResultMandateDistributionStarted>().Should().HaveCount(1);
        await RunAllEvents();
    }

    private async Task EnterBundles()
    {
        var resultBundleService = CreateService<ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient>(
            RolesMockedData.ErfassungElectionAdmin);
        var resultBundleReviewerService = CreateService<ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient>(
            tenantId: TestDefaults.TenantId,
            userId: "reviewer",
            RolesMockedData.ErfassungElectionAdmin);

        // This bundle should have 2 ballots, each with all list candidates except the last one
        var bundle01b = await resultBundleService.CreateBundleAsync(new CreateProportionalElectionResultBundleRequest
        {
            ElectionResultId = ElectionResultId,
            ListId = List01bId,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleNumberEntered>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleCreated>().Should().HaveCount(1);
        await RunAllEvents();

        for (var i = 0; i < 2; i++)
        {
            await resultBundleService.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
            {
                BundleId = bundle01b.BundleId,
                Candidates = { CopyCandidateBallotPositionsFromList(List01bId).SkipLast(1) },
            });
            EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
            EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBallotCreated>().Should().HaveCount(1);
            await RunAllEvents();
        }

        await FinishBundle(resultBundleService, resultBundleReviewerService, bundle01b.BundleId);

        // This bundle should have 2 ballots, each with all list candidates except the last replaced
        // Once with candidate 12 from list 01d, once with candidate 11
        var bundle01c = await resultBundleService.CreateBundleAsync(new CreateProportionalElectionResultBundleRequest
        {
            ElectionResultId = ElectionResultId,
            ListId = List01cId,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleNumberEntered>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleCreated>().Should().HaveCount(1);
        await RunAllEvents();

        await resultBundleService.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
        {
            BundleId = bundle01c.BundleId,
            Candidates =
            {
                CopyCandidateBallotPositionsFromList(List01cId)
                    .SkipLast(1)
                    .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        CandidateId = Candidate01dP12Id,
                        Position = NumberOfMandates,
                    }),
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBallotCreated>().Should().HaveCount(1);
        await RunAllEvents();

        await resultBundleService.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
        {
            BundleId = bundle01c.BundleId,
            Candidates =
            {
                CopyCandidateBallotPositionsFromList(List01cId)
                    .SkipLast(1)
                    .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        CandidateId = Candidate01dP11Id,
                        Position = NumberOfMandates,
                    }),
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBallotCreated>().Should().HaveCount(1);
        await RunAllEvents();

        await FinishBundle(resultBundleService, resultBundleReviewerService, bundle01c.BundleId);

        // This bundle should have 2 ballots, each with all list candidates except the two replaced
        // Once with candidate 01 from list 02b, once with candidate 02 (the candidate is listed twice each time)
        var bundle02a = await resultBundleService.CreateBundleAsync(new CreateProportionalElectionResultBundleRequest
        {
            ElectionResultId = ElectionResultId,
            ListId = List02aId,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleNumberEntered>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleCreated>().Should().HaveCount(1);
        await RunAllEvents();

        await resultBundleService.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
        {
            BundleId = bundle02a.BundleId,
            Candidates =
            {
                CopyCandidateBallotPositionsFromList(List02aId)
                    .SkipLast(2)
                    .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        CandidateId = Candidate02bP01Id,
                        Position = NumberOfMandates - 1,
                    })
                    .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        CandidateId = Candidate02bP01Id,
                        Position = NumberOfMandates,
                    }),
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBallotCreated>().Should().HaveCount(1);
        await RunAllEvents();

        await resultBundleService.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
        {
            BundleId = bundle02a.BundleId,
            Candidates =
            {
                CopyCandidateBallotPositionsFromList(List02aId)
                    .SkipLast(2)
                    .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        CandidateId = Candidate02bP02Id,
                        Position = NumberOfMandates - 1,
                    })
                    .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        CandidateId = Candidate02bP02Id,
                        Position = NumberOfMandates,
                    }),
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBallotCreated>().Should().HaveCount(1);
        await RunAllEvents();

        await FinishBundle(resultBundleService, resultBundleReviewerService, bundle02a.BundleId);

        // This bundle should have 2 ballots, each with all list candidates except the two replaced
        // Both times with candidate 01 and 02 from list 02c
        var bundle02c = await resultBundleService.CreateBundleAsync(new CreateProportionalElectionResultBundleRequest
        {
            ElectionResultId = ElectionResultId,
            ListId = List02cId,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleNumberEntered>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleCreated>().Should().HaveCount(1);
        await RunAllEvents();

        for (var i = 0; i < 2; i++)
        {
            await resultBundleService.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
            {
                BundleId = bundle02c.BundleId,
                Candidates =
                {
                    CopyCandidateBallotPositionsFromList(List02cId)
                        .SkipLast(2)
                        .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                        {
                            CandidateId = Candidate02cP01Id,
                            Position = NumberOfMandates - 1,
                        })
                        .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                        {
                            CandidateId = Candidate02cP02Id,
                            Position = NumberOfMandates,
                        }),
                },
            });
            EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
            EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBallotCreated>().Should().HaveCount(1);
            await RunAllEvents();
        }

        await FinishBundle(resultBundleService, resultBundleReviewerService, bundle02c.BundleId);

        // This bundle should have 4 ballots
        var bundleNoList = await resultBundleService.CreateBundleAsync(new CreateProportionalElectionResultBundleRequest
        {
            ElectionResultId = ElectionResultId,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(2);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleNumberEntered>().Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleCreated>().Should().HaveCount(1);
        await RunAllEvents();

        await resultBundleService.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
        {
            BundleId = bundleNoList.BundleId,
            Candidates = { CopyCandidateBallotPositionsFromList(List11Id) },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBallotCreated>().Should().HaveCount(1);
        await RunAllEvents();

        for (var i = 0; i < 2; i++)
        {
            await resultBundleService.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
            {
                BundleId = bundleNoList.BundleId,
                Candidates =
                {
                    CopyCandidateBallotPositionsFromList(List03bId)
                        .SkipLast(2)
                        .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                        {
                            CandidateId = Candidate03aP01Id,
                            Position = NumberOfMandates - 1,
                        })
                        .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                        {
                            CandidateId = Candidate03aP02Id,
                            Position = NumberOfMandates,
                        }),
                },
            });
            EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
            EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBallotCreated>().Should().HaveCount(1);
            await RunAllEvents();
        }

        await resultBundleService.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
        {
            BundleId = bundleNoList.BundleId,
            Candidates =
            {
                CopyCandidateBallotPositionsFromList(List03bId)
                    .SkipLast(2)
                    .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        CandidateId = Candidate03aP01Id,
                        Position = NumberOfMandates - 1,
                    })
                    .Append(new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        CandidateId = Candidate03aP01Id,
                        Position = NumberOfMandates,
                    }),
            },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBallotCreated>().Should().HaveCount(1);
        await RunAllEvents();

        await FinishBundle(resultBundleService, resultBundleReviewerService, bundleNoList.BundleId);
    }

    private async Task FinishBundle(
        ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient resultBundleService,
        ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient resultBundleReviewerService,
        string bundleId)
    {
        await resultBundleService.BundleSubmissionFinishedAsync(new ProportionalElectionResultBundleSubmissionFinishedRequest
        {
            BundleId = bundleId,
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleSubmissionFinished>().Should().HaveCount(1);
        await RunAllEvents();

        await resultBundleReviewerService.SucceedBundleReviewAsync(new SucceedProportionalElectionBundleReviewRequest
        {
            BundleIds = { bundleId },
        });
        EventPublisherMock.AllPublishedEvents.Should().HaveCount(1);
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultBundleReviewSucceeded>().Should().HaveCount(1);
        await RunAllEvents();
    }

    private IEnumerable<CreateUpdateProportionalElectionResultBallotCandidateRequest> CopyCandidateBallotPositionsFromList(string listId)
    {
        var candidates = Lists.First(l => l.Id == listId).Candidates;
        return candidates
            .Select(c => new CreateUpdateProportionalElectionResultBallotCandidateRequest
            {
                CandidateId = c.Id,
                Position = c.Position,
                OnList = true,
            })
            .Concat(candidates
                .Where(c => c.AccumulatedPosition.HasValue)
                .Select(c => new CreateUpdateProportionalElectionResultBallotCandidateRequest
                {
                    CandidateId = c.Id,
                    Position = c.AccumulatedPosition!.Value,
                    OnList = true,
                }))
            .OrderBy(x => x.Position);
    }

    private EventInfo GetMockedBasisEventInfo()
    {
        return new EventInfo
        {
            Timestamp = new Timestamp
            {
                Seconds = _mockedBasisEventInfoSeconds++,
            },
            Tenant = new EventInfoTenant
            {
                Id = SecureConnectTestDefaults.MockedTenantDefault.Id,
                Name = SecureConnectTestDefaults.MockedTenantDefault.Name,
            },
            User = new EventInfoUser
            {
                Id = SecureConnectTestDefaults.MockedUserDefault.Loginid,
                FirstName = SecureConnectTestDefaults.MockedUserDefault.Firstname,
                LastName = SecureConnectTestDefaults.MockedUserDefault.Lastname,
                Username = SecureConnectTestDefaults.MockedUserDefault.Username,
            },
        };
    }

    private record ListTestData(string Id, string Number, string Name, ListExpectedResult ExpectedResult, List<CandidateTestData> Candidates);

    private record ListExpectedResult(
        int CandidateVotesFromUnmodifiedLists,
        int VotesFromBlankRowsOfUnmodifiedLists,
        int CandidateVotesFromModifiedLists,
        int VotesFromBlankRowsOfModifiedLists,
        int TotalVoteCount,
        int CountOfUnmodifiedLists,
        int CountOfModifiedLists);

    private record CandidateTestData(string Id, int Position, int? AccumulatedPosition, CandidateExpectedResult ExpectedResult);

    private record CandidateExpectedResult(
        int VotesFromUnmodifiedLists,
        int VotesFromModifiedLists,
        int TotalVoteCount,
        int VotesFromOtherLists,
        int VotesFromAccumulations);
}
