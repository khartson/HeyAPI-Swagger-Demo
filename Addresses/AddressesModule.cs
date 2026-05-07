using Addresses.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Addresses;

public class AddressesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/addresses")
            .WithTags("Addresses");

        g.MapPost("/validate", ValidateAsync)
            .WithName("ValidateAddress")
            .Produces<StandardizedAddress>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        g.MapPost("/normalize", NormalizeAsync)
            .WithName("NormalizeAddress")
            .Produces<StandardizedAddress>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        g.MapPost("/deduplicate", DeduplicateAsync)
            .WithName("DeduplicateAddresses")
            .Produces<DeduplicateAddressesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        g.MapGet("/search", SearchAsync)
            .WithName("SearchAddress")
            .Produces<StandardizedAddress>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<Results<Ok<StandardizedAddress>, BadRequest<ProblemDetails>>> ValidateAsync(
        AddressInput body,
        IAddressNormalizationService svc,
        CancellationToken cancellationToken)
    {
        var err = svc.ValidateInput(body, out _);
        if (err is not null)
            return TypedResults.BadRequest(InvalidInput(err));

        var result = await svc.GeocodeAsync(body, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<StandardizedAddress>, BadRequest<ProblemDetails>>> NormalizeAsync(
        AddressInput body,
        IAddressNormalizationService svc,
        CancellationToken cancellationToken)
    {
        var err = svc.ValidateInput(body, out _);
        if (err is not null)
            return TypedResults.BadRequest(InvalidInput(err));

        var result = await svc.GeocodeAsync(body, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<DeduplicateAddressesResponse>, BadRequest<ProblemDetails>>> DeduplicateAsync(
        DeduplicateAddressesRequest body,
        IAddressNormalizationService svc,
        CancellationToken cancellationToken)
    {
        if (body.Addresses is null || body.Addresses.Count == 0)
        {
            return TypedResults.BadRequest(InvalidInput("Provide at least one address in addresses."));
        }

        for (var i = 0; i < body.Addresses.Count; i++)
        {
            var err = svc.ValidateInput(body.Addresses[i], out _);
            if (err is not null)
                return TypedResults.BadRequest(InvalidInput($"Index {i}: {err}"));
        }

        var result = await svc.DeduplicateAsync(body, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<StandardizedAddress>, BadRequest<ProblemDetails>>> SearchAsync(
        string? q,
        string? street,
        string? city,
        string? state,
        string? zip,
        IAddressNormalizationService svc,
        CancellationToken cancellationToken)
    {
        var hasQ = !string.IsNullOrWhiteSpace(q);
        var hasStructured = !string.IsNullOrWhiteSpace(street)
            || !string.IsNullOrWhiteSpace(city)
            || !string.IsNullOrWhiteSpace(state)
            || !string.IsNullOrWhiteSpace(zip);

        if (hasQ == hasStructured)
        {
            return TypedResults.BadRequest(InvalidInput(
                "Provide either query parameter q (one-line address) or structured street, city, state, and/or zip — not both, and not neither."));
        }

        var input = hasQ
            ? new AddressInput { OneLine = q }
            : new AddressInput { Street = street, City = city, State = state, Zip = zip };

        var err = svc.ValidateInput(input, out _);
        if (err is not null)
            return TypedResults.BadRequest(InvalidInput(err));

        var result = await svc.GeocodeAsync(input, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static ProblemDetails InvalidInput(string detail) =>
        new()
        {
            Title = "Invalid address input",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        };
}
