using Microsoft.EntityFrameworkCore;

namespace AccessForm.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<RequestType> RequestTypes { get; set; }
    public DbSet<Request> AccessRequests { get; set; }
    public DbSet<NoticeOfTransferRequest> NoticeOfTransferRequests { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Seed initial request types
        modelBuilder.Entity<RequestType>().HasData(
            new RequestType { Id = 1, Name = "Заявка на предоставление доступа" },
            new RequestType { Id = 2, Name = "Уведомление о переводе" },
            new RequestType { Id = 3, Name = "Заявка на прекращение доступа" }
        );
    }
} 