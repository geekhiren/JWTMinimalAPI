using JWTMinimalAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace JWTMinimalAPI.Migrations
{
    public class DBContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; }
        public DbSet<User> Users { get; set; }

        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
        }
    }
}
