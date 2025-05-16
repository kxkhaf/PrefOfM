namespace AuthService.API.Middlewares.Configurations;

public static class SwaggerMiddleware
{
    public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app)
    {
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => 
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service v1");
                c.RoutePrefix = "swagger";
            });
        }
        
        return app;
    }
}