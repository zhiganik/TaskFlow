using Serilog;

namespace TaskFlow.Api.Config;

public static class ApplicationConfig
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(opts =>
            {
                opts.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
                opts.DisplayRequestDuration();
            });
        }

        app.UseHttpsRedirection();
        app.MapControllers();
        return app;
    }
}
