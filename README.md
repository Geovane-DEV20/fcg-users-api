# FCG Users API - Microsserviço de Usuários

API de microsserviço para gerenciamento de cadastro de usuários, extraída do monolito **Fiap.Cloud.Games** seguindo os padrões DDD (Domain-Driven Design).

> 🎯 **Objetivo:** Separar a lógica de cadastro de usuários em um microsserviço independente, mantendo 100% das regras de negócio do monolito original.

## 📋 Funcionalidades

- ✅ **Cadastro de usuários** com validação robusta
- ✅ **Validação de email** com regex
- ✅ **Validação de senha** (mínimo 8 caracteres, letra, número e caractere especial)
- ✅ **Hash de senha** usando PBKDF2 com SHA256
- ✅ **Token de ativação** com expiração de 24 horas
- ✅ **Confirmação de email** via token
- ✅ **Atualização de dados** cadastrais
- ✅ **Alteração de senha** com verificação da senha atual
- ✅ **Listagem paginada** de usuários
- ✅ **Proteção contra tentativas de login** (bloqueio após 5 falhas)
- ✅ **Status da conta** (Pendente Ativação, Ativa, Desativada)
- ✅ **Perfis de usuário** (Usuario, Administrador)

## 🏗️ Arquitetura

A API foi desenvolvida seguindo os princípios de **Domain-Driven Design (DDD)**:

```
fcg-users-api/
├── Controllers/           # Camada de apresentação (API endpoints)
├── Services/             # Casos de uso / Application layer
├── Models/               # Entidades de domínio
│   ├── ValueObjects/    # Objetos de valor (Email, Senha)
│   └── Enums/           # Enumerações (Perfil, StatusConta)
├── Repositories/         # Interfaces e implementações de repositório
├── Data/                # Contexto do Entity Framework
├── DTOs/                # Data Transfer Objects
├── Exceptions/          # Exceções de domínio customizadas
└── Middlewares/         # Middleware de tratamento de exceções
```

## 🚀 Endpoints da API

### Cadastro de Usuário
```http
POST /api/usuarios
Content-Type: application/json

{
  "nome": "João Silva",
  "email": "joao@example.com",
  "senha": "SenhaForte123!"
}
```

**Resposta (201 Created):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Confirmação de Email
```http
POST /api/usuarios/ativacao
Content-Type: application/json

{
  "token": "abc123def456"
}
```

**Resposta:** 204 No Content

### Obter Usuário por ID
```http
GET /api/usuarios/{id}
```

**Resposta (200 OK):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nome": "João Silva",
  "email": "joao@example.com",
  "perfil": "Usuario",
  "status": "Ativa",
  "criadoEm": "2024-01-15T10:30:00Z"
}
```

### Atualizar Dados
```http
PUT /api/usuarios/{id}
Content-Type: application/json

{
  "nome": "João da Silva"
}
```

### Alterar Senha
```http
PUT /api/usuarios/{id}/senha
Content-Type: application/json

