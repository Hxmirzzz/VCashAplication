using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Data;
using System.IO;
using VCashApp.Data;
using VCashApp.Data.Seed;
using VCashApp.Extentions;
using VCashApp.Infrastructure.Branches;
using VCashApp.Infrastructure.Middleware;
using VCashApp.Models;
using VCashApp.Services;
using VCashApp.Services.Cef;
using VCashApp.Services.Employee.Application;
using VCashApp.Services.Employee.Infrastructure;
using VCashApp.Services.Employee.Domain;
using VCashApp.Services.CentroEfectivo.Collection.Application;
using VCashApp.Services.CentroEfectivo.Collection.Domain;
using VCashApp.Services.CentroEfectivo.Provision.Application;
using VCashApp.Services.CentroEfectivo.Provision.Domain;
using VCashApp.Services.CentroEfectivo.Shared.Domain;
using VCashApp.Services.CentroEfectivo.Shared.Infrastructure;
using VCashApp.Services.EmployeeLog.Application;
using VCashApp.Services.EmployeeLog.Integration;
using VCashApp.Services.EmployeeLog.Queries;
using VCashApp.Services.Range;
using VCashApp.Services.Service;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "VCashApp")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

    // Sublogger a SQL: sólo auditoría (IsAudit=true) o Warning+
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Level >= LogEventLevel.Warning ||
            (e.Properties.ContainsKey("IsAudit") && e.Properties["IsAudit"].ToString() == "True"))
        .WriteTo.MSSqlServer(
            connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
            sinkOptions: new MSSqlServerSinkOptions
            {
                TableName = "AppLogs",
                AutoCreateSqlTable = true
            },
            columnOptions: new ColumnOptions
            {
                TimeStamp = { ColumnName = "Timestamp", DataType = SqlDbType.DateTimeOffset, NonClusteredIndex = true },
                Level = { ColumnName = "Level", DataType = SqlDbType.NVarChar, DataLength = 12 },
                Message = { ColumnName = "Message", DataType = SqlDbType.NVarChar, DataLength = -1 },
                Exception = { ColumnName = "Exception", DataType = SqlDbType.NVarChar, DataLength = -1 },
                Properties = { ColumnName = "Properties", DataType = SqlDbType.NVarChar, DataLength = -1 },

                AdditionalColumns = new List<SqlColumn>
                {
                    new SqlColumn("SourceContext", SqlDbType.NVarChar) { DataLength = 255, AllowNull = true },

                    // Propiedades principales de auditoría:
                    new SqlColumn("IsAudit",     SqlDbType.Bit)        { AllowNull = true }, // del scope
                    new SqlColumn("Action",      SqlDbType.NVarChar)   { DataLength = 255, AllowNull = true },
                    new SqlColumn("Username",    SqlDbType.NVarChar)   { DataLength = 255, AllowNull = true },
                    new SqlColumn("IpAddress",   SqlDbType.NVarChar)   { DataLength = 45,  AllowNull = true },
                    new SqlColumn("UrlPath",     SqlDbType.NVarChar)   { DataLength = 512, AllowNull = true },
                    new SqlColumn("Result",      SqlDbType.NVarChar)   { DataLength = 255, AllowNull = true },
                    new SqlColumn("DetailMessage", SqlDbType.NVarChar) { DataLength = -1,  AllowNull = true },

                    // Contexto de dominio:
                    new SqlColumn("EntityType",  SqlDbType.NVarChar)   { DataLength = 50,  AllowNull = true },
                    new SqlColumn("EntityId",    SqlDbType.NVarChar)   { DataLength = 450, AllowNull = true },
                    new SqlColumn("UrlId",       SqlDbType.NVarChar)   { DataLength = 450, AllowNull = true },
                    new SqlColumn("ModelId",     SqlDbType.NVarChar)   { DataLength = 450, AllowNull = true }
                }
            },
            restrictedToMinimumLevel: LogEventLevel.Information
        )
    )

    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Request starting"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Request finished"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Route matched with"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executed DbCommand"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executing ViewResult"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executed ViewResult"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executed action"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executed endpoint"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Start processing HTTP request"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Sending HTTP request"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Received HTTP response"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("End processing HTTP request"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Accessing expired session"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executing RedirectResult"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("The query uses a row limiting operator"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("The file"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executing JsonResult"))
    .CreateLogger();

builder.Host.UseSerilog();

// --- 2. Configuración de Servicios ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.LogTo(Log.Information, LogLevel.Information);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataProtectionKeysStable")))
    .SetApplicationName("VCashApp");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = false;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddRoles<IdentityRole>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/plain", "application/javascript", "text/css", "application/x-font-woff", "application/font-sfnt", "application/font-ttf", "application/font-otf", "image/svg+xml" });
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IBranchContext, BranchContext>();
builder.Services.AddScoped<IBranchResolver, BranchResolver>();

