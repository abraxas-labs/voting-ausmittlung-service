// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Linq;

namespace Voting.Ausmittlung.Test.Utils;

internal static class XmlUtil
{
    public static string FormatTestXml(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            return doc.ToString();
        }
        catch (Exception)
        {
            return xml;
        }
    }
}
