# Arquitetura da API de Usuários

## 📐 Visão Geral

A API de usuários segue os princípios de **Domain-Driven Design (DDD)** e **Clean Architecture**, separando as responsabilidades em camadas bem definidas.

## 🏛️ Camadas da Aplicação

```
┌─────────────────────────────────────────────────────────────┐
│                    Controllers Layer                        │
│                    (API Endpoints)                          │
│                   UsersController.cs                        │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│                    (Use Cases)                              │
│                  CadastroService.cs                         │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     Domain Layer                            │
│                   (Business Logic)                          │
│  ┌──────────────┐  ┌─────────────┐  ┌──────────────┐      │
│  │   Usuario    │  │ ValueObjects│  │    Enums     │      │
│  │ (Aggregate)  │  │ Email/Senha │  │Perfil/Status │      │
│  └──────────────┘  └─────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                        │
│                (Data Access & External)                     │
│  ┌──────────────────┐  ┌───────────────────────────┐      │
│  │UsuarioRepository │  │  ApplicationDbContext     │      │
│  └──────────────────┘  └───────────────────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

## 📦 Componentes Principais

### 1. Controllers (Presentation Layer)
**Responsabilidade:** Receber requisições HTTP, validar entrada e retornar respostas.

- `UsersController.cs` - Endpoints da API REST

**Endpoints:**
- `POST /api/usuarios` - Cadastrar usuário
- `POST /api/usuarios/ativacao` - Confirmar email
- `GET /api/usuarios/{id}` - Obter usuário
- `PUT /api/usuarios/{id}` - Atualizar dados
- `PUT /api/usuarios/{id}/senha` - Alterar senha
- `GET /api/usuarios` - Listar usuários

### 2. Services (Application Layer)
**Responsabilidade:** Orquestrar casos de uso e coordenar o fluxo entre domínio e infraestrutura.

- `CadastroService.cs` - Casos de uso de cadastro e gerenciamento

**Métodos principais:**
- `CadastrarUsuarioAsync()` - Valida email único e cria usuário
- `ConfirmarEmailAsync()` - Ativa conta via token
- `AtualizarDadosAsync()` - Atualiza informações
- `AlterarSenhaAsync()` - Troca senha com verificação

### 3. Models (Domain Layer)
**Responsabilidade:** Conter a lógica de negócio e regras de domínio.

#### Aggregate Root
- **`Usuario.cs`** - Entidade raiz com toda lógica de negócio
  - Cadastro de conta
  - Geração e validação de token
  - Autenticação e bloqueio
  - Alteração de dados e senha

#### Value Objects
- **`Email.cs`** - Validação de formato de email
- **`Senha.cs`** - Hash, validação de força e comparação

#### Enums
- **`Perfil.cs`** - Usuario, Administrador
- **`StatusConta.cs`** - PendenteAtivacao, Ativa, Desativada

### 4. Repositories (Infrastructure Layer)
**Responsabilidade:** Acesso a dados e persistência.

- `IUsuarioRepository.cs` - Contrato
- `UsuarioRepository.cs` - Implementação com EF Core

**Operações:**
- Adicionar, Obter, Listar usuários
- Verificar existência de email
- Buscar por token de ativação

### 5. Data (Infrastructure Layer)
**Responsabilidade:** Configuração do ORM e mapeamento.

- `ApplicationDbContext.cs` - Contexto do Entity Framework
  - Mapeamento de entidades
  - Conversão de Value Objects
  - Configuração de índices únicos

### 6. DTOs (Data Transfer Objects)
**Responsabilidade:** Contratos de entrada/saída da API.

- `CadastrarUsuarioRequest/Response`
- `ConfirmarEmailRequest`
- `AtualizarDadosRequest`
- `AlterarSenhaRequest`
- `UsuarioResponse`

### 7. Exceptions (Domain Layer)
**Responsabilidade:** Exceções de domínio tipadas.

- `DomainException` - Base
- `RegraDeNegocioException`
- `RecursoDuplicadoException`
- `RecursoExpiradoException`
- `ContaBloqueadaException`
- `NaoAutorizadoException`
- `RecursoNaoEncontradoException`

### 8. Middlewares (Cross-cutting)
**Responsabilidade:** Preocupações transversais.

- `ExceptionHandlingMiddleware.cs` - Tratamento global de exceções

## 🔄 Fluxo de Cadastro de Usuário

```
┌──────────┐       ┌────────────┐       ┌─────────────┐       ┌────────────┐
│  Client  │──────>│ Controller │──────>│   Service   │──────>│  Repository│
└──────────┘       └────────────┘       └─────────────┘       └────────────┘
     │                    │                     │                     │
     │ POST /usuarios     │                     │                     │
     │─────────────────>  │                     │                     │
     │                    │ CadastrarUsuario    │                     │
     │                    │──────────────────> │                     │
     │                    │                     │ ExisteComEmail?    │
     │                    │                     │──────────────────> │
     │                    │                     │ <──────────────────│
     │                    │                     │ false              │
     │                    │                     │                     │
     │                    │                     │ Usuario.CadastrarConta()
     │                    │                     │ (Domain Logic)     │
     │                    │                     │                     │
     │                    │                     │ AdicionarAsync     │
     │                    │                     │──────────────────> │
     │                    │                     │ SalvarAlteracoes   │
     │                    │                     │──────────────────> │
     │                    │ <──────────────────│                     │
     │                    │ Response(ID)        │                     │
     │ <──────────────────│                     │                     │
     │ 201 Created        │                     │                     │
