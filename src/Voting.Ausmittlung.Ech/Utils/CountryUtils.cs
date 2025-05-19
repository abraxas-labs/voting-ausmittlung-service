// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Voting.Ausmittlung.Ech.Models;

namespace Voting.Ausmittlung.Ech.Utils;

public static class CountryUtils
{
    public const int SwissCountryId = 8100;
    public const string SwissCountryIso = "CH";
    public const string SwissCountryNameShort = "Schweiz";

    private const string BfsCountryListFile = "Voting.Ausmittlung.Ech.Files.Utils.BFSCountryList.xml";
    private static readonly List<Country> _countries = GetCountryList();

    public static Country? GetCountryFromIsoId(string isoId)
    {
        return _countries.Find(x => x.IsoId.Equals(isoId, StringComparison.InvariantCultureIgnoreCase));
    }

    private static List<Country> GetCountryList()
    {
        var serializer = new XmlSerializer(typeof(CountryXmlRootModel));
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(BfsCountryListFile)
                           ?? throw new FileNotFoundException(BfsCountryListFile);

        using var reader = new StreamReader(stream);
        var rootModel = serializer.Deserialize(reader) as CountryXmlRootModel;

        ArgumentNullException.ThrowIfNull(rootModel?.Country);

        return rootModel.Country
            .Where(x => x.EntryValid && x.RecognizedCh)
            .OrderBy(x => x.Description)
            .ToList();
    }
}
