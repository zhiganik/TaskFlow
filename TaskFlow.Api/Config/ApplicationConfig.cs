using Serilog;
using TaskFlow.Api.Middleware;

namespace TaskFlow.Api.Config;

public static class ApplicationConfig
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseSerilogRequestLogging();

        app.UseSwagger();
        app.UseSwaggerUI(opts =>
        {
            opts.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
            opts.DisplayRequestDuration();
        });

        app.UseHttpsRedirection();
        app.UseCors("AllowFrontend");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");

        return app;
    }
}
