

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<PersonClient>();
builder.Services.AddTransient<PersonRepository>();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/john", 
    async ctx => {
        var repo = ctx.RequestServices.GetRequiredService<PersonRepository>();

        var john = await repo.GetPerson("John");

        ctx.Response.StatusCode = 200;
        await ctx.Response.WriteAsJsonAsync(john);
    });

app.Run();

// NOTE: add this to be able to test with WebApplicationFactory
public partial class Program { }
