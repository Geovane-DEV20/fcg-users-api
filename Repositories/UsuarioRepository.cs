using Microsoft.EntityFrameworkCore;
using fcg_users_api.Data;
using fcg_users_api.Models;
using fcg_users_api.Models.ValueObjects;

namespace fcg_users_api.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ApplicationDbContext _context;

    public UsuarioRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await _context.Usuarios.AddAsync(usuario, cancellationToken);
    }

    public async Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email.Valor == email.Valor, cancellationToken);
    }

    public async Task<Usuario?> ObterPorTokenDeAtivacaoAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.TokenAtivacao == token, cancellationToken);
    }

    public async Task<bool> ExisteComEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios
            .AnyAsync(u => u.Email.Valor == email.Valor, cancellationToken);
    }

    public async Task<IReadOnlyList<Usuario>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios
            .OrderBy(u => u.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ContarAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios.CountAsync(cancellationToken);
    }

    public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
