using SdmCo.Reddit.Api.Settings;
using Serilog;
using Serilog.Exceptions;

namespace SdmCo.Reddit.Api.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
    {
        var serilogSettings = new SerilogSettings();
        builder.Configuration.GetSection(SerilogSettings.SectionName).Bind(serilogSettings);

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration.ReadFrom.Configuration(context.Configuration);

            loggerConfiguration
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithExceptionDetails();

            if (serilogSettings.UseConsole)
                loggerConfiguration.WriteTo.Async(writeTo =>
                    writeTo.Console(outputTemplate: serilogSettings.LogTemplate));

        });

        return builder;
    }
}