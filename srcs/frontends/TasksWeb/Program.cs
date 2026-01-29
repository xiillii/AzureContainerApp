using TasksWeb.Components;
using TasksWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure HttpClient for API calls
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "http://tasks-api:8080";
builder.Services.AddHttpClient<TasksApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Register services
builder.Services.AddScoped<AuthService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri(apiBaseUrl);
    var logger = sp.GetRequiredService<ILogger<AuthService>>();
    return new AuthService(httpClient, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
