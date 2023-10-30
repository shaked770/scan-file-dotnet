using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDirectoryBrowser();
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseDefaultFiles();

app.UseStaticFiles();

app.UseHttpsRedirection();

app.MapPost("/", ([FromForm] IFormFile file) =>
{
    using (var ms = file.OpenReadStream())
    {
        long searchResult = Seek(ms, "virus");
        if (searchResult != -1)
        {
            return Results.BadRequest();
        }
        using (var fileStream = File.Create("/home/shakedrosenblat/projects/dotnet/tryapi/savedFiles/" + file.FileName))
        {
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(fileStream);
        }
    }
    return Results.Ok();
})
.WithName("upload")
.WithOpenApi();

app.Run();

static long Seek(Stream stream, string search)
{
    byte[] searchBytes = Encoding.Default.GetBytes(search);
    int bufferSize = 1024;
    if (bufferSize < searchBytes.Length * 2) bufferSize = searchBytes.Length * 2;

    var buffer = new byte[bufferSize];
    var size = bufferSize;
    var offset = 0;
    var position = stream.Position;

    while (true)
    {
        var r = stream.Read(buffer, offset, size);
        if (r <= 0) return -1;
        ReadOnlySpan<byte> ro = buffer;
        if (r < size)
        {
            ro = ro.Slice(0, offset + size);
        }
        var i = ro.IndexOf(searchBytes);
        if (i > -1) return position + i;
        if (r < size) return -1;
        offset = searchBytes.Length;
        size = bufferSize - offset;
        Array.Copy(buffer, buffer.Length - offset, buffer, 0, offset);
        position += bufferSize - offset;
    }
}