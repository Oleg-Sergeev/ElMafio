using Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ServerUserConfiguration : IEntityTypeConfiguration<ServerUser>
{
    public virtual void Configure(EntityTypeBuilder<ServerUser> builder)
    {
        builder.HasKey(bu => new { bu.UserId, bu.ServerId });

        builder
            .Property(bu => bu.UserId)
            .ValueGeneratedNever();
        
        builder
            .Property(bu => bu.ServerId)
            .ValueGeneratedNever();

        builder
            .HasOne(x => x.AccessLevel)
            .WithMany()
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
