// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Globalization;
using System.Linq.Expressions;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Maps;

public sealed class WabstiCWPGemeindenBfsClassMap : ClassMap<WabstiCWPGemeindenBfsRenderService.Data>
{
    public WabstiCWPGemeindenBfsClassMap()
    {
        AutoMap(CultureInfo.InvariantCulture);
        ShowIfAuditedTentativeOrPlausibilized(x => x.TotalCountOfVoters);
        ShowIfAuditedTentativeOrPlausibilized(x => x.TotalReceivedBallots);
        ShowIfAuditedTentativeOrPlausibilized(x => x.CountOfAccountedBallots);
        ShowIfAuditedTentativeOrPlausibilized(x => x.CountOfBlankBallots);
        ShowIfAuditedTentativeOrPlausibilized(x => x.CountOfInvalidBallots);
        ShowIfAuditedTentativeOrPlausibilized(x => x.TotalCountOfModifiedLists);
        ShowIfAuditedTentativeOrPlausibilized(x => x.TotalCountOfUnmodifiedLists);
        ShowIfAuditedTentativeOrPlausibilized(x => x.TotalCountOfListsWithParty);
        ShowIfAuditedTentativeOrPlausibilized(x => x.TotalCountOfListsWithoutParty);
        ShowIfAuditedTentativeOrPlausibilized(x => x.CountOfVotersTotalSwissAbroad);
        ShowIfAuditedTentativeOrPlausibilized(x => x.VotingCardsBallotBox);
        ShowIfAuditedTentativeOrPlausibilized(x => x.VotingCardsPaper);
        ShowIfAuditedTentativeOrPlausibilized(x => x.VotingCardsByMail);
        ShowIfAuditedTentativeOrPlausibilized(x => x.VotingCardsByMailNotValid);
        ShowIfAuditedTentativeOrPlausibilized(x => x.VotingCardsEVoting);
        ShowIfAuditedTentativeOrPlausibilized(x => x.VoterParticipation, new WabstiCPercentageConverter());
    }

    private void ShowIfAuditedTentativeOrPlausibilized<T>(
        Expression<Func<WabstiCWPGemeindenBfsRenderService.Data, T>> expr, ITypeConverter? typeConverter = null)
    {
        Map(expr).Convert(row =>
        {
            if (!row.Value.IsAuditedTentativelyOrPlausibilised)
            {
                return string.Empty;
            }

            var value = expr.Compile()(row.Value)!;
            return typeConverter == null
                ? value.ToString()
                : typeConverter.ConvertToString(value, null, null);
        });
    }
}
