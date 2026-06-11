using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Pm.Data;

namespace Pm.Script
{
    class Program
    {
        static void Main(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=pm.db");

            using (var context = new AppDbContext(optionsBuilder.Options))
            {
                var users = context.Users.Where(u => u.Username == "worskhop" || u.FullName.Contains("Yoel")).ToList();
                foreach (var u in users)
                {
                    Console.WriteLine($"User: {u.UserId} | {u.Username} | {u.FullName} | Role: {u.RoleId}");
                }
            }
        }
    }
}
