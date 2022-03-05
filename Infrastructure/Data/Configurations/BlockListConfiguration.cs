using Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class BlockUserConfiguration : IEntityTypeConfiguration<ServerUser>
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
    }
}
