using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KBS.API.Controllers;

public class StaffOperationFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var error = context.Exception;
        var code = error switch
        {
            KeyNotFoundException => 404,
            UnauthorizedAccessException => 403,
            ArgumentException => 400,
            InvalidOperationException => 409,
            PostgresException { SqlState: "40001" or "23505" } => 409,
            DbUpdateException { InnerException: PostgresException { SqlState: "40001" or "23505" or "23503" } } => 409,
            _ => 0
        };
        if (code == 0) return;
        var message = error is PostgresException or DbUpdateException
            ? "Dữ liệu vừa thay đổi. Vui lòng tải lại và thử lại."
            : error.Message;
        context.Result = new ObjectResult(new { message }) { StatusCode = code };
        context.ExceptionHandled = true;
    }
}
