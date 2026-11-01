using System.Security.Cryptography;
using fcg_users_api.Exceptions;

namespace fcg_users_api.Models.ValueObjects;

public sealed class Senha
{
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;
    private const int Iteracoes = 100_000;

    public string Hash { get; }

    private Senha(string hash)
    {
        Hash = hash;
    }

    public static Senha Criar(string senhaEmTextoPlano)
    {
        Validar(senhaEmTextoPlano);
        return new Senha(GerarHash(senhaEmTextoPlano));
    }

    public static Senha ReconstruirDoHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new DomainException("SENHA_HASH_INVALIDO", "O hash da senha não pode ser vazio.");

        return new Senha(hash);
    }

    public bool Confere(string senhaEmTextoPlano)
    {
        var partes = Hash.Split('.');
        var salt = Convert.FromBase64String(partes[0]);
        var hashArmazenado = Convert.FromBase64String(partes[1]);

        var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(senhaEmTextoPlano, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);

        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashArmazenado);
    }

    private static void Validar(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
            throw new DomainException("SENHA_FRACA", "A senha deve ter no mínimo 8 caracteres.");

        var temLetra = senha.Any(char.IsLetter);
        var temNumero = senha.Any(char.IsDigit);
        var temCaractereEspecial = senha.Any(c => !char.IsLetterOrDigit(c));

        if (!temLetra || !temNumero || !temCaractereEspecial)
            throw new DomainException("SENHA_FRACA", "A senha deve conter letra, número e caractere especial.");
    }

    private static string GerarHash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}
