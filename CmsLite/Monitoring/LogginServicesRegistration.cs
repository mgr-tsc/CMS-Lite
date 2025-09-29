using System;

namespace CmsLite.Monitoring;

public static class LogginServicesRegistration
{

    /// <summary>
    /// Registers logging services
    /// </summary>
    /// <param name="builder">
    /// The WebApplicationBuilder to add logging services to
    /// </param>
    public static void AddLoggingServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddLogging();
    }
}
