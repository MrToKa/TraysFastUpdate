using Microsoft.EntityFrameworkCore;

namespace TraysFastUpdate.Data
{
    public class TraysFastUpdateDbContext : DbContext
    {
        public TraysFastUpdateDbContext()
        {
        }

        public TraysFastUpdateDbContext(DbContextOptions<TraysFastUpdateDbContext> options)
            : base(options)
        {
        }

        private string BuildConnectionString()
        {
            //Home settings environment
            IConfigurationRoot configuration = new ConfigurationBuilder()
                //.AddJsonFile(@"C:\Users\TOKA\source\repos\TraysFastUpdate\TraysFastUpdate\appsettings.json")
                .AddJsonFile(@"C:\Users\todor.chankov\source\repos\TraysFastUpdate\TraysFastUpdate\appsettings.json")
                .Build();

            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            return connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseNpgsql(
        BuildConnectionString(),
        options => options.UseAdminDatabase("postgres"));
        }

        public DbSet<Models.Tray> Trays { get; set; }
        public DbSet<Models.Cable> Cables { get; set; }
        public DbSet<Models.CableType> CableTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<Models.Tray>().ToTable("Trays");
            _ = modelBuilder.Entity<Models.Cable>().ToTable("Cables");
            _ = modelBuilder.Entity<Models.CableType>().ToTable("CableTypes");

            modelBuilder.Entity<Models.Cable>()
                .HasOne(c => c.CableType)
                .WithMany(ct => ct.Cables)
                .HasForeignKey(c => c.CableTypeId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
