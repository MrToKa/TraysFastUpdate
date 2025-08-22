using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using TraysFastUpdate.Components;
using TraysFastUpdate.Data;
using TraysFastUpdate.Data.Repositories;
using TraysFastUpdate.Services;
using TraysFastUpdate.Services.Calculations;
using TraysFastUpdate.Services.Contracts;
using TraysFastUpdate.Services.Drawing;
using TraysFastUpdate.Services.Export;

namespace TraysFastUpdate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add MudBlazor services
            builder.Services.AddMudServices();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Repository
            builder.Services.AddScoped<ITraysFastUpdateDbRepository, TraysFastUpdateDbRepository>();
            
            // Core business services
            builder.Services.AddScoped<ICableTypeService, CableTypeService>();
            builder.Services.AddScoped<ICableService, CableService>();
            builder.Services.AddScoped<ITrayService, TrayService>();
            
            // Calculation services
            builder.Services.AddScoped<ITrayCalculationService, TrayCalculationService>();
            
            // Drawing services
            builder.Services.AddScoped<ITrayDrawingService, TrayDrawingService>();
            builder.Services.AddScoped<ICableBundleDrawer, CableBundleDrawer>();
            
            // Navigation services
            builder.Services.AddScoped<TrayNavigationService>();
            
            // Export services
            builder.Services.AddScoped<IFileExportService, FileExportService>();
            builder.Services.AddScoped<IWordExportService, WordExportService>();
            builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<TraysFastUpdateDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddQuickGridEntityFrameworkAdapter();

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.UseMigrationsEndPoint();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            };

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
