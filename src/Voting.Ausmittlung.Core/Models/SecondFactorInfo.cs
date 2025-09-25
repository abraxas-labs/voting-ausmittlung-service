// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.TemporaryData.Models;

namespace Voting.Ausmittlung.Core.Models;

public record SecondFactorInfo(SecondFactorTransaction Transaction, string Code, string QrCode);
