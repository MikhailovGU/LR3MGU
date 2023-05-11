using Microsoft.EntityFrameworkCore;
using stankin3.Models;

namespace stankin3.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base (options) { }

    public DbSet<Rate> Rates { get; set; }
}