```

## 🔐 Validações e Regras de Negócio

### Validações no Value Object Email
```csharp
// Regex: ^[^@\s]+@[^@\s]+\.[^@\s]+$
- Formato válido de email
- Não pode ser nulo ou vazio
```

### Validações no Value Object Senha
```csharp
- Mínimo 8 caracteres
- Pelo menos uma letra
- Pelo menos um número
- Pelo menos um caractere especial
- Hash com PBKDF2 (100k iterações, SHA256)
```

### Validações na Entidade Usuario
```csharp
- Nome não pode ser vazio
- Email único no sistema
- Token de ativação expira em 24h
- Bloqueio após 5 tentativas falhadas (15min)
- Status deve seguir fluxo: Pendente → Ativa → Desativada
```

## 🗄️ Modelo de Dados

```sql
Table: Usuarios
├── Id (GUID, PK)
├── Nome (VARCHAR(200))
├── Email (VARCHAR(255), UNIQUE INDEX)
├── SenhaHash (VARCHAR(500))
├── Perfil (INT) -- 1: Usuario, 2: Administrador
├── Status (INT) -- 1: PendenteAtivacao, 2: Ativa, 3: Desativada
├── TokenAtivacao (VARCHAR(100), INDEX)
├── TokenAtivacaoExpiraEm (DATETIME, NULL)
├── TentativasFalhasConsecutivas (INT)
├── BloqueadaAte (DATETIME, NULL)
└── CriadoEm (DATETIME)
```

## 🚀 Extensibilidade

### Pontos de Extensão

1. **Eventos de Domínio** (TODO)
   - Publicar eventos ao cadastrar usuário
   - Integrar com message broker (RabbitMQ, Azure Service Bus)

2. **Envio de Email** (TODO)
   - Implementar `IServicoDeEmail`
   - Enviar email de ativação assíncrono

3. **Autenticação JWT** (TODO)
   - Adicionar geração de tokens JWT
   - Implementar middleware de autorização

4. **Observabilidade** (TODO)
   - Adicionar telemetria (OpenTelemetry)
   - Logs estruturados (Serilog)
   - Metrics e tracing

5. **Cache** (TODO)
   - Redis para cache de consultas
   - Cache distribuído para tokens

## 📊 Padrões Utilizados

- **Repository Pattern** - Abstração de acesso a dados
- **Aggregate Root** - Usuario como raiz de agregação
- **Value Objects** - Email e Senha imutáveis
- **Factory Method** - `Usuario.CadastrarConta()`
- **Dependency Injection** - Inversão de controle
- **Middleware Pattern** - Tratamento de exceções
- **DTO Pattern** - Separação de contratos da API

## 🔄 Próximas Etapas

1. ✅ Implementar lógica de cadastro
2. ✅ Validações de domínio
3. ✅ Tratamento de exceções
4. ⏳ Adicionar testes unitários
5. ⏳ Implementar autenticação JWT
6. ⏳ Integração com serviço de email
7. ⏳ Adicionar eventos de domínio
8. ⏳ Documentação OpenAPI completa
9. ⏳ Containerização (Docker)
10. ⏳ CI/CD pipeline