builder.Services.AddScoped<IUserService, UserService>();

// Employee Application
builder.Services.Configure<VCashApp.Services.Employee.Application.Options.RepositoryOptions>(
    builder.Configuration.GetSection("Repositorio"));
builder.Services.AddScoped<IEmployeeRepository, EfEmployeeRepository>();
builder.Services.AddScoped<IEmployeeFileStorage, FileSystemEmployeeStorage>();
builder.Services.AddScoped<IEmployeeReadService, EmployeeReadService>();
builder.Services.AddScoped<IEmployeeWriteService, EmployeeWriteService>();

builder.Services.AddScoped<IRutaDiariaService, RutaDiariaService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ICefTransactionService, CefTransactionService>();
builder.Services.AddScoped<ICefContainerService, CefContainerService>();
builder.Services.AddScoped<ICefIncidentService, CefIncidentService>();
builder.Services.AddScoped<ICefServiceCreationService, CefServiceCreationService>(); // TEMPORAL
builder.Services.AddScoped<ICgsServiceService, CgsService>();
builder.Services.AddScoped<IRangeService, RangeService>();
builder.Services.AddScoped<VCashApp.Services.Logging.IAuditLogger, VCashApp.Services.Logging.AuditLogger>();
builder.Services.AddScoped<AuditActionFilter>();

// Shared
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<ICefTransactionRepository, CefTransactionRepository>(); // (tu repo existente)
builder.Services.AddScoped<ICefTransactionQueries, CefTransactionQueries>();
builder.Services.AddScoped<IAuditLogger, SerilogAuditLogger>();

builder.Services.AddScoped<ICefContainerRepository, CefContainerRepository>();
builder.Services.AddScoped<ICefIncidentRepository, CefIncidentRepository>();
builder.Services.AddScoped<ICefCatalogRepository, CefCatalogRepository>();

// Policies (stateless)
builder.Services.AddSingleton<IProvisionStateMachine, ProvisionStateMachine>();
builder.Services.AddSingleton<IAllowedValueTypesPolicy, ProvisionAllowedValueTypesPolicy>();
builder.Services.AddSingleton<IEnvelopePolicy>(_ => new ProvisionEnvelopePolicy { AllowEnvelopes = true });
builder.Services.AddSingleton<ITolerancePolicy>(_ => new ZeroTolerancePolicy(0m));
builder.Services.AddSingleton<ICollectionStateMachine, CollectionStateMachine>();
builder.Services.AddSingleton<ICountingPolicy, CountingPolicy>();

// Collection Application
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<ICollectionReadService, CollectionReadService>();

// Provision Application
builder.Services.AddScoped<IProvisionService, ProvisionService>();
builder.Services.AddScoped<IProvisionReadService, ProvisionReadService>();

builder.Services.AddScoped<ICefCatalogRepository, CefCatalogRepository>();
builder.Services.AddScoped<ICefIncidentService, CefIncidentService>();
builder.Services.AddScoped<ICefContainerRepository, CefContainerRepository>();
builder.Services.AddHttpClient();

builder.Services.AddScoped<IEmployeeLogService, EmployeeLogService>();
builder.Services.AddScoped<IDailyRouteUpdater, DailyRouteUpdater>();
builder.Services.AddScoped<IEmployeeLogLookupsService, EmployeeLogLookupsService>();

builder.Services.AddControllersWithViews(o =>
{
    o.Filters.AddService<AuditActionFilter>();
});
builder.Services.AddRazorPages();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Por favor ingrese 'Bearer ' + token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VCashApp API",
        Version = "v1",
        Description = "API de gestión de registros y rutas de empleados para VCashApp."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    else
    {
        Console.WriteLine($"WARNING: XML Documentation file not found at: {xmlPath}");
    }
    c.EnableAnnotations();
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return true;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VCashApp API V1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "VCashApp API Documentation";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCompression();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseMiddleware<BranchContextMiddleware>();
app.UseMiddleware<LogIpMiddleware>();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await IdentitySeedData.SeedRolesAsync(roleManager);
    await IdentitySeedData.SeedUsersAsync(userManager, roleManager, dbContext);
    await IdentitySeedData.SeedBranchPermissionsAsync(dbContext, userManager);
    await IdentitySeedData.SeedPermissionsAsync(dbContext, roleManager);
    await IdentitySeedData.SeedCefIncidentTypesAsync(dbContext);
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();