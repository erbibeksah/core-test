using eRaptors.Data;
using eRaptors.IServices;
using eRaptors.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "Raptors", Version = "Raptors 1.0" }); });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

// BS:04012025
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IGeoLocationService, IpApiGeoLocationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
// Add HTTP client with secure SSL
builder.Services.AddHttpClient("SecureClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
    });

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("https://localhost:7026", "http://localhost:5108")  // Allow this origin
              .AllowAnyHeader()  // Allow any headers
              .AllowAnyMethod(); // Allow any HTTP method
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage(); 
    app.UseSwagger(); 
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"); });
}
else
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"); });
}   

app.UseHttpsRedirection();
// Use CORS middleware
app.UseCors("AllowLocalhost");
app.UseRouting();
//app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Swagger UI"));
app.UseAuthorization();

app.MapControllers();

app.Run();
