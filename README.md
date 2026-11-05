# FCG Users API

Microsserviço responsável por gerenciar cadastro, autenticação JWT e autorização de usuários da plataforma FIAP Cloud Games.

## Funcionalidades

- Cadastro de usuários
- Autenticação com JWT
- Listagem de usuários (protegido)
- Publicação de evento `UserCreatedEvent` para o RabbitMQ quando um novo usuário é cadastrado

## Tecnologias

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core (InMemory Database)
- JWT Bearer Authentication
- RabbitMQ.Client
- Swagger/OpenAPI

## Variáveis de Ambiente

- `Jwt__Secret`: Chave secreta para assinatura do JWT
- `RabbitMQ__Host`: Hostname do servidor RabbitMQ (padrão: localhost)
- `RabbitMQ__Port`: Porta do RabbitMQ (padrão: 5672)
- `RabbitMQ__Username`: Usuário do RabbitMQ (padrão: guest)
- `RabbitMQ__Password`: Senha do RabbitMQ (padrão: guest)

## Como Executar

### Com .NET CLI

```bash
dotnet restore
dotnet build
dotnet run
```

### Com Docker

```bash
docker build -t fcg-users-api .
docker run -p 8080:8080 fcg-users-api
```

## Endpoints

- `POST /api/auth/register` - Registrar novo usuário
- `POST /api/auth/login` - Autenticar usuário e obter token JWT
- `GET /api/users` - Listar todos os usuários (requer autenticação)
- `GET /api/users/{id}` - Obter detalhes de um usuário (requer autenticação)

## Eventos Publicados

### UserCreatedEvent
**Routing Key:** `user.created`

```json
{
  "userId": "guid",
  "nome": "string",
  "email": "string",
  "timestamp": "datetime"
}
```
