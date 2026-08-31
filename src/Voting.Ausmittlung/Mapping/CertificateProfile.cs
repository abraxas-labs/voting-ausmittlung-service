// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class CertificateProfile : Profile
{
    public CertificateProfile()
    {
        CreateMap<CantonSettingsECountingCertificateInfo, ProtoModels.CertificateInfo>();
        CreateMap<ResultImportECountingClientCertificateInfo, ProtoModels.CertificateInfo>();
    }
}
