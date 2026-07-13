using Microsoft.EntityFrameworkCore;

namespace SistemaERP.Areas.Admin.Models
{
    public class AdminContext : DbContext
    {
        public AdminContext(DbContextOptions<AdminContext> options)
            : base(options)
        {
        }

        public DbSet<Rubro> Rubros { get; set; } = null!;
        public DbSet<Empresa> Empresas { get; set; } = null!;
        public DbSet<AdminUsuario> AdminUsuarios { get; set; } = null!;
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; } = null!;
        public DbSet<BackupRegistro> Backups { get; set; } = null!;
        public DbSet<CampoPersonalizado> CamposPersonalizados { get; set; } = null!;
        public DbSet<PlantillaRubro> PlantillasRubros { get; set; } = null!;
        public DbSet<UsuarioEmpresa> UsuariosEmpresa { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapeo de relaciones explícitas para evitar cascading deletes circulares
            modelBuilder.Entity<Empresa>()
                .HasOne(e => e.Rubro)
                .WithMany(r => r.Empresas)
                .HasForeignKey(e => e.RubroId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BackupRegistro>()
                .HasOne(b => b.Empresa)
                .WithMany(e => e.Backups)
                .HasForeignKey(b => b.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BackupRegistro>()
                .HasOne(b => b.AdminUsuario)
                .WithMany(u => u.Backups)
                .HasForeignKey(b => b.AdminUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdminAuditLog>()
                .HasOne(a => a.AdminUsuario)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.AdminUsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminAuditLog>()
                .HasOne(a => a.Empresa)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(a => a.EmpresaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CampoPersonalizado>()
                .HasOne(c => c.Empresa)
                .WithMany(e => e.CamposPersonalizados)
                .HasForeignKey(c => c.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampoPersonalizado>()
                .HasOne(c => c.Rubro)
                .WithMany(r => r.CamposPersonalizados)
                .HasForeignKey(c => c.RubroId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CampoPersonalizado>()
                .HasOne(c => c.AdminUsuario)
                .WithMany(u => u.CamposPersonalizados)
                .HasForeignKey(c => c.CreadoPorAdmin)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PlantillaRubro>()
                .HasOne(p => p.Rubro)
                .WithMany(r => r.Plantillas)
                .HasForeignKey(p => p.RubroId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsuarioEmpresa>()
                .HasOne(u => u.Empresa)
                .WithMany()
                .HasForeignKey(u => u.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UsuarioEmpresa>()
                .HasIndex(u => new { u.Login, u.EmpresaId })
                .IsUnique();
        }
    }
}
