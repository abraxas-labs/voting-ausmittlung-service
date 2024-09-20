// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.EventSignature;

public enum PublicKeySignatureValidationResultType
{
    Valid,
    AuthenticationTagInvalid,
    HsmSignatureInvalid,
}
