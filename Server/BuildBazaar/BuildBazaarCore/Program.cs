using Amazon.S3;
using BuildBazaarCore.Controllers;
using BuildBazaarCore.Services;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// Configure AWS Services
// ==============================
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();

// ==============================
// Register Application Services
// ==============================
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IConfigService, ConfigService>();
builder.Services.AddScoped<IAwsService, AwsService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBuildService, BuildService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IBuildUrlService, BuildUrlService>();
builder.Services.AddScoped<IReferenceImageService, ReferenceImageService>();
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();

// ==============================
// Configure Middleware Services
// ==============================
builder.Services.AddSystemWebAdapters();
builder.Services.AddHttpForwarder();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ==============================
// Configure Middleware
// ==============================

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=604800"); // Cache for 7 days
    }
});

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy", "frame-ancestors 'none';");
    await next();
});

app.UseRouting();
app.UseAuthorization();
app.UseSystemWebAdapters();
app.UseSession();

// ==============================
// Define Routes
// ==============================

app.MapControllerRoute("Builds", "Builds", new { controller = "Home", action = "Builds" });
app.MapControllerRoute("Public", "Public", new { controller = "Home", action = "Public" });
app.MapDefaultControllerRoute();

// ==============================
// Initialize Config & Run App
// ==============================

var BuildBazaarConfigService = app.Services.GetRequiredService<IConfigService>();
await BuildBazaarConfigService.InitializeAsync();

app.Run();
