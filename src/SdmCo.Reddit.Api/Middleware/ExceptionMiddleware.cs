using System.Net;
using SdmCo.Reddit.Api.Entities;
using SdmCo.Reddit.Common.Exceptions;

namespace SdmCo.Reddit.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    public async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        _logger.LogError(exception, exception.Message);

        if (exception is SubredditNotFoundException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;

            await context.Response.WriteAsync(new ErrorDetails
            {
                StatusCode = context.Response.StatusCode,
                Message = $"Subreddit not found"

            }.ToString());
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            await context.Response.WriteAsync(new ErrorDetails
            {
                StatusCode = context.Response.StatusCode,
                Message = _env.IsProduction()
                    ? "An error has occured"
                    : exception.Message // Don't want to leak exception details to users in Production
            }.ToString());
        }
    }
}