{
  "senhaAtual": "SenhaAntiga123!",
  "novaSenha": "SenhaNova456!"
}
```

### Listar Usuários (Paginado)
```http
GET /api/usuarios?pagina=1&tamanhoPagina=20
```

## 🔒 Regras de Negócio

### Validação de Email
- Formato válido (regex: `^[^@\s]+@[^@\s]+\.[^@\s]+$`)
- Único no sistema (não permite duplicação)

### Validação de Senha
- Mínimo de 8 caracteres
- Deve conter pelo menos:
  - Uma letra
  - Um número
  - Um caractere especial
- Hash gerado com PBKDF2 (100.000 iterações, SHA256)

### Token de Ativação
- Gerado automaticamente no cadastro
- Validade de 24 horas
- Invalidado após confirmação

### Bloqueio de Conta
- Após 5 tentativas consecutivas de autenticação falhadas
- Bloqueio temporário de 15 minutos
- Contador zerado após login bem-sucedido

### Status da Conta
1. **PendenteAtivacao**: Conta recém-criada aguardando confirmação de email
2. **Ativa**: Conta confirmada e pronta para uso
3. **Desativada**: Conta desabilitada (não pode ser reativada)

## 🛠️ Tecnologias Utilizadas

- **.NET 8.0**
- **ASP.NET Core Web API**
- **Entity Framework Core 8.0**
- **Entity Framework InMemory** (desenvolvimento)
- **SQL Server** (produção - configurável)
- **Swagger/OpenAPI** (documentação)

## 🗄️ Banco de Dados Dedicado

Esta API segue o padrão **Database per Service**:

✅ **Banco próprio**: `FcgUsersDb` (SQL Server)  
✅ **Acesso exclusivo**: Apenas esta API acessa este banco  
✅ **Isolamento total**: Outros microsserviços comunicam via HTTP/eventos  
✅ **Migrations independentes**: Evolução autônoma do schema  

> 📖 Para mais detalhes, veja [Estratégia de Banco de Dados](Documentacao/ESTRATEGIA_BANCO_DADOS.md)

## 📦 Instalação e Execução

### Pré-requisitos
- .NET 8.0 SDK
- SQL Server (opcional, para produção)

### Passos

1. **Restaurar pacotes NuGet:**
```powershell
dotnet restore
```

2. **Executar a aplicação:**
```powershell
dotnet run
```

3. **Acessar o Swagger:**
```
https://localhost:5001/swagger
```

### Configuração do Banco de Dados

**Para Desenvolvimento (InMemory - Padrão):**  
O banco está configurado para usar InMemory por padrão no `Program.cs`.  
✅ Sem configuração necessária  
✅ Dados são perdidos ao reiniciar (ideal para testes)

**Para Produção (SQL Server):**

1. Edite `Program.cs` e comente a linha do InMemory
2. Descomente a linha do SQL Server
3. Configure a connection string em `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SEU_SERVIDOR;Database=FcgUsersDb;User Id=usuario;Password=senha;TrustServerCertificate=True"
  }
}
```

4. Execute as migrations:

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> 💡 **Importante**: Esta API tem seu **próprio banco de dados dedicado** (`FcgUsersDb`). Outros microsserviços NÃO devem acessar este banco diretamente.

## 🔄 Próximos Passos (TODOs)

- [ ] Implementar message broker (RabbitMQ/Azure Service Bus) para comunicação assíncrona
- [ ] Adicionar eventos de domínio (UsuarioCriado, EmailConfirmado, etc.)
- [ ] Implementar publicação de eventos no CadastroService
- [ ] Implementar envio de email de ativação via evento
- [ ] Adicionar autenticação JWT
- [ ] Implementar autorização baseada em perfis
- [ ] Implementar health checks (incluindo verificação do banco de dados e message broker)
- [ ] Adicionar logging estruturado (Serilog)
- [ ] Implementar rate limiting
- [ ] Adicionar testes unitários e de integração
- [ ] Configurar CI/CD pipeline
- [ ] Deploy em ambiente de produção com SQL Server dedicado

> 📖 **Leia também:**
> - [Comunicação Assíncrona](Documentacao/COMUNICACAO_ASSINCRONA.md) - **Event-Driven Architecture**
> - [Estratégia de Banco de Dados](Documentacao/ESTRATEGIA_BANCO_DADOS.md) - Como funciona o banco dedicado
> - [Diagrama de Microsserviços](Documentacao/DIAGRAMA_MICROSSERVICOS.md) - Arquitetura completa
> - [Arquitetura](Documentacao/ARQUITETURA.md) - Visão detalhada da estrutura
> - [Comparação com Monolito](Documentacao/COMPARACAO_MONOLITO.md) - O que mudou
> - [Guia Rápido](Documentacao/GUIA_RAPIDO.md) - Tutorial prático

## 📝 Tratamento de Erros

A API retorna erros padronizados no formato:

```json
{
  "codigo": "EMAIL_DUPLICADO_DETECTADO",
  "mensagem": "Já existe uma conta cadastrada com este e-mail."
}
```

### Códigos de Erro Comuns

- `NOME_INVALIDO`: Nome vazio ou inválido
- `EMAIL_INVALIDO`: Formato de email inválido
- `EMAIL_DUPLICADO_DETECTADO`: Email já cadastrado
- `SENHA_FRACA`: Senha não atende aos requisitos
- `TOKEN_ATIVACAO_INVALIDO`: Token de ativação não encontrado
- `TOKEN_ATIVACAO_EXPIRADO`: Token expirou (>24h)
- `CONTA_JA_ATIVADA`: Tentativa de reativar conta já ativa
- `USUARIO_NAO_ENCONTRADO`: ID de usuário não existe
- `AUTENTICACAO_RECUSADA`: Senha incorreta
- `TENTATIVA_DE_ACESSO_BLOQUEADA`: Conta bloqueada temporariamente

## 📄 Licença

Este projeto faz parte do trabalho acadêmico da FIAP.
