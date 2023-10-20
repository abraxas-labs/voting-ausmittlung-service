// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace Voting.Ausmittlung.Report.Services;

public class CsvService
{
    private static readonly CsvConfiguration CsvConfiguration = NewCsvConfig();

    public async Task Render<TRow>(PipeWriter writer, IAsyncEnumerable<TRow> records, CancellationToken ct = default)
    {
        // use utf8 with bom (excel requires bom)
        await using var streamWriter = new StreamWriter(writer.AsStream(), Encoding.UTF8);
        await using var csvWriter = new CsvWriter(streamWriter, CsvConfiguration);
        await csvWriter.WriteRecordsAsync(records, ct);
    }

    public async Task Render<TRow>(PipeWriter writer, IEnumerable<TRow> records, Action<IWriter>? configure = null, CancellationToken ct = default)
    {
        // use utf8 with bom (excel requires bom)
        await using var streamWriter = new StreamWriter(writer.AsStream(), Encoding.UTF8);
        await using var csvWriter = new CsvWriter(streamWriter, CsvConfiguration);
        configure?.Invoke(csvWriter);
        await csvWriter.WriteRecordsAsync(records, ct);
    }

    /// <summary>
    /// Renders a csv based on objects which contain dynamic columns.
    /// The dynamic columns need to be of type <see cref="IDictionary"/>.
    /// All dynamic dictionary columns of all records need to have the same keys in the same order.
    /// (consider using <see cref="SortedDictionary{TKey,TValue}"/>).
    /// </summary>
    /// <param name="writer">The writer to write the result to.</param>
    /// <param name="records">The records.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <typeparam name="TRow">The type of the records.</typeparam>
    /// <returns>A task representing the async operation.</returns>
    public async Task RenderDynamic<TRow>(
        PipeWriter writer,
        IAsyncEnumerable<TRow> records,
        CancellationToken ct = default)
    {
        var recordsEnumerator = records.GetAsyncEnumerator(ct);
        if (!await recordsEnumerator.MoveNextAsync(ct))
        {
            return;
        }

        var csvConfig = NewCsvConfig();
        csvConfig.HasHeaderRecord = false;

        // use utf8 with bom (excel requires bom)
        await using var streamWriter = new StreamWriter(writer.AsStream(), Encoding.UTF8);
        await using var csvWriter = new CsvWriter(streamWriter, csvConfig);

        var firstRecord = recordsEnumerator.Current!;
        var map = csvWriter.Context.AutoMap(firstRecord.GetType());
        var members = new MemberMapCollection();
        members.AddMembers(map);

        var headers = new List<string>();

        foreach (var member in members)
        {
            if (typeof(IDictionary).IsAssignableFrom(member.Data.Type)
                || typeof(IDictionary<,>).IsAssignableFrom(member.Data.Type))
            {
                headers.AddRange(GetHeaders(firstRecord, member.Data.Member));
                continue;
            }

            var name = member.Data.Names.FirstOrDefault();
            if (name != null)
            {
                headers.Add(name);
            }
        }

        csvWriter.WriteDynamicHeader(new CsvDynamicHeaderProvider(headers));
        await csvWriter.NextRecordAsync();
        csvWriter.WriteRecord(firstRecord);
        await csvWriter.NextRecordAsync();

        while (await recordsEnumerator.MoveNextAsync(ct))
        {
            csvWriter.WriteRecord(recordsEnumerator.Current!);
            await csvWriter.NextRecordAsync();
            ct.ThrowIfCancellationRequested();
        }
    }

    private static CsvConfiguration NewCsvConfig() =>
        new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
        };

    private IEnumerable<string> GetHeaders<T>(T record, MemberInfo member)
    {
        var value = member switch
        {
            PropertyInfo property => property.GetValue(record, null),
            FieldInfo field => field.GetValue(record),
            _ => throw new InvalidOperationException("unexpected member info type"),
        };

        if (value == null)
        {
            yield break;
        }

        if (value is not IDictionary dict)
        {
            throw new InvalidOperationException("dynamic properties need to be of type IDictionary.");
        }

        foreach (var key in dict.Keys)
        {
            yield return key?.ToString() ?? string.Empty;
        }
    }

    private class CsvDynamicHeaderProvider : IDynamicMetaObjectProvider
    {
        private readonly IEnumerable<string> _memberNames;

        public CsvDynamicHeaderProvider(IEnumerable<string> memberNames)
        {
            _memberNames = memberNames;
        }

        public DynamicMetaObject GetMetaObject(Expression parameter) => new CsvDynamicHeaderMetaObject(_memberNames, parameter);
    }

    private class CsvDynamicHeaderMetaObject : DynamicMetaObject
    {
        private readonly IEnumerable<string> _memberNames;

        public CsvDynamicHeaderMetaObject(IEnumerable<string> memberNames, Expression parameter)
            : base(parameter, BindingRestrictions.Empty)
        {
            _memberNames = memberNames;
        }

        public override IEnumerable<string> GetDynamicMemberNames() => _memberNames;
    }
}
