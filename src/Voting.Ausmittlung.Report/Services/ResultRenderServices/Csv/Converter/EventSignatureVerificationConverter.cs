// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Voting.Ausmittlung.Report.EventLogs;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.Converter;

public class EventSignatureVerificationConverter : TypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        return value switch
        {
            EventLogEventSignatureVerification.NoSignature => "Keine Signatur",
            EventLogEventSignatureVerification.VerificationSuccess => "Gültig",
            EventLogEventSignatureVerification.VerificationFailed => "Nicht Gültig",
            _ => string.Empty,
        };
    }
}
