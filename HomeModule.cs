public class HomeModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Text(
                "Address API. Open /swagger for OpenAPI and interactive documentation.",
                "text/plain"))
            .WithTags("Health");

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
            .WithTags("Health");
    }
}
