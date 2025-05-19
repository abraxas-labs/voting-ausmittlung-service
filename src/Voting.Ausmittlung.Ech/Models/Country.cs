// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Ech.Models;

[Serializable]
public class Country
{
    [XmlElement("id")]
    public int Id { get; set; }

    [XmlElement("iso2Id")]
    public string IsoId { get; set; } = string.Empty;

    [XmlElement("shortNameDe")]
    public string Description { get; set; } = string.Empty;

    [XmlElement("entryValid")]
    public bool EntryValid { get; set; }

    [XmlElement("recognizedCh")]
    public bool RecognizedCh { get; set; }
}
