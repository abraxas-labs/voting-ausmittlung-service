// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Report.EventLogs;
using Voting.Ausmittlung.Report.EventLogs.Aggregates;
using Voting.Ausmittlung.Report.EventLogs.EventProcessors;
using Voting.Ausmittlung.Report.Services;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Xml;
using Voting.Lib.DmDoc.Configuration;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IReportingServiceCollection AddReport(
        this IServiceCollection services,
        DmDocConfig dmDocConfig)
    {
        services.AddScoped<TemplateService>();
        services.AddScoped<ExportService>();
        services.AddScoped<CsvService>();

        services.AddScoped<MajorityElectionDomainOfInfluenceResultBuilder>();
        services.AddScoped<VoteDomainOfInfluenceResultBuilder>();
        services.AddScoped<ProportionalElectionUnionEndResultBuilder>();
        services.AddScoped<WabstiCContestDetailsAttacher>();
        services.AddScoped<MultiLanguageTranslationUtil>();

        services.AddDmDoc(dmDocConfig);
        services.AddSingleton<IPdfService, DmDocPdfService>();

        services.AddEventLog();

        return new ReportingServiceCollection(services)
            .AddCsvContestRenderServices()
            .AddWabstiCRenderServices()
            .AddCsvVoteResultRenderServices()
            .AddCsvProportionalElectionResultRenderServices()
            .AddCsvProportionalElectionUnionResultRenderServices()
            .AddPdfContestRenderServices()
            .AddPdfVoteResultRenderServices()
            .AddPdfMajorityElectionResultRenderServices()
            .AddPdfProportionalElectionResultRenderServices()
            .AddPdfProportionalElectionUnionResultRenderServices()
            .AddXmlRenderServices();
    }

    private static IReportingServiceCollection AddCsvContestRenderServices(this IReportingServiceCollection services)
    {
        return services.AddRendererService<CsvContestActivityProtocolRenderService>(AusmittlungCsvContestTemplates.ActivityProtocol);
    }

    private static IReportingServiceCollection AddCsvProportionalElectionResultRenderServices(this IReportingServiceCollection services)
    {
        return services
            .AddRendererService<CsvProportionalElectionCandidatesCountingCircleResultsWithVoteSourcesResultService>(
                AusmittlungCsvProportionalElectionTemplates.CandidateCountingCircleResultsWithVoteSources)
            .AddRendererService<CsvProportionalElectionCandidatesAlphabeticalRenderService>(
                AusmittlungCsvProportionalElectionTemplates.CandidatesAlphabetical)
            .AddRendererService<CsvProportionalElectionCandidatesNumericalRenderService>(
                AusmittlungCsvProportionalElectionTemplates.CandidatesNumerical);
    }

    private static IReportingServiceCollection AddCsvProportionalElectionUnionResultRenderServices(this IReportingServiceCollection services)
    {
        return services
            .AddRendererService<CsvProportionalElectionUnionPartyMandatesRenderService>(AusmittlungCsvProportionalElectionUnionTemplates.PartyMandates)
            .AddRendererService<CsvProportionalElectionUnionPartyVotesRenderService>(AusmittlungCsvProportionalElectionUnionTemplates.PartyVotes)
            .AddRendererService<CsvProportionalElectionUnionVoterParticipationRenderService>(AusmittlungCsvProportionalElectionUnionTemplates.VoterParticipation);
    }

    private static IReportingServiceCollection AddCsvVoteResultRenderServices(this IReportingServiceCollection services)
    {
        return services
            .AddRendererService<CsvVoteEVotingDetailsResultService>(AusmittlungCsvVoteTemplates.EVotingDetails);
    }

    private static IReportingServiceCollection AddWabstiCRenderServices(this IReportingServiceCollection services)
    {
        return services
            .AddRendererService<WabstiCWMWahlRenderService>(AusmittlungWabstiCTemplates.WMWahl)
            .AddRendererService<WabstiCWMWahlergebnisseRenderService>(AusmittlungWabstiCTemplates.WMWahlergebnisse)
            .AddRendererService<WabstiCWMGemeindenRenderService>(AusmittlungWabstiCTemplates.WMGemeinden)
            .AddRendererService<WabstiCWMStaticGemeindenRenderService>(AusmittlungWabstiCTemplates.WMStaticGemeinden)
            .AddRendererService<WabstiCWMKandidatenRenderService>(AusmittlungWabstiCTemplates.WMKandidaten)
            .AddRendererService<WabstiCWMKandidatenGdeRenderService>(AusmittlungWabstiCTemplates.WMKandidatenGde)
            .AddRendererService<WabstiCWPWahlRenderService>(AusmittlungWabstiCTemplates.WPWahl, AusmittlungWabstiCTemplates.WPWahlEinzel)
            .AddRendererService<WabstiCWPGemeindenRenderService>(AusmittlungWabstiCTemplates.WPGemeinden, AusmittlungWabstiCTemplates.WPGemeindenEinzel)
            .AddRendererService<WabstiCWPGemeindenSkStatRenderService>(AusmittlungWabstiCTemplates.WPGemeindenSkStat, AusmittlungWabstiCTemplates.WPGemeindenSkStatEinzel)
            .AddRendererService<WabstiCWPGemeindenBfsRenderService>(AusmittlungWabstiCTemplates.WPGemeindenBfs, AusmittlungWabstiCTemplates.WPGemeindenBfsEinzel)
            .AddRendererService<WabstiCWPStaticGemeindenRenderService>(AusmittlungWabstiCTemplates.WPStaticGemeinden, AusmittlungWabstiCTemplates.WPStaticGemeindenEinzel)
            .AddRendererService<WabstiCWPKandidatenRenderService>(AusmittlungWabstiCTemplates.WPKandidaten, AusmittlungWabstiCTemplates.WPKandidatenEinzel)
            .AddRendererService<WabstiCWPStaticKandidatenRenderService>(AusmittlungWabstiCTemplates.WPStaticKandidaten, AusmittlungWabstiCTemplates.WPStaticKandidatenEinzel)
            .AddRendererService<WabstiCWPKandidatenGdeRenderService>(AusmittlungWabstiCTemplates.WPKandidatenGde, AusmittlungWabstiCTemplates.WPKandidatenGdeEinzel)
            .AddRendererService<WabstiCWPListenRenderService>(AusmittlungWabstiCTemplates.WPListen, AusmittlungWabstiCTemplates.WPListenEinzel)
            .AddRendererService<WabstiCWPListenGdeRenderService>(AusmittlungWabstiCTemplates.WPListenGde, AusmittlungWabstiCTemplates.WPListenGdeEinzel)
            .AddRendererService<WabstiCWPListenGdeSkStatRenderService>(AusmittlungWabstiCTemplates.WPListenGdeSkStat, AusmittlungWabstiCTemplates.WPListenGdeSkStatEinzel)
            .AddRendererService<WabstiCSGStaticGeschaefteRenderService>(AusmittlungWabstiCTemplates.SGStaticGeschaefte)
            .AddRendererService<WabstiCSGGeschaefteRenderService>(AusmittlungWabstiCTemplates.SGGeschaefte)
            .AddRendererService<WabstiCSGGemeindenRenderService>(AusmittlungWabstiCTemplates.SGGemeinden)
            .AddRendererService<WabstiCSGStaticGemeindenRenderService>(AusmittlungWabstiCTemplates.SGStaticGemeinden)
            .AddRendererService<WabstiCSGAbstimmungsergebnisseRenderService>(AusmittlungWabstiCTemplates.SGAbstimmungsergebnisse);
    }

    private static IReportingServiceCollection AddPdfVoteResultRenderServices(this IReportingServiceCollection services)
    {
        return services
            .AddRendererService<PdfVoteEndResultRenderService>(AusmittlungPdfVoteTemplates.EndResultProtocol)
            .AddRendererService<PdfVoteDomainOfInfluenceResultRenderService>(
                AusmittlungPdfVoteTemplates.TemporaryEndResultDomainOfInfluencesProtocol,
                AusmittlungPdfVoteTemplates.EndResultDomainOfInfluencesProtocol)
            .AddRendererService<PdfVoteResultRenderService>(AusmittlungPdfVoteTemplates.ResultProtocol)
            .AddRendererService<PdfVoteEVotingDetailsResultRenderService>(AusmittlungPdfVoteTemplates.EVotingDetailsResultProtocol)
            .AddRendererService<PdfVoteCountingCircleEVotingResultRenderService>(AusmittlungPdfVoteTemplates.EVotingCountingCircleResultProtocol)
            .AddRendererService<PdfVoteEVotingResultRenderService>(AusmittlungPdfVoteTemplates.EVotingResultProtocol)
            .AddRendererService<PdfVoteResultBundleReviewRenderService>(AusmittlungPdfVoteTemplates.ResultBundleReview);
    }

    private static IReportingServiceCollection AddPdfMajorityElectionResultRenderServices(this IReportingServiceCollection services)
    {
        return services
            .AddRendererService<PdfMajorityElectionCountingCircleResultRenderService>(
                AusmittlungPdfMajorityElectionTemplates.CountingCircleProtocol,
                AusmittlungPdfMajorityElectionTemplates.CountingCircleEVotingProtocol)
            .AddRendererService<PdfMajorityElectionEndResultRenderService>(
                AusmittlungPdfMajorityElectionTemplates.EndResultProtocol,
                AusmittlungPdfMajorityElectionTemplates.EndResultEVotingProtocol)
            .AddRendererService<PdfMajorityElectionEndResultDetailRenderService>(
                AusmittlungPdfMajorityElectionTemplates.EndResultDetailProtocol,
                AusmittlungPdfMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol)
            .AddRendererService<PdfMajorityElectionResultBundleReviewRenderService>(AusmittlungPdfMajorityElectionTemplates.ResultBundleReview);
    }

    private static IReportingServiceCollection AddPdfContestRenderServices(this IReportingServiceCollection services)
    {
        return services.AddRendererService<PdfContestActivityProtocolRenderService>(AusmittlungPdfContestTemplates.ActivityProtocol);
    }

    private static IReportingServiceCollection AddPdfProportionalElectionUnionResultRenderServices(this IReportingServiceCollection services)
    {
        return services
            .AddRendererService<PdfProportionalElectionUnionDoubleProportionalResultRenderService>(
                AusmittlungPdfProportionalElectionTemplates.UnionEndResultQuorumUnionListDoubleProportional,
                AusmittlungPdfProportionalElectionTemplates.UnionEndResultSuperApportionmentDoubleProportional,
                AusmittlungPdfProportionalElectionTemplates.UnionEndResultSubApportionmentDoubleProportional,
                AusmittlungPdfProportionalElectionTemplates.UnionEndResultNumberOfMandatesDoubleProportional,
                AusmittlungPdfProportionalElectionTemplates.UnionEndResultCalculationDoubleProportional);
    }

    private static IReportingServiceCollection AddPdfProportionalElectionResultRenderServices(
        this IReportingServiceCollection services)
    {
        return services
            .AddRendererService<PdfProportionalElectionUnionRenderService>(
                AusmittlungPdfProportionalElectionTemplates.ListVotesPoliticalBusinessUnionEndResults)
            .AddRendererService<PdfProportionalElectionCountingCircleResultRenderService>(
                AusmittlungPdfProportionalElectionTemplates.ListVotesCountingCircleProtocol,
                AusmittlungPdfProportionalElectionTemplates.ListVotesCountingCircleEVotingProtocol,
                AusmittlungPdfProportionalElectionTemplates.ListsCountingCircleProtocol)
            .AddRendererService<PdfProportionalElectionCandidatesCountingCircleResultRenderService>(
                AusmittlungPdfProportionalElectionTemplates.ListCandidateVotesCountingCircleProtocol,
                AusmittlungPdfProportionalElectionTemplates.ListCandidateEmptyVotesCountingCircleProtocol,
                AusmittlungPdfProportionalElectionTemplates.ListCandidateEmptyVotesCountingCircleEVotingProtocol)
            .AddRendererService<PdfProportionalElectionCandidateVoteSourcesCountingCircleResultRenderService>(
                AusmittlungPdfProportionalElectionTemplates.ListCandidateVoteSourcesCountingCircleProtocol)
            .AddRendererService<PdfProportionalElectionEndResultRenderService>(
                AusmittlungPdfProportionalElectionTemplates.ListVotesEndResults)
            .AddRendererService<PdfProportionalElectionCandidatesEndResultRenderService>(
                AusmittlungPdfProportionalElectionTemplates.ListCandidateVotesEndResults,
                AusmittlungPdfProportionalElectionTemplates.ListCandidateEndResults,
                AusmittlungPdfProportionalElectionTemplates.ListCandidateEndResultsEVoting)
            .AddRendererService<PdfProportionalElectionCandidateVoteSourcesEndResultRenderService>(
                AusmittlungPdfProportionalElectionTemplates.ListCandidateVoteSourcesEndResults)
            .AddRendererService<PdfProportionalElectionEndResultCalculationRenderService>(
                AusmittlungPdfProportionalElectionTemplates.EndResultCalculation)
            .AddRendererService<PdfProportionalElectionEndResultListUnionsRenderService>(
                AusmittlungPdfProportionalElectionTemplates.EndResultListUnions,
                AusmittlungPdfProportionalElectionTemplates.EndResultListUnionsEVoting)
            .AddRendererService<PdfProportionalElectionResultBundleReviewRenderService>(
                AusmittlungPdfProportionalElectionTemplates.ResultBundleReview)
            .AddRendererService<PdfProportionalElectionDoubleProportionalResultRenderService>(
                AusmittlungPdfProportionalElectionTemplates.EndResultDoubleProportional);
    }

    private static IReportingServiceCollection AddXmlRenderServices(this IReportingServiceCollection services)
    {
        return services
            .AddRendererService<XmlVoteEch0110RenderService>(AusmittlungXmlVoteTemplates.Ech0110)
            .AddRendererService<XmlMajorityElectionEch0110RenderService>(AusmittlungXmlMajorityElectionTemplates.Ech0110)
            .AddRendererService<XmlProportionalElectionEch0110RenderService>(AusmittlungXmlProportionalElectionTemplates.Ech0110)
            .AddRendererService<XmlMajorityElectionEch0222RenderService>(AusmittlungXmlMajorityElectionTemplates.Ech0222)
            .AddRendererService<XmlProportionalElectionEch0222RenderService>(AusmittlungXmlProportionalElectionTemplates.Ech0222)
            .AddRendererService<XmlVoteEch0222RenderService>(AusmittlungXmlVoteTemplates.Ech0222)
            .AddRendererService<XmlEch0252ProportionalElectionRenderService>(AusmittlungXmlContestTemplates.ProportionalElectionsEch0252)
            .AddRendererService<XmlEch0252MajorityElectionRenderService>(AusmittlungXmlContestTemplates.MajorityElectionsEch0252)
            .AddRendererService<XmlEch0252VoteRenderService>(AusmittlungXmlContestTemplates.VoteEch0252);
    }

    private static IServiceCollection AddEventLog(this IServiceCollection services)
    {
        // event log uses event sourcing aggregates to read basis events.
        services.Scan(scan => scan.FromAssemblyOf<EventLog>()
            .AddClasses(classes => classes.AssignableTo(typeof(BaseEventSourcingAggregate)))
            .AsSelf()
            .WithTransientLifetime());

        services.Scan(scan => scan.FromAssemblyOf<EventLog>()
            .AddClasses(classes => classes.AssignableTo(typeof(AggregateSet<>)))
            .AsSelf()
            .WithTransientLifetime());

        services.AddTransient<MajorityElectionAggregateSet>();

        return services
            .AddSingleton<EventLogInitializerAdapterRegistry>()
            .AddScoped<EventLogBuilderContextBuilder>()
            .AddScoped<EventLogsBuilder>()
            .AddScoped<EventLogBuilder>()
            .AddScoped<EventLogEventSignatureVerifier>()
            .AddReportEventProcessors();
    }

    private static IServiceCollection AddReportEventProcessors(this IServiceCollection services)
    {
        services.Scan(scan =>
            scan.FromAssemblyOf<EventLogsBuilder>()
                .AddClasses(classes => classes.AssignableTo(typeof(IReportEventProcessor<>)))
                .AsImplementedInterfaces()
                .WithSingletonLifetime());

        var adapterTypes = services
            .Where(s => s.ServiceType.IsGenericType && s.ServiceType.GetGenericTypeDefinition() == typeof(IReportEventProcessor<>))
            .Select(s => typeof(ReportEventProcessorAdapter<>).MakeGenericType(s.ServiceType.GetGenericArguments()[0]))
            .ToList();

        foreach (var adapter in adapterTypes)
        {
            services.AddSingleton(adapter);
        }

        return services;
    }
}
