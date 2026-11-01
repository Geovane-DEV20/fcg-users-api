# Comparação: Monolito vs Microsserviço

## 📋 Visão Geral

Este documento compara a implementação original no monolito **Fiap.Cloud.Games** com a nova implementação no microsserviço **fcg-users-api**.

## 🏗️ Estrutura de Arquivos

### Monolito (Fiap.Cloud.Games)
```
Games.Core/
├── Identidade/
│   ├── Usuario.cs                    (Entidade de domínio)
│   ├── Perfil.cs                     (Enum)
│   ├── StatusConta.cs                (Enum)
│   ├── IUsuarioRepository.cs         (Interface)
│   └── CasosDeUso/
│       ├── CadastroCasosDeUso.cs     (Use cases)
│       └── UsuarioDtos.cs            (DTOs)
├── ObjetosDeValor/
│   ├── Email.cs                      (Value object)
│   └── Senha.cs                      (Value object)
└── Common/
    └── AggregateRoot.cs              (Base class)

Games.Infrastructure/
├── Repositorios/
│   └── UsuarioRepository.cs          (Implementação)
└── Mapeamentos/
    └── UsuarioConfiguration.cs       (EF mapping)

FIAP.Cloud.Games/
└── Controllers/
    └── UsuariosController.cs         (API endpoints)
```

### Microsserviço (fcg-users-api)
```
fcg-users-api/
├── Controllers/
│   └── UsersController.cs            (API endpoints)
├── Services/
│   └── CadastroService.cs            (Use cases consolidados)
├── Models/
│   ├── Usuario.cs                    (Entidade de domínio)
│   ├── ValueObjects/
│   │   ├── Email.cs                  (Value object)
│   │   └── Senha.cs                  (Value object)
│   └── Enums/
│       ├── Perfil.cs                 (Enum)
│       └── StatusConta.cs            (Enum)
├── Repositories/
│   ├── IUsuarioRepository.cs         (Interface)
│   └── UsuarioRepository.cs          (Implementação)
├── Data/
│   └── ApplicationDbContext.cs       (EF mapping)
├── DTOs/
│   └── UsuarioDtos.cs                (DTOs)
├── Exceptions/
│   └── DomainException.cs            (Exceções)
└── Middlewares/
    └── ExceptionHandlingMiddleware.cs
```

## 🔄 Mapeamento de Componentes

| Monolito | Microsserviço | Mudanças |
|----------|---------------|----------|
| `Games.Core/Identidade/Usuario.cs` | `Models/Usuario.cs` | ✅ Código idêntico, removidos eventos |
| `Games.Core/ObjetosDeValor/Email.cs` | `Models/ValueObjects/Email.cs` | ✅ Código idêntico |
| `Games.Core/ObjetosDeValor/Senha.cs` | `Models/ValueObjects/Senha.cs` | ✅ Código idêntico |
| `Games.Core/Identidade/Perfil.cs` | `Models/Enums/Perfil.cs` | ✅ Código idêntico |
| `Games.Core/Identidade/StatusConta.cs` | `Models/Enums/StatusConta.cs` | ✅ Código idêntico |
| `Games.Core/Identidade/CasosDeUso/*` | `Services/CadastroService.cs` | ⚠️ Consolidado em um serviço |
| `Games.Core/Identidade/IUsuarioRepository.cs` | `Repositories/IUsuarioRepository.cs` | ➕ Adicionado `SalvarAlteracoesAsync()` |
| `Games.Infrastructure/Repositorios/UsuarioRepository.cs` | `Repositories/UsuarioRepository.cs` | ✅ Implementação similar |
| `Games.Infrastructure/Mapeamentos/UsuarioConfiguration.cs` | `Data/ApplicationDbContext.cs` | ⚠️ Configuração inline no DbContext |
| `FIAP.Cloud.Games/Controllers/UsuariosController.cs` | `Controllers/UsersController.cs` | ⚠️ Simplificado para cadastro |

## 🔍 Diferenças Principais

### 1. Eventos de Domínio

