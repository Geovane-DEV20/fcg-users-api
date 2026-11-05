using FcgUsersApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FcgUsersApi.Data;

public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}
