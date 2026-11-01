using System.Net;
using System.Text.Json;
using fcg_users_api.Exceptions;

namespace fcg_users_api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            case DomainException domainEx:
                errorResponse.Codigo = domainEx.Codigo;
                errorResponse.Mensagem = domainEx.Message;
                response.StatusCode = domainEx switch
                {
                    RecursoDuplicadoException => (int)HttpStatusCode.Conflict,
                    RecursoNaoEncontradoException => (int)HttpStatusCode.NotFound,
                    RecursoExpiradoException => (int)HttpStatusCode.Gone,
                    ContaBloqueadaException => (int)HttpStatusCode.Forbidden,
                    NaoAutorizadoException => (int)HttpStatusCode.Unauthorized,
                    RegraDeNegocioException => (int)HttpStatusCode.UnprocessableEntity,
                    _ => (int)HttpStatusCode.BadRequest
                };
                _logger.LogWarning(exception, "Domain exception: {Codigo} - {Mensagem}", domainEx.Codigo, domainEx.Message);
                break;

            default:
                errorResponse.Codigo = "ERRO_INTERNO";
                errorResponse.Mensagem = "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                _logger.LogError(exception, "Unhandled exception occurred");
                break;
        }

        var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(result);
    }
}

public class ErrorResponse
{
    public string Codigo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
}
