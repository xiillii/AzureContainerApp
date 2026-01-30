using TasksWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure HttpClient for API calls
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "http://tasks-api:8080";
builder.Services.AddHttpClient<TasksApiClient>((sp, client) =>
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
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    return new AuthService(httpClient, logger, httpContextAccessor);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
