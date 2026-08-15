using FluentValidation;
using MediatR;

namespace RepairShop.Application.Common.Behaviors;

/// <summary>
/// Chạy TRƯỚC mọi Command/Query Handler. Tự động tìm tất cả IValidator&lt;TRequest&gt;
/// đã đăng ký cho request này, nếu có lỗi thì ném ValidationException, KHÔNG cho Handler chạy tiếp.
/// Controller và Handler hoàn toàn không cần biết validate diễn ra ở đây.
/// </summary>
public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Kiểm tra request có Validator
        if (!_validators.Any())
            return await next();

        // Đóng gói Request thành một Context chuẩn Fluent Validation
        var context = new ValidationContext<TRequest>(request);

        // Cho tất cả kiểm tra cùng lúc
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Gom lỗi lại thành một danh sách duy nhất
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count != 0)
            throw new RepairShop.Application.Common.Exceptions.ValidationException(failures);

        return await next();
    }
}