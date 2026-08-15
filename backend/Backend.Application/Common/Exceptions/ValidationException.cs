using FluentValidation.Results;

namespace RepairShop.Application.Common.Exceptions;

/// <summary>
/// Bọc lại lỗi FluentValidation thành dạng Dictionary <field, messages[]>
/// để Exception Middleware dễ format ra "errors: []" chuẩn hóa.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("Đã xảy ra một hoặc nhiều lỗi khi validate dữ liệu.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}