**Monolito:**
```csharp
// Usuario.cs herda de AggregateRoot
public sealed class Usuario : AggregateRoot
{
    public static Usuario CadastrarConta(...)
    {
        // ...
        usuario.AdicionarEvento(new SenhaDefinida(usuario.Id, agora));
        usuario.AdicionarEvento(new UsuarioRegistrado(usuario.Id, email.Valor, agora));
        return usuario;
    }
}
```

**Microsserviço:**
```csharp
// Usuario.cs não possui eventos (por enquanto)
public class Usuario
{
    public static Usuario CadastrarConta(...)
    {
        // ... mesma lógica sem eventos
        return usuario;
    }
}
```

**Motivo:** Eventos serão adicionados posteriormente com integração a message broker.

### 2. Unit of Work

**Monolito:**
```csharp
public class CadastrarUsuarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<CadastrarUsuarioResponse> ExecutarAsync(...)
    {
        // ...
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }
}
```

**Microsserviço:**
```csharp
public class CadastroService
{
    private readonly IUsuarioRepository _usuarioRepository;
    
    public async Task<CadastrarUsuarioResponse> CadastrarUsuarioAsync(...)
    {
        // ...
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
    }
}
```

**Motivo:** Simplificação para o microsserviço focado. O repositório gerencia o DbContext diretamente.

### 3. Casos de Uso

**Monolito:**
```
- CadastrarUsuarioUseCase
- ConfirmarEmailUseCase
- ObterUsuarioLogadoUseCase
- AtualizarDadosUseCase
- AlterarSenhaUseCase
- ListarUsuariosUseCase
- ObterUsuarioPorIdUseCase
- AlterarPerfilUsuarioUseCase
- DesativarUsuarioUseCase
```

**Microsserviço:**
```
- CadastroService (consolida todos os use cases)
```

**Motivo:** Por ser um microsserviço focado, consolidamos em um único serviço. Pode ser separado no futuro se necessário.

### 4. Serviços Externos

**Monolito:**
```csharp
public class CadastrarUsuarioUseCase
{
    private readonly IServicoDeEmail _servicoDeEmail;
    private readonly IBibliotecaProvisionador _bibliotecaProvisionador;
    
    // Envia email de ativação
    await _servicoDeEmail.EnviarEmailDeAtivacaoAsync(...);
    
    // Provisiona biblioteca inicial
    await _bibliotecaProvisionador.ProvisionarAsync(...);
}
```

**Microsserviço:**
```csharp
public class CadastroService
{
    // TODO: Implementar comunicação via mensageria
    // TODO: Enviar email de ativação com o token
    // TODO: Provisionar recursos via eventos
}
```

**Motivo:** Comunicação entre microsserviços será implementada via message broker (RabbitMQ, Azure Service Bus, etc.).

## ✅ Funcionalidades Mantidas

Todas as regras de negócio do monolito foram mantidas:

| Funcionalidade | Status | Notas |
|----------------|--------|-------|
| ✅ Validação de email (formato) | Mantida | Mesmo regex |
| ✅ Validação de senha (força) | Mantida | Mesmos requisitos |
| ✅ Hash PBKDF2 com SHA256 | Mantida | 100k iterações |
| ✅ Verificação de email duplicado | Mantida | Consulta ao repositório |
| ✅ Token de ativação (24h) | Mantida | Mesmo tempo de expiração |
| ✅ Confirmação de email | Mantida | Mesma lógica |
| ✅ Bloqueio por tentativas (5x, 15min) | Mantida | Mesmos limites |
| ✅ Status da conta (Pendente/Ativa/Desativada) | Mantida | Mesmo fluxo |
| ✅ Perfis (Usuario/Administrador) | Mantida | Mesmos valores |
| ✅ Atualização de dados | Mantida | Mesmas validações |
| ✅ Alteração de senha | Mantida | Com verificação |

## ⏳ Funcionalidades Pendentes

