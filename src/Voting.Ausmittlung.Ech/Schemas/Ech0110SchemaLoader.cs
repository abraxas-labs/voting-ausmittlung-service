// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace Voting.Ausmittlung.Ech.Schemas;

public static class Ech0110SchemaLoader
{
    [SuppressMessage(
        "SonarQube",
        "S5332: Using http protocol is insecure. Use https instead.",
        Justification = "These URLs are only used as XML namespaces")]
    private static readonly Dictionary<string, string> Schemas = new()
    {
        ["http://www.ech.ch/xmlns/eCH-0110/4"] = "eCH-0110-4-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0222/1"] = "eCH-0222-1-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0155/4"] = "eCH-0155-4-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0058/5"] = "eCH-0058-5-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0044/4"] = "eCH-0044-4-1.xsd",
        ["http://www.ech.ch/xmlns/eCH-0010/6"] = "eCH-0010-6-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0008/3"] = "eCH-0008-3-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0007/6"] = "eCH-0007-6-0.xsd",
        ["http://www.ech.ch/xmlns/eCH-0006/2"] = "eCH-0006-2-0.xsd",
    };

    public static XmlSchemaSet LoadEch0110Schemas()
    {
        var schemaDirectory = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Schemas");
        var xmlSchemaSet = new XmlSchemaSet();
        foreach (var (schemaName, schemaFileName) in Schemas)
        {
            var schemaPath = Path.Join(schemaDirectory, schemaFileName);
            using var xmlReader = XmlReader.Create(schemaPath);
            xmlSchemaSet.Add(schemaName, xmlReader);
        }

        return xmlSchemaSet;
    }
}
