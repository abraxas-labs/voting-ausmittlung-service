// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.ModelBuilders;

public class CertificateModelBuilder :
    IEntityTypeConfiguration<CantonSettingsECountingCertificateChain>,
    IEntityTypeConfiguration<CantonSettingsECountingCertificateInfo>
{
    public void Configure(EntityTypeBuilder<CantonSettingsECountingCertificateChain> builder)
    {
        builder
            .ComplexProperty(d => d.ImportedBy);

        builder
            .Property(d => d.ImportedAt)
            .HasUtcConversion();
    }

    public void Configure(EntityTypeBuilder<CantonSettingsECountingCertificateInfo> builder)
    {
        builder
            .HasOne(x => x.CertificateChain)
            .WithMany(x => x.Infos)
            .HasForeignKey(x => x.CertificateChainId)
            .IsRequired();

        builder
            .Property(d => d.NotBefore)
            .HasUtcConversion();

        builder
            .Property(d => d.NotAfter)
            .HasUtcConversion();
    }
}
