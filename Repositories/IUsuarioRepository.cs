using fcg_users_api.Models;
using fcg_users_api.Models.ValueObjects;

namespace fcg_users_api.Repositories;

public interface IUsuarioRepository
{
    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<Usuario?> ObterPorTokenDeAtivacaoAsync(string token, CancellationToken cancellationToken = default);

    Task<bool> ExisteComEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> ListarAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken = default);

    Task<int> ContarAsync(CancellationToken cancellationToken = default);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
