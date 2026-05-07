using Addresses;
using Addresses.Census;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CensusGeocoderOptions>(
    builder.Configuration.GetSection(CensusGeocoderOptions.SectionName));

builder.Services.AddHttpClient<ICensusGeocoderClient, CensusGeocoderClient>((sp, client) =>
{
    var o = sp.GetRequiredService<IOptions<CensusGeocoderOptions>>().Value;
    var baseUrl = o.BaseUrl.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, o.TimeoutSeconds));
});

builder.Services.AddScoped<IAddressNormalizationService, AddressNormalizationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Address API (Census Geocoder facade)",
        Version = "v1",
        Description =
            "Validates, normalizes, deduplicates, and forward-geocodes addresses for the United States, " +
            "Puerto Rico, and U.S. Island Areas using the public U.S. Census Bureau Geocoder " +
            "(https://geocoding.geo.census.gov/). This is not worldwide coverage and not street autocomplete."
    });
});

builder.Services.AddCarter();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Address API v1");
});

app.MapCarter();

app.Run();
