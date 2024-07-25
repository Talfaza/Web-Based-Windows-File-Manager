using Microsoft.EntityFrameworkCore;
using WebBasedFileManager.Models;

namespace WebBasedFileManager.Data
{
    public class FileManagerContext : DbContext
    {
        public FileManagerContext(DbContextOptions<FileManagerContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
