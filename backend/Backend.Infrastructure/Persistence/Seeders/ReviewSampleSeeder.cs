using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Common;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Billing.Enums;
using RepairShop.Domain.Modules.Customers;
using RepairShop.Domain.Modules.Devices;
using RepairShop.Domain.Modules.Quotes;
using RepairShop.Domain.Modules.Quotes.Enums;
using RepairShop.Domain.Modules.Tickets;

namespace RepairShop.Infrastructure.Persistence.Seeders;

public class ReviewSampleSeeder : IDataSeeder
{
    private readonly AppDbContext _context;

    public ReviewSampleSeeder(AppDbContext context) => _context = context;

    public int Order => 50;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Avoid adding sample tickets inside the automated integration test container unless explicitly empty
        var dbName = _context.Database.GetDbConnection().Database;
        if (string.Equals(dbName, "repairshop_test", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await SeedSampleReviewsAsync(cancellationToken);
    }

    public async Task SeedSampleReviewsAsync(CancellationToken cancellationToken = default)
    {
        var adminUser = await _context.Users
            .FirstOrDefaultAsync(u => u.DeletedAt == null, cancellationToken);
        if (adminUser is null) return;

        var statuses = await _context.RepairStatuses
            .ToDictionaryAsync(s => s.Code, s => s, cancellationToken);

        if (!statuses.TryGetValue(RepairStatusCodes.CheckedIn, out var checkedIn) ||
            !statuses.TryGetValue(RepairStatusCodes.Assigned, out var assigned) ||
            !statuses.TryGetValue(RepairStatusCodes.Diagnosing, out var diagnosing) ||
            !statuses.TryGetValue(RepairStatusCodes.WaitingApproval, out var waitingApproval) ||
            !statuses.TryGetValue(RepairStatusCodes.InRepair, out var inRepair) ||
            !statuses.TryGetValue(RepairStatusCodes.QaTesting, out var qaTesting) ||
            !statuses.TryGetValue(RepairStatusCodes.ReadyForPickup, out var readyForPickup) ||
            !statuses.TryGetValue(RepairStatusCodes.Delivered, out var delivered))
        {
            return;
        }

        var services = await _context.Services
            .Where(s => s.DeletedAt == null && s.IsActive)
            .ToListAsync(cancellationToken);
        if (services.Count == 0) return;

        var models = await _context.DeviceModels
            .Include(m => m.Brand)
            .Where(m => m.DeletedAt == null && m.IsActive)
            .ToListAsync(cancellationToken);

        var existingTicketCodes = (await _context.RepairTickets
            .Select(t => t.TicketCode)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var samples = GetSampleReviewSpecs();
        var anyCreated = false;

        foreach (var sample in samples)
        {
            if (existingTicketCodes.Contains(sample.TicketCode))
                continue;

            var matchedServices = sample.ServiceNames
                .Select(name => services.FirstOrDefault(s =>
                    string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) ||
                    s.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
                .Where(s => s is not null)
                .Select(s => s!)
                .DistinctBy(s => s.Id)
                .ToList();

            if (matchedServices.Count == 0)
                continue;

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == sample.CustomerPhone && c.DeletedAt == null, cancellationToken);

            if (customer is null)
            {
                customer = new Customer(sample.CustomerName, sample.CustomerPhone, sample.CustomerEmail, sample.CustomerAddress);
                await _context.Customers.AddAsync(customer, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var catalogModel = models.FirstOrDefault(m =>
                m.DeviceType == sample.DeviceType &&
                string.Equals(m.ModelName, sample.ModelName, StringComparison.OrdinalIgnoreCase));

            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.SerialNumber == sample.SerialNumber && d.DeletedAt == null, cancellationToken);

            if (device is null)
            {
                device = new Device(
                    customer.Id,
                    sample.DeviceType,
                    catalogModel?.Brand.Name ?? sample.BrandName,
                    catalogModel?.ModelName ?? sample.ModelName,
                    sample.SerialNumber,
                    catalogModel?.Id);
                await _context.Devices.AddAsync(device, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var ticket = new RepairTicket(
                sample.TicketCode,
                customer.Id,
                device.Id,
                adminUser.Id,
                sample.IssueReported,
                checkedIn);

            ticket.RecordInitialStatusHistory(adminUser.Id);
            ticket.AssignTechnician(adminUser.Id, assigned, adminUser.Id, "Phân công kỹ thuật viên Fixora");
            ticket.StartDiagnosis(diagnosing, adminUser.Id, "Bắt đầu kiểm tra thiết bị");
            ticket.SubmitDiagnosis(
                sample.DiagnosisResult,
                "Hao mòn linh kiện theo thời gian sử dụng",
                string.Join("; ", matchedServices.Select(s => s.Name)),
                null);

            var quote = new Quote(ticket.Id, $"Báo giá dịch vụ sửa chữa {device.Brand} {device.Model}", adminUser.Id);
            foreach (var svc in matchedServices)
            {
                quote.AddItem(
                    QuoteItemType.Service,
                    svc.Name,
                    1,
                    svc.BasePrice ?? 350000m,
                    partId: null,
                    serviceId: svc.Id);
            }

            ticket.AttachQuote(quote);
            ticket.SubmitQuote(waitingApproval, adminUser.Id, "Gửi báo giá cho khách hàng");
            quote.Approve();
            ticket.ApproveQuote(inRepair, adminUser.Id, "Khách hàng đồng ý báo giá");
            ticket.RecordCompletionNotes("Đã hoàn tất sửa chữa và kiểm tra ổn định các chức năng.");
            ticket.StartQualityCheck(qaTesting, adminUser.Id, "Chuyển sang kiểm tra chất lượng QA");
            ticket.PassQualityCheck(readyForPickup, adminUser.Id, "Thiết bị đạt tiêu chuẩn chất lượng Fixora");

            var invoice = ticket.CreateInvoice(PaymentMethod.Cash, quote.TotalAmount, adminUser.Id, quote.Id);
            invoice.MarkAsPaid(DateTime.UtcNow.AddDays(-sample.DaysAgo));
            ticket.Deliver(delivered, adminUser.Id, "Đã bàn giao thiết bị cho khách hàng");

            var review = ticket.CreateReview(sample.Rating, sample.Comment);

            await _context.RepairTickets.AddAsync(ticket, cancellationToken);
            await _context.Quotes.AddAsync(quote, cancellationToken);
            await _context.Invoices.AddAsync(invoice, cancellationToken);
            await _context.Reviews.AddAsync(review, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Mark all SLA entries for this completed sample ticket as Completed so SLA monitoring stays clean
            var slas = await _context.TicketSLAs
                .Where(s => s.RepairTicketId == ticket.Id && s.Status != SLAStatus.Completed)
                .ToListAsync(cancellationToken);
            foreach (var sla in slas)
            {
                sla.Complete(DateTime.UtcNow);
            }
            if (slas.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            anyCreated = true;
        }

        if (anyCreated)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record SampleReviewSpec(
        string TicketCode,
        string CustomerName,
        string CustomerPhone,
        string CustomerEmail,
        string CustomerAddress,
        DeviceType DeviceType,
        string BrandName,
        string ModelName,
        string SerialNumber,
        string IssueReported,
        string DiagnosisResult,
        string[] ServiceNames,
        int Rating,
        string Comment,
        int DaysAgo);

    private static IReadOnlyList<SampleReviewSpec> GetSampleReviewSpecs() =>
    [
        new(
            "RT-SEED-0001",
            "Nguyễn Minh Tuấn",
            "0901234501",
            "minhtuan.seed@fixora.vn",
            "Quận 1, TP. Hồ Chí Minh",
            DeviceType.Phone,
            "Apple",
            "iPhone 15 Pro Max",
            "FX-SEED-IP15PM-01",
            "Pin tụt nhanh và máy nóng khi sạc",
            "Pin chai còn 76% dung lượng chuẩn, bụi bẩn cổng sạc",
            ["Thay pin điện thoại", "Vệ sinh điện thoại & phục hồi kháng nước"],
            5,
            "Kỹ thuật viên Fixora kiểm tra rất kỹ, thay pin iPhone 15 Pro Max lấy ngay trong ngày, máy mát hẳn và có dán lại ron kháng nước.",
            12),
        new(
            "RT-SEED-0002",
            "Trần Thu Hà",
            "0901234502",
            "thuha.seed@fixora.vn",
            "Quận 3, TP. Hồ Chí Minh",
            DeviceType.Phone,
            "Samsung",
            "Samsung Galaxy S23 Ultra",
            "FX-SEED-S23U-02",
            "Rơi vỡ mặt kính trước, màn hình và bút S-Pen vẫn dùng được",
            "Nứt kính ngoài, tấm nền AMOLED và cảm ứng nguyên vẹn",
            ["Ép kính, thay mặt kính điện thoại", "Thay pin điện thoại"],
            5,
            "Ép kính Galaxy S23 Ultra cực kỳ đẹp, không bọt khí, cảm ứng mượt như mới. Báo giá minh bạch ngay từ đầu.",
            9),
        new(
            "RT-SEED-0003",
            "Lê Hoàng Nam",
            "0901234503",
            "hoangnam.seed@fixora.vn",
            "Quận Bình Thạnh, TP. Hồ Chí Minh",
            DeviceType.Phone,
            "Apple",
            "iPhone 13",
            "FX-SEED-IP13-03",
            "Màn hình bị sọc trắng sau khi va đập mạnh",
            "Hỏng tấm nền OLED cần thay màn hình mới",
            ["Thay màn hình điện thoại"],
            4,
            "Màn hình mới hiển thị sắc nét, True Tone đầy đủ. Thời gian chờ khoảng 2 tiếng đúng như lịch hẹn.",
            7),
        new(
            "RT-SEED-0004",
            "Phạm Gia Huy",
            "0901234504",
            "giahuy.seed@fixora.vn",
            "TP. Thủ Đức, TP. Hồ Chí Minh",
            DeviceType.Laptop,
            "ASUS",
            "ASUS ROG Strix G16",
            "FX-SEED-ROGG16-04",
            "Máy nóng lên 95 độ khi render và dung lượng ổ cứng đầy",
            "Keo tản nhiệt khô, bụi kín khe tản nhiệt; cần vệ sinh và nâng cấp SSD",
            ["Vệ sinh máy, tra keo tản nhiệt laptop", "Nâng cấp SSD NVMe & RAM laptop"],
            5,
            "Sau khi vệ sinh tra keo và nâng cấp SSD tại Fixora, chiếc ROG Strix G16 giảm hơn 14 độ khi full load, khởi động cực nhanh!",
            6),
        new(
            "RT-SEED-0005",
            "Võ Ngọc Ánh",
            "0901234505",
            "ngocanh.seed@fixora.vn",
            "Quận 7, TP. Hồ Chí Minh",
            DeviceType.Laptop,
            "Apple",
            "MacBook Air M2",
            "FX-SEED-MBAIR-05",
            "Pin báo Service Recommended và kẹt một số phím",
            "Pin chai sau 650 chu kỳ sạc, cụm phím bị kẹt cơ học",
            ["Thay pin laptop", "Thay bàn phím laptop"],
            5,
            "Dịch vụ chăm sóc MacBook rất chuyên nghiệp, có thể theo dõi từng bước qua mã phiếu trực tuyến.",
            5),
        new(
            "RT-SEED-0006",
            "Đặng Quốc Bảo",
            "0901234506",
            "quocbao.seed@fixora.vn",
            "Quận Phú Nhuận, TP. Hồ Chí Minh",
            DeviceType.Tablet,
            "Apple",
            "iPad Pro 11 M2",
            "FX-SEED-IPADPRO-06",
            "Nứt mặt kính khi để trong balo, cảm ứng vẫn nhạy",
            "Vỡ lớp kính bảo vệ ngoài, màn hình Liquid Retina còn tốt",
            ["Ép kính cảm ứng máy tính bảng", "Thay pin máy tính bảng"],
            5,
            "Ép kính iPad Pro 11 M2 xong dùng Apple Pencil vẽ hoàn toàn bình thường, tiết kiệm hơn rất nhiều so với thay nguyên cụm.",
            4),
        new(
            "RT-SEED-0007",
            "Hoàng Thanh Trúc",
            "0901234507",
            "thanhtruc.seed@fixora.vn",
            "Quận Tân Bình, TP. Hồ Chí Minh",
            DeviceType.Smartwatch,
            "Apple",
            "Apple Watch Series 9",
            "FX-SEED-AWS9-07",
            "Pin chỉ dùng được nửa ngày là sập nguồn",
            "Dung lượng pin suy giảm dưới 75%, cần thay pin và dán lại keo kháng nước",
            ["Thay pin đồng hồ thông minh"],
            4,
            "Thay pin Apple Watch Series 9 gọn gàng, đồng hồ dùng lại được gần 2 ngày, nhân viên lễ tân tư vấn nhiệt tình.",
            3),
        new(
            "RT-SEED-0008",
            "Bùi Đức Duy",
            "0901234508",
            "ducduy.seed@fixora.vn",
            "Quận 10, TP. Hồ Chí Minh",
            DeviceType.Electronics,
            "Sony",
            "PlayStation 5",
            "FX-SEED-PS5-08",
            "Tay cầm DualSense bị trôi cần analog trái và loa Marshall pin yếu",
            "Mòn chiết áp analog tay cầm PS5",
            ["Sửa tay cầm chơi game PS5 / Switch", "Kiểm tra và vệ sinh mainboard"],
            5,
            "Thay cần Hall Effect cho tay cầm PS5 chơi mượt không còn bị drift chút nào. Rất hài lòng với chất lượng của Fixora!",
            2)
    ];
}
