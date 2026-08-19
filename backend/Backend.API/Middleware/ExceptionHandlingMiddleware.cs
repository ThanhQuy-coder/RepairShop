using System.Net;
using System.Text.Json;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Domain.Common.Exceptions;
using RepairShop.Shared.Models;

namespace RepairShop.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Cho phép HTTP Request đi tiếp vào API Controller, MediatR Pipeline, Handler
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Thực hiện xử lý lỗi được ném ra và gửi về controller
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = MapException(exception);

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            // Chỉ log đầy đủ stack trace cho lỗi 500 thật sự — lỗi nghiệp vụ 400/401/404/409 là luồng bình thường
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            // Lỗi nghiệp vụ (validate sai, email trùng, sai mật khẩu...) — không phải bug, chỉ cảnh báo
            _logger.LogWarning("Business exception: {ExceptionType} - {Message}",
                exception.GetType().Name, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Sử dụng cú pháp Pattern Matching (switch) của C# để ánh xạ từng loại Exception ra HTTP Status Code và Thông báo lỗi (JSON) thích hợp
    /// </summary>
    private static (HttpStatusCode StatusCode, ApiErrorResponse Response) MapException(Exception exception) =>
        exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ApiErrorResponse
                {
                    Message = "Dữ liệu gửi lên không hợp lệ.",
                    Errors = validationEx.Errors.SelectMany(e => e.Value).ToList()
                }),

            EmailAlreadyExistsException emailEx => (
                HttpStatusCode.Conflict,
                new ApiErrorResponse { Message = emailEx.Message }),

            InvalidCredentialsException credEx => (
                HttpStatusCode.Unauthorized,
                new ApiErrorResponse { Message = credEx.Message }),

            InvalidTicketTransitionException transitionEx => (
                HttpStatusCode.Conflict,
                new ApiErrorResponse { Message = transitionEx.Message }),

            InvalidTicketOperationException operationEx => (
                HttpStatusCode.Conflict,
                new ApiErrorResponse { Message = operationEx.Message }),

            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                new ApiErrorResponse { Message = domainEx.Message }),

            KeyNotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ApiErrorResponse { Message = notFoundEx.Message }),

            DeviceOwnershipMismatchException ownershipEx => (
                HttpStatusCode.Conflict, // 409 — đúng tinh thần lỗi "vi phạm ràng buộc quan hệ dữ liệu", không phải 400 (sai format) hay 403 (sai quyền)
                new ApiErrorResponse { Message = ownershipEx.Message }),

            _ => (
                HttpStatusCode.InternalServerError,
                new ApiErrorResponse { Message = "Đã xảy ra lỗi hệ thống, vui lòng thử lại sau." })
        };
}