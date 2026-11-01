using fcg_users_api.Models.ValueObjects;
using fcg_users_api.Models.Enums;
using fcg_users_api.Exceptions;

namespace fcg_users_api.Models;

public class Usuario
{
    public const int MaximoTentativasFalhas = 5;
    public static readonly TimeSpan DuracaoBloqueio = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ValidadeTokenAtivacao = TimeSpan.FromHours(24);

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public Senha Senha { get; private set; } = null!;
    public Perfil Perfil { get; private set; }
    public StatusConta Status { get; private set; }
    public string? TokenAtivacao { get; private set; }
    public DateTime? TokenAtivacaoExpiraEm { get; private set; }
    public int TentativasFalhasConsecutivas { get; private set; }
    public DateTime? BloqueadaAte { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Usuario()
    {
    }

    public static Usuario CadastrarConta(string nome, Email email, string senhaEmTextoPlano, DateTime agora)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("NOME_INVALIDO", "O nome não pode ser vazio.");

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = nome.Trim(),
            Email = email,
            Perfil = Perfil.Usuario,
            Status = StatusConta.PendenteAtivacao,
            CriadoEm = agora,
        };

        usuario.Senha = Senha.Criar(senhaEmTextoPlano);
        usuario.GerarTokenDeAtivacao(agora);

        return usuario;
    }

    public string GerarTokenDeAtivacao(DateTime agora)
    {
        TokenAtivacao = Guid.NewGuid().ToString("N");
        TokenAtivacaoExpiraEm = agora.Add(ValidadeTokenAtivacao);
        return TokenAtivacao;
    }

    public void ConfirmarEmail(string token, DateTime agora)
    {
        if (Status == StatusConta.Ativa)
            throw new RegraDeNegocioException("CONTA_JA_ATIVADA", "Esta conta já foi ativada anteriormente.");

        if (Status == StatusConta.Desativada)
            throw new RegraDeNegocioException("CONTA_DESATIVADA", "Não é possível ativar uma conta desativada.");

        if (TokenAtivacao is null || !string.Equals(TokenAtivacao, token, StringComparison.Ordinal))
            throw new DomainException("TOKEN_ATIVACAO_INVALIDO", "O token de ativação informado é inválido.");

        if (TokenAtivacaoExpiraEm is null || agora > TokenAtivacaoExpiraEm.Value)
            throw new RecursoExpiradoException("TOKEN_ATIVACAO_EXPIRADO", "O token de ativação expirou.");

        Status = StatusConta.Ativa;
        TokenAtivacao = null;
        TokenAtivacaoExpiraEm = null;
    }

    public void Autenticar(string senhaEmTextoPlano, DateTime agora)
    {
        if (BloqueadaAte.HasValue && agora < BloqueadaAte.Value)
            throw new ContaBloqueadaException("TENTATIVA_DE_ACESSO_BLOQUEADA", "Conta temporariamente bloqueada por tentativas sucessivas de autenticação inválidas.");

        if (Status != StatusConta.Ativa || !Senha.Confere(senhaEmTextoPlano))
        {
            RegistrarFalhaDeAutenticacao(agora);
            return;
        }

        TentativasFalhasConsecutivas = 0;
        BloqueadaAte = null;
    }

    private void RegistrarFalhaDeAutenticacao(DateTime agora)
    {
        TentativasFalhasConsecutivas++;

        if (TentativasFalhasConsecutivas >= MaximoTentativasFalhas)
        {
            BloqueadaAte = agora.Add(DuracaoBloqueio);
            TentativasFalhasConsecutivas = 0;
            throw new ContaBloqueadaException("TENTATIVA_DE_ACESSO_BLOQUEADA", "Conta bloqueada após tentativas sucessivas de autenticação inválidas.");
        }

        throw new NaoAutorizadoException("AUTENTICACAO_RECUSADA", "E-mail ou senha inválidos.");
    }

    public void AtualizarDados(string novoNome, DateTime agora)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new DomainException("NOME_INVALIDO", "O nome não pode ser vazio.");

        Nome = novoNome.Trim();
    }

    public void AlterarSenha(string senhaAtual, string novaSenha, DateTime agora)
    {
        if (!Senha.Confere(senhaAtual))
            throw new NaoAutorizadoException("AUTENTICACAO_RECUSADA", "A senha atual informada está incorreta.");

        Senha = Senha.Criar(novaSenha);
    }

    public void AlterarPerfil(Perfil novoPerfil, DateTime agora)
    {
        Perfil = novoPerfil;
    }

    public void DesativarConta(DateTime agora)
    {
        if (Status == StatusConta.Desativada)
            throw new RegraDeNegocioException("CONTA_JA_DESATIVADA", "Esta conta já está desativada.");

        Status = StatusConta.Desativada;
    }
}
