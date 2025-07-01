using API_FORMAT.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
    options.MultipartHeadersLengthLimit = 1024 * 1024;
});


builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.WriteIndented = true;
    });


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});

var app = builder.Build();


var uploadsFolder = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
if (!Directory.Exists(uploadsFolder))
{
    Directory.CreateDirectory(uploadsFolder);
}


app.UseCors("AllowAll");
app.UseHttpsRedirection();


app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
    RequestPath = "",
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif"
        }
    },
    OnPrepareResponse = ctx =>
    {

        ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    }
});

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();