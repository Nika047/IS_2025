using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Primitives;
using DevExpress.Xpo.Metadata;
using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using NLog.Web;
using DevExpress.AspNetCore;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using ui.Settings;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using ui.Helper;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.Configure<TouristsSettings>(
    builder.Configuration.GetSection("TouristsSettings"));

builder.Services.AddSingleton(builder.Configuration);
builder.Services.AddScoped<ExpertEngine>();

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
});


builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services.AddDevExpressControls();
builder.Services
    .AddMvc()
    .AddNewtonsoftJson();

builder.Services
    .AddXpoDefaultDataLayer(
        ServiceLifetime.Singleton,
        dl => dl
            .UseConnectionString(builder.Configuration["ConnectionStrings:Data"])
            .UseThreadSafeDataLayer(true)
            .UseCustomDictionaryFactory(() => {
                ReflectionDictionary dictionary = new();
                //dictionary.CollectClassInfos(typeof(DbPermissionAccount).Assembly);
                return dictionary;
            })
        );


builder.Services.AddXpoDefaultUnitOfWork();

builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    })
    .AddXpoModelJsonOptions();

var app = builder.Build();

app.UseDevExpressControls();

using (var scope = app.Services.CreateScope())
{
    var dataLayer = scope.ServiceProvider.GetRequiredService<IDataLayer>();
    XpoDefault.DataLayer = dataLayer;
    XpoDefault.Session = null; // —брос сессии по умолчанию
}

if (!app.Environment.IsDevelopment())
{
    app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

//app.UseAuthentication();
//app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
