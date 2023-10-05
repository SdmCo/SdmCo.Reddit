using Microsoft.OpenApi.Models;
using SdmCo.Reddit.Api.Extensions;
using SdmCo.Reddit.Api.Middleware;
using SdmCo.Reddit.Common.Persistence;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilog();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SdmCo Reddit Stats API", Version = "v1" });
});

var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString")!;

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddScoped<IRedditRepository, RedisRepository>();

var app = builder.Build();

app.Logger.LogInformation("{ApplicationName} created.", builder.Environment.ApplicationName);

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Logger.LogInformation("Starting application.");

app.Run();