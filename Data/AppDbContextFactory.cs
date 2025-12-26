// AppDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pm.Data;

namespace Pm.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseMySql(
                "Server=203.153.127.45;Port=3306;Database=mknpm;User Id=sql_pm_web;Password=$mknsmart123$;CharSet=utf8mb4;",
                ServerVersion.AutoDetect("Server=203.153.127.45;Port=3306;Database=mknpm;User Id=sql_pm_web;Password=$mknsmart123$;")
            );
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}