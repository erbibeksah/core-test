
namespace eRaptors.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ShortenedUrl> ShortenedUrls { get; set; }
        public DbSet<UrlVisit> UrlVisits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ShortenedUrl>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.LongUrl)
                    .IsRequired()
                    .HasMaxLength(2048);

                entity.Property(e => e.ShortUrl)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(7);

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.HasIndex(e => e.Code)
                    .IsUnique();

                // One-to-many relationship with UrlVisit
                entity.HasMany(e => e.Visits)
                    .WithOne()
                    .HasForeignKey(e => e.ShortenedUrlId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UrlVisit>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.IpAddress)
                    .IsRequired()
                    .HasMaxLength(45); // To accommodate both IPv4 and IPv6

                entity.Property(e => e.VirtualLocation)
                    .IsRequired()
                    .HasMaxLength(2048);

                entity.Property(e => e.VisitedAt)
                    .IsRequired();
            });
        }
    }
}
