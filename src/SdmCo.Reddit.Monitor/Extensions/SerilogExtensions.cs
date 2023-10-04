using SdmCo.Reddit.Common.Settings;
using Serilog;
using Serilog.Exceptions;

namespace SdmCo.Reddit.Monitor.Extensions;

public static class SerilogExtensions
{
    public static IHostBuilder UseSerilog(this IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            var serilogSettings = new SerilogSettings();
            var section = config.Build().GetSection(SerilogSettings.SectionName);
            section.Bind(serilogSettings);

            builder.UseSerilog((context, services, loggerConfiguration) =>
            {
                loggerConfiguration.ReadFrom.Configuration(context.Configuration);

                loggerConfiguration
                    .Enrich.WithProperty("Application", hostingContext.HostingEnvironment.ApplicationName)
                    .Enrich.WithProperty("Environment", hostingContext.HostingEnvironment.EnvironmentName)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithExceptionDetails();

                if (serilogSettings.UseConsole)
                    loggerConfiguration.WriteTo.Async(writeTo =>
                        writeTo.Console(outputTemplate: serilogSettings.LogTemplate));

            });


        });

        return builder;
    }
}