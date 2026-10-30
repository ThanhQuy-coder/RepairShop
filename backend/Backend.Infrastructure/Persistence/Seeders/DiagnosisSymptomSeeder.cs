using Microsoft.EntityFrameworkCore;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Diagnostics;

namespace RepairShop.Infrastructure.Persistence.Seeders;

public class DiagnosisSymptomSeeder : IDataSeeder
{
    private readonly AppDbContext _context;
    public DiagnosisSymptomSeeder(AppDbContext context) => _context = context;
    public int Order => 25;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _context.DiagnosisSymptoms
            .Select(s => new { s.Name, s.DeviceType })
            .ToListAsync(cancellationToken);
        var existingKeys = existing.Select(s => $"{s.DeviceType}:{s.Name}").ToHashSet();
        var symptoms = SeedData()
            .Where(s => existingKeys.Add($"{s.DeviceType}:{s.Name}"))
            .ToList();
        if (symptoms.Count == 0) return;
        await _context.DiagnosisSymptoms.AddRangeAsync(symptoms, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<DiagnosisSymptom> SeedData()
    {
        var data = new Dictionary<DeviceType, string[]>
        {
            [DeviceType.Phone] =
            [
                "Không lên nguồn", "Pin nhanh hết", "Không sạc được", "Sạc chập chờn",
                "Màn hình bị vỡ/nứt", "Màn hình không hiển thị", "Cảm ứng không hoạt động",
                "Camera lỗi", "Loa/micro không hoạt động", "Không nhận SIM/mất sóng",
                "Wi-Fi/Bluetooth lỗi", "Máy quá nóng", "Nút bấm không hoạt động",
                "Lỗi phần mềm/treo máy", "Vào nước"
            ],
            [DeviceType.Laptop] =
            [
                "Không lên nguồn", "Pin chai/không sạc được", "Máy tự tắt", "Máy quá nóng",
                "Quạt tản nhiệt kêu lớn", "Màn hình không hiển thị", "Màn hình bị sọc/nhấp nháy",
                "Bàn phím lỗi", "Touchpad không hoạt động", "Wi-Fi không kết nối",
                "Loa/micro lỗi", "Máy chạy chậm", "Không nhận ổ cứng/SSD", "Không nhận RAM",
                "Lỗi hệ điều hành", "Hư hỏng do chất lỏng"
            ],
            [DeviceType.Electronics] =
            [
                "Không lên nguồn", "Nguồn điện không ổn định", "Thiết bị hoạt động chập chờn",
                "Không nhận tín hiệu", "Màn hình không hiển thị", "Nút điều khiển không hoạt động",
                "Loa không phát âm thanh", "Cổng kết nối bị lỗi", "Quá nhiệt",
                "Pin không hoạt động", "Lỗi bo mạch", "Hư hỏng do chất lỏng", "Lỗi phần mềm/firmware"
            ],
            [DeviceType.Tablet] =
            [
                "Không lên nguồn", "Pin nhanh hết", "Không sạc được", "Màn hình vỡ/nứt",
                "Màn hình không hiển thị", "Cảm ứng lỗi", "Loa/micro lỗi", "Camera lỗi",
                "Wi-Fi/Bluetooth lỗi", "Không nhận SIM/mất sóng", "Máy quá nóng",
                "Lỗi phần mềm/treo máy", "Cổng sạc lỗi", "Hư hỏng do chất lỏng"
            ],
            [DeviceType.Smartwatch] =
            [
                "Không lên nguồn", "Pin nhanh hết", "Không sạc được", "Màn hình vỡ/nứt",
                "Màn hình không hiển thị", "Cảm ứng lỗi", "Không kết nối Bluetooth",
                "Không đồng bộ dữ liệu", "GPS không hoạt động", "Cảm biến đo không chính xác",
                "Nút bấm/núm xoay lỗi", "Rung/thông báo không hoạt động",
                "Không nhận SIM/eSIM (nếu hỗ trợ)", "Hư hỏng do chất lỏng"
            ]
        };

        return data.SelectMany(pair => pair.Value.Select((name, index) =>
            new DiagnosisSymptom(name, pair.Key, index + 1)));
    }
}
