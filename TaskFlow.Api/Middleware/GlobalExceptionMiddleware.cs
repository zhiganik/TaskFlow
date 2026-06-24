using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Exceptions;

namespace TaskFlow.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, ILogger<GlobalExceptionMiddleware> logger)
    {
        try { await next(ctx); }
        catch (Exception ex) { await HandleAsync(ctx, ex, logger); }
    }

    private static async Task HandleAsync(HttpContext ctx, Exception ex, ILogger logger)
    {
        if (ex is ValidationException validationEx)
        {
            await WriteValidationProblemAsync(ctx, validationEx);
            return;
        }

        var (status, title) = ex switch
        {
            NotFoundException     e => (404, e.Message),
            ConflictException     e => (409, e.Message),
            ForbiddenException    e => (403, e.Message),
            UnauthorizedException e => (401, e.Message),
            _                       => (500, "An unexpected error occurred.")
        };

        if (status == 500)
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title, Instance = ctx.Request.Path
        });
    }

    private static Task WriteValidationProblemAsync(HttpContext ctx, ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        ctx.Response.ContentType = "application/problem+json";
        return ctx.Response.WriteAsJsonAsync(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Instance = ctx.Request.Path
        });
    }
}
