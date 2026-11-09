using RepairShop.Application.Modules.AIAdvisory.Commands.GetConsultation;
using RepairShop.Application.Modules.AIAdvisory.Validators;

namespace RepairShop.UnitTests.Application.Validators;

public class GetConsultationCommandValidatorTests
{
    private readonly GetConsultationCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var command = new GetConsultationCommand(
            "Phone", "iPhone", "13", "Pin tụt nhanh, máy nóng",
            new DateOnly(2026, 11, 10), true, true, "Nguyễn Văn A", "0901234567");

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("Tablet", "iPhone", "13", "Pin tụt nhanh", "DeviceType")]
    [InlineData("Phone", "", "13", "Pin tụt nhanh", "Brand")]
    [InlineData("Phone", "iPhone", "", "Pin tụt nhanh", "Model")]
    [InlineData("Phone", "iPhone", "13", "", "IssueDescription")]
    public void InvalidRequiredField_Fails(
        string deviceType, string brand, string model, string issue, string _)
    {
        var command = new GetConsultationCommand(deviceType, brand, model, issue);

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void TooLongIssueDescription_Fails()
    {
        var command = new GetConsultationCommand(
            "Phone", "iPhone", "13", new string('a', 501));

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abc-def-ghi")]
    public void InvalidPhone_Fails(string phone)
    {
        var command = new GetConsultationCommand(
            "Phone", "iPhone", "13", "Pin tụt nhanh", null, true, true, null, phone);

        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void EmptyPhone_IsAllowed()
    {
        var command = new GetConsultationCommand(
            "Phone", "iPhone", "13", "Pin tụt nhanh");

        Assert.True(_validator.Validate(command).IsValid);
    }
}
