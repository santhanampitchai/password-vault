using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PasswordVault.Application.DTOs;

namespace PasswordVault.API.Middleware;

/// <summary>
/// Converts unhandled exceptions to structured ProblemDetails / ApiError responses.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorAsync(ctx, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext ctx, Exception ex)
    {
        var (status, code, message) = ex switch
        {
            KeyNotFoundException     => (HttpStatusCode.NotFound,       "NOT_FOUND",    ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", ex.Message),
            InvalidOperationException   => (HttpStatusCode.BadRequest,   "BAD_REQUEST",  ex.Message),
            ArgumentException           => (HttpStatusCode.BadRequest,   "BAD_REQUEST",  ex.Message),
            _                           => (HttpStatusCode.InternalServerError, "SERVER_ERROR", "An unexpected error occurred.")
        };

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode  = (int)status;

        var body = JsonSerializer.Serialize(
            new ApiError(code, message),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return ctx.Response.WriteAsync(body);
    }
}
