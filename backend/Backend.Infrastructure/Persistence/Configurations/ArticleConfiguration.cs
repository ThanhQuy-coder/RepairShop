using RepairShop.Domain.Modules.Content;
using RepairShop.Domain.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepairShop.Infrastructure.Persistence.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    private static readonly Guid AdminAuthorId = Guid.Parse("11111111-1111-1111-1111-111111111111");

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

        builder.HasData(
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000201"),
            Title = "5 dấu hiệu cho thấy pin điện thoại của bạn cần được thay thế",
            Content = "Pin sụt nhanh dù không dùng nhiều ứng dụng nặng, máy nóng bất thường khi sạc, " +
                    "tự động tắt nguồn dù pin còn phần trăm, sạc đầy rất lâu hoặc pin phồng lên là những " +
                    "dấu hiệu rõ ràng nhất cho thấy pin đã chai và cần thay mới. Việc thay pin sớm không " +
                    "chỉ giúp máy hoạt động ổn định hơn mà còn tránh được rủi ro cháy nổ do pin phồng.",
            ImageUrl = (string?)null,
            AuthorId = AdminAuthorId,
            IsPublished = true,
            CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000202"),
            Title = "Hướng dẫn bảo quản laptop đúng cách để tăng tuổi thọ",
            Content = "Vệ sinh khe tản nhiệt định kỳ, tránh để laptop trên bề mặt mềm gây bí nguồn, " +
                    "không sạc pin qua đêm liên tục và hạn chế di chuyển máy khi đang hoạt động là những " +
                    "thói quen đơn giản giúp laptop bền hơn. Ngoài ra, nên tra keo tản nhiệt định kỳ 12-18 " +
                    "tháng/lần để tránh tình trạng máy nóng và giảm hiệu năng theo thời gian.",
            ImageUrl = (string?)null,
            AuthorId = AdminAuthorId,
            IsPublished = true,
            CreatedAt = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000203"),
            Title = "Vì sao không nên tự sửa chữa thiết bị điện tử tại nhà",
            Content = "Tự tháo máy khi không có đủ dụng cụ chuyên dụng dễ làm hỏng các chân kết nối nhỏ, " +
                    "gãy cáp màn hình hoặc làm mất ốc vít. Ngoài ra, việc thao tác sai cách với pin lithium " +
                    "có thể gây cháy nổ nguy hiểm. Mang thiết bị đến kỹ thuật viên có kinh nghiệm giúp chẩn " +
                    "đoán đúng lỗi và tránh phát sinh thêm hư hỏng không đáng có.",
            ImageUrl = (string?)null,
            AuthorId = AdminAuthorId,
            IsPublished = true,
            CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000204"),
            Title = "Quy trình tiếp nhận và sửa chữa tại RepairShop",
            Content = "Khi mang thiết bị đến, nhân viên sẽ ghi nhận tình trạng máy, chụp ảnh hiện trạng và " +
                    "cấp mã phiếu để bạn tra cứu tiến độ. Sau khi kỹ thuật viên chẩn đoán xong, cửa hàng sẽ " +
                    "gửi báo giá để bạn xác nhận trước khi tiến hành sửa chữa. Toàn bộ quá trình đều minh " +
                    "bạch và có thể theo dõi trực tuyến bằng mã phiếu, không phát sinh chi phí ngoài báo giá.",
            ImageUrl = (string?)null,
            AuthorId = AdminAuthorId,
            IsPublished = true,
            CreatedAt = new DateTime(2026, 1, 18, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        },
        new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000205"),
            Title = "[Nháp] Sắp ra mắt dịch vụ nhận sửa chữa tận nơi",
            Content = "Bài viết đang soạn thảo, sẽ công bố chi tiết dịch vụ nhận và trả thiết bị tận nơi " +
                    "trong khu vực nội thành. Nội dung sẽ được cập nhật trước khi xuất bản chính thức.",
            ImageUrl = (string?)null,
            AuthorId = AdminAuthorId,
            IsPublished = false, // bản nháp — dùng để demo tính năng Publish/Unpublish (Task 7.11)
            CreatedAt = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = (DateTime?)null,
            DeletedAt = (DateTime?)null,
        });
    }
}