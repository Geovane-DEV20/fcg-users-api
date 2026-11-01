using Microsoft.EntityFrameworkCore;
using fcg_users_api.Models;
using fcg_users_api.Models.ValueObjects;

namespace fcg_users_api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(200);

            entity.OwnsOne(e => e.Email, email =>
            {
                email.Property(e => e.Valor)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("Email");

                email.HasIndex(e => e.Valor)
                    .IsUnique();
            });

            entity.OwnsOne(e => e.Senha, senha =>
            {
                senha.Property(s => s.Hash)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasColumnName("SenhaHash");
            });

            entity.Property(e => e.Perfil)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.TokenAtivacao)
                .HasMaxLength(100);

            entity.Property(e => e.TokenAtivacaoExpiraEm);

            entity.Property(e => e.TentativasFalhasConsecutivas)
                .IsRequired();

            entity.Property(e => e.BloqueadaAte);

            entity.Property(e => e.CriadoEm)
                .IsRequired();

            entity.HasIndex(e => e.TokenAtivacao);
        });
    }
}