| Funcionalidade | Monolito | Microsserviço | Planejamento |
|----------------|----------|---------------|--------------|
| Envio de email de ativação | ✅ Implementado | ⏳ TODO | Via message broker |
| Provisionamento de biblioteca | ✅ Implementado | ⏳ TODO | Evento para outro serviço |
| Autenticação JWT | ✅ Implementado | ⏳ TODO | Próxima fase |
| Autorização por perfil | ✅ Implementado | ⏳ TODO | Com JWT |
| Eventos de domínio | ✅ Implementado | ⏳ TODO | Com event bus |
| Logout/Sessão | ✅ Implementado | ⏳ TODO | Gestão de tokens |

## 📊 Comparação de Exceções

Todas as exceções do monolito foram replicadas:

```csharp
✅ DomainException
✅ RegraDeNegocioException
✅ RecursoDuplicadoException
✅ RecursoExpiradoException
✅ ContaBloqueadaException
✅ NaoAutorizadoException
✅ RecursoNaoEncontradoException
```

## 🔄 Endpoints API

### Endpoints Implementados

| Endpoint | Método | Monolito | Microsserviço |
|----------|--------|----------|---------------|
| `/api/usuarios` | POST | ✅ | ✅ |
| `/api/usuarios/ativacao` | POST | ✅ | ✅ |
| `/api/usuarios/{id}` | GET | ✅ | ✅ |
| `/api/usuarios/{id}` | PUT | ✅ | ✅ |
| `/api/usuarios/{id}/senha` | PUT | ✅ | ✅ |
| `/api/usuarios` | GET | ✅ | ✅ |

### Endpoints Não Implementados (específicos do monolito)

| Endpoint | Método | Motivo |
|----------|--------|--------|
| `/api/usuarios/me` | GET | Requer autenticação JWT |
| `/api/usuarios/me` | PUT | Requer autenticação JWT |
| `/api/usuarios/{id}/perfil` | PATCH | Requer autorização Admin |
| `/api/usuarios/{id}` | DELETE | Requer autorização Admin |

## 🎯 Decisões de Design

### 1. Sem AggregateRoot Base Class
- **Monolito:** Usuario herda de AggregateRoot
- **Microsserviço:** Usuario é uma classe standalone
- **Motivo:** Simplificação. Eventos serão adicionados quando implementar message broker.

### 2. DbContext Inline Configuration
- **Monolito:** Usa classes de configuração separadas (IEntityTypeConfiguration)
- **Microsserviço:** Configuração inline no OnModelCreating
- **Motivo:** Apenas uma entidade por enquanto. Se adicionar mais, migrar para classes separadas.

### 3. Service Consolidation
- **Monolito:** Um UseCase por operação
- **Microsserviço:** Um Service consolidado
- **Motivo:** Menos arquivos, mais simples para começar. Pode ser separado se crescer.

### 4. InMemory Database Default
- **Monolito:** SQL Server direto
- **Microsserviço:** InMemory para dev, SQL Server para prod
- **Motivo:** Facilitar testes e desenvolvimento local sem dependências.

## 📈 Próximos Passos

1. ✅ **Fase 1 - Fundação** (Concluída)
   - Estrutura básica
   - Regras de negócio
   - Endpoints de cadastro

2. ⏳ **Fase 2 - Autenticação**
   - Implementar JWT
   - Login endpoint
   - Refresh tokens

3. ⏳ **Fase 3 - Comunicação**
   - Integrar message broker
   - Publicar eventos de domínio
   - Consumir eventos de outros serviços

4. ⏳ **Fase 4 - Observabilidade**
   - Logs estruturados
   - Telemetria
   - Health checks

5. ⏳ **Fase 5 - Testes**
   - Testes unitários
   - Testes de integração
   - Testes de contrato

## 💡 Conclusão

O microsserviço **fcg-users-api** mantém 100% da lógica de negócio do monolito para cadastro de usuários, mas com uma arquitetura simplificada e focada. As funcionalidades que dependem de outros contextos (biblioteca, email) serão implementadas via comunicação assíncrona entre microsserviços.
