var builder = WebApplication.CreateBuilder(args);

builder.AddConfigurations();

builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddBackendServices();

builder.Logging.AddAzureWebAppDiagnostics();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "Lumenic Backend");
});

app.UseCors(option =>
{
    option.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
});
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();