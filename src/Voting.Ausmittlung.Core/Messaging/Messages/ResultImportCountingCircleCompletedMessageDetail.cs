// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Messaging.Messages;

public record ResultImportCountingCircleCompletedMessageDetail(ResultImportType ImportType, bool HasWriteIns);
