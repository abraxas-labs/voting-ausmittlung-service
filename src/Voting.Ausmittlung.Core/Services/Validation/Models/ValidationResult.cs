// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Models;

public class ValidationResult
{
    public ValidationResult(SharedProto.Validation validation, bool isValid, object? data = null, bool isOptional = false)
    {
        Validation = validation;
        IsValid = isValid;
        Data = data;
        IsOptional = isOptional;
    }

    public SharedProto.Validation Validation { get; }

    public bool IsValid { get; }

    public bool IsOptional { get; }

    /// <summary>
    /// Gets additional validation data which will be mapped to a proto message.
    /// It requires a corresponding ValidationProtoMessage which is defined in the oneof data field on the
    /// ValidationResult message ProtoModels.
    /// </summary>
    public object? Data { get; }
}
