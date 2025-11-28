// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Ech;

namespace Voting.Ausmittlung.Ech.Utils;

public static class Ech0222VersionFinder
{
    private const string Ech0222V3Scheme = "http://www.ech.ch/xmlns/eCH-0222/3";
    private const string Ech0222V1Scheme = "http://www.ech.ch/xmlns/eCH-0222/1";

    public static Ech0222Version GetEch0222Version(Stream ech0222Stream)
    {
        var scheme = EchSchemaFinder.GetSchema(ech0222Stream, new[] { Ech0222V1Scheme, Ech0222V3Scheme });

        if (string.IsNullOrWhiteSpace(scheme))
        {
            throw new InvalidOperationException("Cannot determine the Ech-0222 Version from the input file");
        }

        return scheme == Ech0222V1Scheme
            ? Ech0222Version.V1
            : Ech0222Version.V3;
    }
}
