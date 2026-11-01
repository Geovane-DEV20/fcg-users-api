using System.Text.RegularExpressions;
using fcg_users_api.Exceptions;

namespace fcg_users_api.Models.ValueObjects;

public sealed partial class Email : IEquatable<Email>
{
    public string Valor { get; }

    public Email(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || !FormatoValidoRegex().IsMatch(valor))
            throw new DomainException("EMAIL_INVALIDO", "O e-mail informado não tem um formato válido.");

        Valor = valor;
    }

    public bool Equals(Email? outro)
    {
        if (outro is null) return false;
        return string.Equals(Valor, outro.Valor, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Valor.ToLowerInvariant().GetHashCode();

    public override string ToString() => Valor;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex FormatoValidoRegex();
}
