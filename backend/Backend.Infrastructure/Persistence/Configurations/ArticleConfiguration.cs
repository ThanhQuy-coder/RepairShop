using RepairShop.Domain.Modules.Content;
using RepairShop.Domain.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Content).IsRequired();
        builder.Property(a => a.ImageUrl).HasMaxLength(500);
        builder.Property(a => a.IsPublished).HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<User>().WithMany().HasForeignKey(a => a.AuthorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => a.IsPublished);
    }
}