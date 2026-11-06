using System.Text;
using Moq;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Content.Commands;
using RepairShop.Application.Modules.Content.Queries;
using RepairShop.Application.Modules.Content.Validators;
using RepairShop.Application.Modules.Devices;
using RepairShop.Application.Modules.Reviews.Queries;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Domain.Modules.Content;
using RepairShop.Domain.Modules.Devices;

namespace Backend.UnitTests.Application;

public class CatalogAndReviewTests
{
    // =========================================================================
    // 1. BRAND & DEVICE MODEL TESTS (Task 1 & Section 9.1)
    // =========================================================================

    [Fact]
    public async Task CreateBrand_WithValidData_CreatesAndReturnsBrand()
    {
        var brandRepoMock = new Mock<IBrandRepository>();
        brandRepoMock
            .Setup(r => r.ExistsByCodeAsync("APPLE", null))
            .ReturnsAsync(false);
        brandRepoMock
            .Setup(r => r.ExistsByNameAsync("Apple", null))
            .ReturnsAsync(false);

        var handler = new BrandHandlers(brandRepoMock.Object);
        var command = new CreateBrandCommand(
            "Apple",
            "apple",
            "/images/brands/apple.svg",
            "Apple Inc. — iPhone, iPad, MacBook, Apple Watch",
            true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Apple", result.Name);
        Assert.Equal("APPLE", result.Code);
        Assert.Equal("/images/brands/apple.svg", result.LogoUrl);
        Assert.True(result.IsActive);
        brandRepoMock.Verify(r => r.AddAsync(It.IsAny<Brand>()), Times.Once);
        brandRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateBrand_WhenCodeAlreadyExists_ThrowsDomainException()
    {
        var brandRepoMock = new Mock<IBrandRepository>();
        brandRepoMock
            .Setup(r => r.ExistsByCodeAsync("SAMSUNG", null))
            .ReturnsAsync(true);

        var handler = new BrandHandlers(brandRepoMock.Object);
        var command = new CreateBrandCommand("Samsung Electronics", "SAMSUNG");

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task GetBrands_FilteredByDeviceType_ReturnsMatchingBrandsWithDeviceTypes()
    {
        var apple = new Brand("Apple", "APPLE", "/images/brands/apple.svg", "Apple", true);
        var dell = new Brand("Dell", "DELL", "/images/brands/dell.svg", "Dell", true);

        var brandRepoMock = new Mock<IBrandRepository>();
        brandRepoMock
            .Setup(r => r.SearchAsync(DeviceType.Laptop, true, null, 1, 50))
            .ReturnsAsync((
                new List<Brand> { apple, dell },
                2,
                new Dictionary<Guid, List<string>>
                {
                    [apple.Id] = ["Phone", "Laptop", "Tablet", "Smartwatch"],
                    [dell.Id] = ["Laptop"],
                },
                new Dictionary<Guid, int>
                {
                    [apple.Id] = 15,
                    [dell.Id] = 4,
                }));

        var handler = new BrandHandlers(brandRepoMock.Object);
        var result = await handler.Handle(
            new GetBrandsQuery(DeviceType: "Laptop", IsActive: true, Page: 1, PageSize: 50),
            CancellationToken.None);

        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains("Laptop", result.Items[0].DeviceTypes);
        Assert.Equal(15, result.Items[0].ModelCount);
    }

    [Fact]
    public async Task CreateDeviceModel_WithValidBrandAndNullableFields_Succeeds()
    {
        var brand = new Brand("Garmin", "GARMIN", "/images/brands/garmin.svg", null, true);
        var brandRepoMock = new Mock<IBrandRepository>();
        var modelRepoMock = new Mock<IDeviceModelRepository>();

        brandRepoMock.Setup(r => r.GetByIdAsync(brand.Id)).ReturnsAsync(brand);
        modelRepoMock
            .Setup(r => r.ExistsAsync(brand.Id, DeviceType.Smartwatch, "Forerunner 965", null))
            .ReturnsAsync(false);

        var handler = new DeviceModelHandlers(modelRepoMock.Object, brandRepoMock.Object);
        var command = new CreateDeviceModelCommand(
            brand.Id,
            "Smartwatch",
            "Forerunner 965",
            ModelNumber: null,
            ReleaseYear: null,
            IsActive: true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(brand.Id, result.BrandId);
        Assert.Equal("Garmin", result.BrandName);
        Assert.Equal("Smartwatch", result.DeviceType);
        Assert.Equal("Forerunner 965", result.ModelName);
        Assert.Null(result.ModelNumber);
        Assert.Null(result.ReleaseYear);
        modelRepoMock.Verify(r => r.AddAsync(It.IsAny<DeviceModel>()), Times.Once);
        modelRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateDeviceModel_WhenBrandIsInactive_ThrowsDomainException()
    {
        var inactiveBrand = new Brand("LegacyBrand", "LEGACY", null, null, isActive: false);
        var brandRepoMock = new Mock<IBrandRepository>();
        var modelRepoMock = new Mock<IDeviceModelRepository>();

        brandRepoMock.Setup(r => r.GetByIdAsync(inactiveBrand.Id)).ReturnsAsync(inactiveBrand);

        var handler = new DeviceModelHandlers(modelRepoMock.Object, brandRepoMock.Object);
        var command = new CreateDeviceModelCommand(
            inactiveBrand.Id,
            "Phone",
            "Legacy Phone X",
            "LP-01",
            2020);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateDeviceModel_WhenBrandDoesNotExist_ThrowsNotFoundException()
    {
        var brandRepoMock = new Mock<IBrandRepository>();
        var modelRepoMock = new Mock<IDeviceModelRepository>();
        var missingBrandId = Guid.NewGuid();

        brandRepoMock.Setup(r => r.GetByIdAsync(missingBrandId)).ReturnsAsync((Brand?)null);

        var handler = new DeviceModelHandlers(modelRepoMock.Object, brandRepoMock.Object);
        var command = new CreateDeviceModelCommand(missingBrandId, "Tablet", "Tab Ultra");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task GetDeviceModels_FilterByBrandAndDeviceType_ReturnsMatchingModels()
    {
        var apple = new Brand("Apple", "APPLE");
        var ipadPro = new DeviceModel(apple.Id, DeviceType.Tablet, "iPad Pro 11 M2", "A2759", 2022);
        typeof(DeviceModel).GetProperty(nameof(DeviceModel.Brand))!.SetValue(ipadPro, apple);

        var brandRepoMock = new Mock<IBrandRepository>();
        var modelRepoMock = new Mock<IDeviceModelRepository>();

        modelRepoMock
            .Setup(r => r.SearchAsync(DeviceType.Tablet, apple.Id, true, null, 1, 100))
            .ReturnsAsync((new List<DeviceModel> { ipadPro }, 1));

        var handler = new DeviceModelHandlers(modelRepoMock.Object, brandRepoMock.Object);
        var result = await handler.Handle(
            new GetDeviceModelsQuery("Tablet", apple.Id, true, null, 1, 100),
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("iPad Pro 11 M2", result.Items[0].ModelName);
        Assert.Equal("Tablet", result.Items[0].DeviceType);
        Assert.Equal("Apple", result.Items[0].BrandName);
    }

    [Fact]
    public async Task DeleteBrand_WhenReferencedByDevicesOrModels_ThrowsDomainException()
    {
        var brand = new Brand("Sony", "SONY");
        var brandRepoMock = new Mock<IBrandRepository>();
        brandRepoMock.Setup(r => r.GetByIdAsync(brand.Id)).ReturnsAsync(brand);
        brandRepoMock.Setup(r => r.IsReferencedAsync(brand.Id)).ReturnsAsync(true);

        var handler = new BrandHandlers(brandRepoMock.Object);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DeleteBrandCommand(brand.Id), CancellationToken.None));
    }

    // =========================================================================
    // 2. SERVICE CATALOG & IMAGE UPLOAD TESTS (Task 2 & Section 9.2)
    // =========================================================================

    [Fact]
    public async Task CreateService_WithUploadedImage_StoresSafeImageUrl()
    {
        var serviceRepoMock = new Mock<IServiceRepository>();
        var storageMock = new Mock<IFileStorageService>();

        storageMock
            .Setup(s => s.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), "fixora/services"))
            .ReturnsAsync(new UploadedFileResult("/uploads/fixora/services/safe-guid.png", "safe-guid"));

        // Minimal valid PNG header bytes
        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
        using var stream = new MemoryStream(pngBytes);

        var handler = new CreateServiceCommandHandler(serviceRepoMock.Object, storageMock.Object);
        var command = new CreateServiceCommand(
            "Thay màn hình iPhone 15 Pro Max",
            "Màn hình OLED chính hãng",
            6500000m,
            "Phone",
            ImageUrl: null,
            ImageStream: stream,
            ImageFileName: "client-untrusted-name.png",
            ImageContentType: "image/png",
            ImageLength: pngBytes.Length);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Thay màn hình iPhone 15 Pro Max", result.Name);
        Assert.Equal("/uploads/fixora/services/safe-guid.png", result.ImageUrl);
        storageMock.Verify(
            s => s.UploadImageAsync(
                It.IsAny<Stream>(),
                It.Is<string>(fn => fn.EndsWith(".png") && !fn.Contains("client-untrusted-name")),
                "fixora/services"),
            Times.Once);
    }

    [Fact]
    public async Task CreateService_WithoutImage_AllowsNullImageUrl()
    {
        var serviceRepoMock = new Mock<IServiceRepository>();
        var handler = new CreateServiceCommandHandler(serviceRepoMock.Object);

        var command = new CreateServiceCommand(
            "Kiểm tra tổng quát thiết bị",
            "Chẩn đoán lỗi phần cứng miễn phí",
            0m,
            "Electronics",
            ImageUrl: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Kiểm tra tổng quát thiết bị", result.Name);
        Assert.Null(result.ImageUrl);
        serviceRepoMock.Verify(r => r.AddAsync(It.IsAny<Service>()), Times.Once);
    }

    [Fact]
    public async Task UpdateService_WithoutNewImage_KeepsExistingImageUrl()
    {
        var existingService = new Service(
            "Thay pin MacBook Pro",
            "Pin chuẩn dung lượng",
            2200000m,
            "Laptop",
            "/images/services/laptop-battery.svg");

        var serviceRepoMock = new Mock<IServiceRepository>();
        var storageMock = new Mock<IFileStorageService>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(existingService.Id)).ReturnsAsync(existingService);
        serviceRepoMock
            .Setup(r => r.GetServiceRatingStatsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, (double AverageRating, int ReviewCount)>());

        var handler = new UpdateServiceCommandHandler(serviceRepoMock.Object, storageMock.Object);
        var command = new UpdateServiceCommand(
            existingService.Id,
            "Thay pin MacBook Pro M1/M2",
            "Pin chuẩn dung lượng bảo hành 12 tháng",
            2350000m,
            "Laptop",
            ImageUrl: null,
            ImageStream: null,
            RemoveImage: false);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("/images/services/laptop-battery.svg", result.ImageUrl);
        Assert.Equal(2350000m, result.BasePrice);
        storageMock.Verify(s => s.DeleteImageAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateService_WithNewUploadedImage_ReplacesImageAndDeletesOldImage()
    {
        var existingService = new Service(
            "Thay kính Apple Watch",
            "Ép kính cảm ứng",
            850000m,
            "Smartwatch",
            "/uploads/fixora/services/old-image.webp");

        var serviceRepoMock = new Mock<IServiceRepository>();
        var storageMock = new Mock<IFileStorageService>();
        serviceRepoMock.Setup(r => r.GetByIdAsync(existingService.Id)).ReturnsAsync(existingService);
        serviceRepoMock
            .Setup(r => r.GetServiceRatingStatsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, (double AverageRating, int ReviewCount)>());

        storageMock
            .Setup(s => s.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), "fixora/services"))
            .ReturnsAsync(new UploadedFileResult("/uploads/fixora/services/new-image.jpg", "new-image"));

        byte[] jpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
        using var stream = new MemoryStream(jpegBytes);

        var handler = new UpdateServiceCommandHandler(serviceRepoMock.Object, storageMock.Object);
        var command = new UpdateServiceCommand(
            existingService.Id,
            "Thay kính cảm ứng Apple Watch",
            "Ép kính giữ chống nước",
            900000m,
            "Smartwatch",
            ImageStream: stream,
            ImageFileName: "watch.jpg",
            ImageContentType: "image/jpeg",
            ImageLength: jpegBytes.Length);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("/uploads/fixora/services/new-image.jpg", result.ImageUrl);
        storageMock.Verify(s => s.DeleteImageAsync("/uploads/fixora/services/old-image.webp"), Times.Once);
    }

    [Fact]
    public void ImageFileValidator_RejectsExecutableAndDoubleExtensionAndOversizedFiles()
    {
        // 1. Executable extension (.exe)
        using var exeStream = new MemoryStream([0x4D, 0x5A, 0x90, 0x00]);
        Assert.Throws<DomainException>(() =>
            ImageFileValidator.ValidateAndBuildSafeFileName(exeStream, "malware.exe", "application/octet-stream", 4));

        // 2. Spoofed .jpg extension with MZ (PE executable) magic bytes
        using var spoofedStream = new MemoryStream([0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00]);
        Assert.Throws<DomainException>(() =>
            ImageFileValidator.ValidateAndBuildSafeFileName(spoofedStream, "photo.jpg", "image/jpeg", 6));

        // 3. Double extension (.php.jpg)
        using var doubleExtStream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0]);
        Assert.Throws<DomainException>(() =>
            ImageFileValidator.ValidateAndBuildSafeFileName(doubleExtStream, "shell.php.jpg", "image/jpeg", 4));

        // 4. Oversized file (> 5MB)
        using var validHeaderStream = new MemoryStream([0xFF, 0xD8, 0xFF, 0xE0]);
        Assert.Throws<DomainException>(() =>
            ImageFileValidator.ValidateAndBuildSafeFileName(
                validHeaderStream,
                "huge.jpg",
                "image/jpeg",
                6 * 1024 * 1024));
    }

    // =========================================================================
    // 3. REVIEW LINKED TO SERVICE TESTS (Task 3 & Section 9.3)
    // =========================================================================

    [Fact]
    public async Task GetServiceReviews_ReturnsAccurateSummaryDistributionAndPaginatedItems()
    {
        var serviceId = Guid.NewGuid();
        var service = new Service(
            "Thay pin iPhone dung lượng chuẩn",
            "Pin chính hãng bảo hành 12 tháng",
            890000m,
            "Phone",
            "/images/services/phone-battery.svg");

        var expectedSummary = new ServiceReviewSummary(
            serviceId,
            AverageRating: 4.7,
            TotalReviews: 3,
            RatingDistribution: new Dictionary<int, int>
            {
                [5] = 2,
                [4] = 1,
                [3] = 0,
                [2] = 0,
                [1] = 0,
            });

        var expectedItems = new List<ServiceReviewItem>
        {
            new(Guid.NewGuid(), 5, "Thay pin rất nhanh, báo dung lượng 100%.", "Nguyễn Văn A", DateTime.UtcNow.AddDays(-1)),
            new(Guid.NewGuid(), 5, "Kỹ thuật viên ký tên linh kiện rõ ràng.", "Trần Thị B", DateTime.UtcNow.AddDays(-3)),
        };

        var reviewRepoMock = new Mock<IReviewRepository>();
        var serviceRepoMock = new Mock<IServiceRepository>();

        serviceRepoMock.Setup(r => r.GetByIdAsync(serviceId)).ReturnsAsync(service);
        reviewRepoMock
            .Setup(r => r.GetByServiceIdAsync(serviceId, 1, 2))
            .ReturnsAsync((expectedSummary, expectedItems, 3));

        var handler = new GetReviewsQueryHandler(reviewRepoMock.Object, serviceRepoMock.Object);
        var result = await handler.Handle(
            new GetServiceReviewsQuery(serviceId, Page: 1, PageSize: 2),
            CancellationToken.None);

        Assert.Equal(4.7, result.Summary.AverageRating);
        Assert.Equal(3, result.Summary.TotalReviews);
        Assert.Equal(2, result.Summary.RatingDistribution[5]);
        Assert.Equal(1, result.Summary.RatingDistribution[4]);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetServicesQuery_EnrichesServicesWithAverageRatingAndReviewCount_ForMultiServiceTickets()
    {
        var serviceA = new Service("Thay màn hình iPhone", "OLED", 3500000m, "Phone", "/images/services/phone-screen.svg");
        var serviceB = new Service("Thay pin iPhone", "Pin chuẩn", 890000m, "Phone", "/images/services/phone-battery.svg");

        var serviceRepoMock = new Mock<IServiceRepository>();
        serviceRepoMock
            .Setup(r => r.SearchAsync(true, 1, 20, "Phone", null))
            .ReturnsAsync((new List<Service> { serviceA, serviceB }, 2));

        // Ticket T1 used both ServiceA and ServiceB and left a 5-star review; ServiceA also has another 4-star review
        serviceRepoMock
            .Setup(r => r.GetServiceRatingStatsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, (double AverageRating, int ReviewCount)>
            {
                [serviceA.Id] = (4.5, 2),
                [serviceB.Id] = (5.0, 1),
            });

        var handler = new GetServicesQueryHandler(serviceRepoMock.Object);
        var result = await handler.Handle(
            new GetServicesQuery(IsActive: true, Page: 1, PageSize: 20, DeviceType: "Phone"),
            CancellationToken.None);

        Assert.Equal(2, result.Total);
        Assert.Equal(4.5, result.Items[0].AverageRating);
        Assert.Equal(2, result.Items[0].ReviewCount);
        Assert.Equal(5.0, result.Items[1].AverageRating);
        Assert.Equal(1, result.Items[1].ReviewCount);
    }
}
