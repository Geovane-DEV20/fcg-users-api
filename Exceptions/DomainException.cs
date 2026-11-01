namespace fcg_users_api.Exceptions;

public class DomainException : Exception
{
    public string Codigo { get; }

    public DomainException(string codigo, string mensagem) : base(mensagem)
    {
        Codigo = codigo;
    }
}

public class RegraDeNegocioException : DomainException
{
    public RegraDeNegocioException(string codigo, string mensagem) : base(codigo, mensagem)
    {
    }
}

public class RecursoExpiradoException : DomainException
{
    public RecursoExpiradoException(string codigo, string mensagem) : base(codigo, mensagem)
    {
    }
}

public class RecursoDuplicadoException : DomainException
{
    public RecursoDuplicadoException(string codigo, string mensagem) : base(codigo, mensagem)
    {
    }
}

public class ContaBloqueadaException : DomainException
{
    public ContaBloqueadaException(string codigo, string mensagem) : base(codigo, mensagem)
    {
    }
}

public class NaoAutorizadoException : DomainException
{
    public NaoAutorizadoException(string codigo, string mensagem) : base(codigo, mensagem)
    {
    }
}

public class RecursoNaoEncontradoException : DomainException
{
    public RecursoNaoEncontradoException(string codigo, string mensagem) : base(codigo, mensagem)
    {
    }
}
