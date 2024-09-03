using GenFarm;
using GenFarm.Common;
using GenFarm.Services;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>();

// Add services to the container
builder.Services.AddControllersWithViews();

// Register the IHttpClientFactory
builder.Services.AddHttpClient();

// Register the OpenAIClient service with the necessary configuration
builder.Services.AddSingleton<GenFarm.Common.OpenAIClient>(provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
    var openAIApiKey = apiSettings.ApiKey;
    var assistantId = apiSettings.AssistantId;
    return new OpenAIClient(openAIApiKey, assistantId);
});

// Register the OpenAIClient service
builder.Services.AddSingleton<GenFarm.Common.OpenAIClient>(provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
    var openAIApiKey = apiSettings.ApiKey;
    var assistantId = apiSettings.AssistantId;
    return new OpenAIClient(openAIApiKey, assistantId);
});

builder.Services.AddSingleton<BackgroundTaskQueue>(_ => new BackgroundTaskQueue(100));
builder.Services.AddHostedService<QueuedHostedService>();


// Register BlogBuilderService only once
builder.Services.AddSingleton<GenFarm.Services.BlogBuilderService>(provider =>
{
    var jwt = apiSettings.Jwt;
    return new BlogBuilderService(
        provider.GetRequiredService<OpenAIClient>(),
        provider.GetRequiredService<HttpClient>(),
        jwt,
        provider.GetRequiredService<BackgroundTaskQueue>()
    );
});





var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // Ensure controllers are mapped
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

app.Run();
