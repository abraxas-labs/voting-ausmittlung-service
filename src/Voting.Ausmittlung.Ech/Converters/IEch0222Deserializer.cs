// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using Voting.Ausmittlung.Ech.Models;

namespace Voting.Ausmittlung.Ech.Converters;

public interface IEch0222Deserializer
{
    VotingImport DeserializeXml(Stream stream);
}
