# 🗄️ Estratégia de Banco de Dados - FCG Users API

## 📊 Database per Service Pattern

Esta API segue o padrão **Database per Service**, onde **cada microsserviço possui seu próprio banco de dados dedicado**.

## 🎯 Banco de Dados Principal: FcgUsersDb

### Visão Geral

```
┌─────────────────────────────────────────────────────────┐
│                    FCG Users API                        │
│                  (Microsserviço)                        │
└─────────────────────┬───────────────────────────────────┘
                      │ Acesso Exclusivo
                      │
                      ▼
         ┌────────────────────────────┐
         │    FcgUsersDb (SQL Server)  │
         │                            │
         │  ┌──────────────────────┐  │
         │  │   Tabela: Usuarios   │  │
         │  └──────────────────────┘  │
         └────────────────────────────┘
```

### Características

✅ **Banco exclusivo** - Apenas a Users API tem acesso
✅ **Isolamento total** - Outros microsserviços NÃO podem acessar diretamente
✅ **Schema proprietário** - Estrutura gerenciada apenas por este serviço
✅ **Migrations independentes** - Evolução autônoma do schema

## 📐 Estrutura do Banco

### Tabela: Usuarios

```sql
CREATE TABLE Usuarios (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Nome NVARCHAR(200) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    SenhaHash NVARCHAR(500) NOT NULL,
    Perfil INT NOT NULL,
    Status INT NOT NULL,
    TokenAtivacao NVARCHAR(100) NULL,
    TokenAtivacaoExpiraEm DATETIME2 NULL,
    TentativasFalhasConsecutivas INT NOT NULL DEFAULT 0,
    BloqueadaAte DATETIME2 NULL,
    CriadoEm DATETIME2 NOT NULL,
    
    CONSTRAINT UK_Usuarios_Email UNIQUE (Email)
);

CREATE INDEX IX_Usuarios_TokenAtivacao 
    ON Usuarios(TokenAtivacao) 
    WHERE TokenAtivacao IS NOT NULL;

CREATE INDEX IX_Usuarios_Status 
    ON Usuarios(Status);
```

### Índices Estratégicos

| Índice | Tipo | Campo(s) | Propósito |
|--------|------|----------|-----------|
| PK_Usuarios | Clustered | Id | Busca por ID |
| UK_Usuarios_Email | Unique | Email | Email único + busca rápida |
| IX_TokenAtivacao | Filtered | TokenAtivacao | Confirmação de email |
| IX_Status | Non-clustered | Status | Listagem por status |

## 🔐 Isolamento de Dados

### Princípio: Nenhum acesso direto entre microsserviços

```
❌ ERRADO - Acesso Direto ao Banco
┌──────────────────┐
│   Games API      │──────┐
└──────────────────┘      │
                          ▼
┌──────────────────┐   ┌──────────────┐
│  Library API     │──>│  FcgUsersDb  │
└──────────────────┘   └──────────────┘
                          ▲
┌──────────────────┐      │
│  Orders API      │──────┘
└──────────────────┘


✅ **Correto - Comunicação 100% Assíncrona**

```
┌──────────────────┐                    ┌──────────────────┐       ┌──────────────┐
│   Games API      │                    │   Users API      │──────>│  FcgUsersDb  │
└──────────────────┘                    └──────────────────┘       └──────────────┘
         │                                       │
         │ Consome Eventos                      │ Publica Eventos
         │                                       │
         └─────────────> Message Broker <───────┘
                    (RabbitMQ/Azure Service Bus)
                              │
┌──────────────────┐          │
│  Library API     │──────────┘
└──────────────────┘  Consome Eventos
```

**Princípio:** Nenhum microsserviço chama outro diretamente via HTTP. Toda comunicação acontece através de eventos publicados no message broker.

#### Eventos Publicados pela Users API

```csharp
// Users API publica evento quando usuário é criado
public class CadastroService
{
    private readonly IMessageBus _messageBus;
    
    public async Task<CadastrarUsuarioResponse> CadastrarUsuarioAsync(...)
    {
        var usuario = Usuario.CadastrarConta(...);
        await _usuarioRepository.SalvarAlteracoesAsync();
        
        // Publicar evento (fire-and-forget)
        await _messageBus.PublishAsync(new UsuarioCriadoEvent
        {
            UsuarioId = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email.Valor,
            Perfil = usuario.Perfil,
            Timestamp = DateTime.UtcNow
        });
        
        return new CadastrarUsuarioResponse(usuario.Id);
    }
}
```

#### Outros Serviços Consomem Eventos

```csharp
// Games API consome evento e mantém cópia local
public class UsuarioCriadoEventHandler : IEventHandler<UsuarioCriadoEvent>
{
    private readonly GamesDbContext _context;
    
    public async Task HandleAsync(UsuarioCriadoEvent evt)
    {
        // Manter cópia apenas dos dados necessários
        var userSummary = new UserSummary
        {
            Id = evt.UsuarioId,
            Nome = evt.Nome,
            Email = evt.Email
        };
        
        await _context.UserSummaries.AddAsync(userSummary);
        await _context.SaveChangesAsync();
    }
}

// Library API consome o mesmo evento
public class UsuarioCriadoEventHandler : IEventHandler<UsuarioCriadoEvent>
{
    private readonly LibraryDbContext _context;
    
    public async Task HandleAsync(UsuarioCriadoEvent evt)
    {
        // Criar biblioteca vazia para o novo usuário
        var biblioteca = new Biblioteca
        {
            Id = Guid.NewGuid(),
            UsuarioId = evt.UsuarioId,
            CriadaEm = DateTime.UtcNow
        };
        
        await _context.Bibliotecas.AddAsync(biblioteca);
        
        // Também manter cópia do usuário
        var userSummary = new UserSummary
        {
            Id = evt.UsuarioId,
            Nome = evt.Nome
        };
        
        await _context.UserSummaries.AddAsync(userSummary);
        await _context.SaveChangesAsync();
    }
}
```

### ✅ Vantagens da Comunicação Assíncrona

1. **Desacoplamento Total**
   - Serviços não conhecem uns aos outros
   - Users API não sabe quem consome seus eventos
   - Novos serviços podem ser adicionados sem modificar código existente

2. **Resiliência**
   - Se Games API estiver offline, eventos ficam na fila
   - Quando voltar, processa eventos pendentes
   - Users API não é afetado por falhas em outros serviços

3. **Escalabilidade**
   - Message broker distribui carga entre instâncias
   - Cada serviço escala independentemente
   - Fila absorve picos de tráfego

4. **Performance**
   - Users API não espera resposta de outros serviços
   - Latência baixa para o cliente
   - Processamento paralelo de eventos

```csharp
// Users API publica evento quando usuário é criado
public class CadastroService
{
    private readonly IMessageBus _messageBus;
    
    public async Task<CadastrarUsuarioResponse> CadastrarUsuarioAsync(...)
    {
        var usuario = Usuario.CadastrarConta(...);
        await _usuarioRepository.SalvarAlteracoesAsync();
        
        // Publicar evento
        await _messageBus.PublishAsync(new UsuarioCriadoEvent
        {
            UsuarioId = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email.Valor,
            Perfil = usuario.Perfil
        });
        
        return new CadastrarUsuarioResponse(usuario.Id);
    }
}

// Games API consome evento e mantém cópia local
public class UsuarioCriadoEventHandler : IEventHandler<UsuarioCriadoEvent>
{
    private readonly GamesDbContext _context;
    
    public async Task HandleAsync(UsuarioCriadoEvent evt)
    {
        // Manter cópia apenas dos dados necessários
        var userSummary = new UserSummary
        {
            Id = evt.UsuarioId,
            Nome = evt.Nome,
            Email = evt.Email
        };
        
        await _context.UserSummaries.AddAsync(userSummary);
        await _context.SaveChangesAsync();
    }
}
```

## 🏗️ Arquitetura Multi-Database

### Visão Geral do Ecossistema

```
┌─────────────────────────────────────────────────────────────────┐
│                      Message Broker                             │
│                   (RabbitMQ / Azure Service Bus)                │
└────────┬─────────────────┬─────────────────┬────────────────────┘
         │                 │                 │
         │                 │                 │
┌────────▼─────────┐ ┌────▼──────────┐ ┌───▼───────────────┐
│   Users API      │ │  Games API    │ │  Library API      │
│                  │ │               │ │                   │
│ ┌──────────────┐ │ │ ┌───────────┐│ │ ┌───────────────┐│
│ │ FcgUsersDb   │ │ │ │FcgGamesDb ││ │ │FcgLibraryDb   ││
│ │              │ │ │ │           ││ │ │               ││
│ │• Usuarios    │ │ │ │• Games    ││ │ │• Bibliotecas  ││
│ │              │ │ │ │• Scores   ││ │ │• Items        ││
│ └──────────────┘ │ │ └───────────┘│ │ └───────────────┘│
└──────────────────┘ └───────────────┘ └───────────────────┘
```

### Cada Banco Tem:

| Microsserviço | Banco de Dados | Tabelas Principais |
|---------------|----------------|-------------------|
| **Users API** | `FcgUsersDb` | Usuarios |
| **Games API** | `FcgGamesDb` | Games, Scores, UserSummaries* |
| **Library API** | `FcgLibraryDb` | Bibliotecas, Items, UserSummaries* |
| **Orders API** | `FcgOrdersDb` | Orders, OrderItems, UserSummaries* |

*UserSummaries = Cópia desnormalizada de dados básicos do usuário

## 📦 Estratégias de Sincronização

### Event-Driven Architecture (Comunicação 100% Assíncrona)

**Todos os microsserviços se comunicam exclusivamente via eventos publicados no Message Broker.**

```
Users API                    Message Broker              Games API
─────────                    ──────────────              ─────────

[Usuário Criado]
      │
      │ Publish (async)
      ├──────────────────> [UsuarioCriadoEvent] ────────> [Salva UserSummary]
      │                         │
      │ Retorna imediatamente    │
      │                         │
[Email Confirmado]              └─────────────────────────> [Library API]
      │                                                     [Cria Biblioteca]
      │ Publish (async)
      ├──────────────────> [EmailConfirmadoEvent] ──────> [Atualiza UserSummary]
      │
      │
[Perfil Alterado]
      │
      │ Publish (async)
      └──────────────────> [PerfilAlteradoEvent] ────────> [Atualiza Permissões]
```

### Eventos da Users API

| Evento | Quando | Consumidores | Ação |
|--------|--------|--------------|------|
| `UsuarioCriadoEvent` | Cadastro | Games, Library, Orders | Criar UserSummary, provisionar recursos |
| `EmailConfirmadoEvent` | Ativação | Games, Library | Atualizar status, liberar acesso |
| `UsuarioAtualizadoEvent` | Update perfil | Games, Library, Orders | Atualizar cópia local |
| `SenhaAlteradaEvent` | Troca senha | Audit, Security | Log de segurança |
| `UsuarioDesativadoEvent` | Desativação | Games, Library, Orders | Revogar acessos, limpar dados |
| `TentativaFalhaEvent` | Login falho | Security, Monitoring | Alertas, bloqueio |
| `ContaBloqueadaEvent` | 5 tentativas | Security, Monitoring | Notificação, log |

### Eventual Consistency

Com comunicação assíncrona, **dados podem ficar temporariamente inconsistentes** (eventual consistency):

```
Timeline:

t0: Users API cria usuário
    └─> FcgUsersDb: Usuario existe ✅

t1: Evento publicado no broker
    └─> Fila: UsuarioCriadoEvent aguardando ⏳

t2: Games API consome evento
    └─> FcgGamesDb: UserSummary criado ✅

t3: Library API consome evento
    └─> FcgLibraryDb: Biblioteca + UserSummary criados ✅

Entre t0 e t3: Dados podem estar inconsistentes entre serviços
Após t3: Consistência eventual alcançada ✅
```

**Isso é aceitável porque:**
- Janela de inconsistência é de milissegundos
- Cada serviço tem os dados que precisa localmente
- Usuários não percebem a diferença
- Resiliência é mais importante que consistência imediata

```
Exemplo: Desativar Conta de Usuário

Users API:
1. Desativa usuário no FcgUsersDb
2. Publica: UsuarioDesativadoEvent

Games API (Saga):
3. Desativa games do usuário
4. Publica: GamesDesativadosSucessoEvent

Library API (Saga):
5. Remove acesso à biblioteca
6. Publica: BibliotecaRemovidaSucessoEvent

Orders API (Saga):
7. Cancela pedidos pendentes
8. Publica: PedidosCanceladosSucessoEvent

Compensação (se algo falhar):
- Rollback em cada serviço
- Reativa usuário se necessário
```

## 🔧 Configuração por Ambiente

### Development (InMemory)
```json
// appsettings.Development.json
{
  "DatabaseProvider": "InMemory",
  "ConnectionStrings": {
    "DefaultConnection": "InMemory-FcgUsersDb"
  }
}
```

```csharp
// Program.cs
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("FcgUsersDb"));
}
```

### Production (SQL Server)
```json
// appsettings.Production.json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=fcg-users-db.database.windows.net;Database=FcgUsersDb;User Id=fcgadmin;Password=***;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

```csharp
// Program.cs
if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));
}
```

## 🎯 Vantagens do Database per Service

### ✅ Benefícios

1. **Isolamento de Falhas**
   - Se o banco do Games API cair, Users API continua funcionando

2. **Escalabilidade Independente**
   - Users API pode ter banco menor e mais rápido
   - Games API pode ter banco maior com mais storage

3. **Tecnologia Adequada**
   - Users API: SQL Server (dados relacionais)
   - Games API: PostgreSQL (features específicas)
   - Library API: MongoDB (documentos)

4. **Deployment Independente**
   - Migrations não afetam outros serviços
   - Schema pode evoluir de forma autônoma

5. **Segurança**
   - Credenciais diferentes por serviço
   - Acesso segregado por rede/firewall

### ⚠️ Desafios

1. **Joins entre serviços**
   - ❌ Não pode fazer JOIN direto
   - ✅ Usar agregação no código ou cópia desnormalizada

2. **Transações distribuídas**
   - ❌ Não pode usar transação ACID tradicional
   - ✅ Usar Saga Pattern ou eventual consistency

3. **Duplicação de dados**
   - ❌ UserSummaries replicado em vários bancos
   - ✅ Sincronização via eventos

4. **Complexidade operacional**
   - ❌ Múltiplos backups, múltiplas migrations
   - ✅ Automação e IaC (Infrastructure as Code)

## 🚀 Roadmap de Implementação

### Fase 1: Setup Inicial (Atual) ✅
- [x] Database InMemory para desenvolvimento
- [x] Entity Framework configurado
- [x] Migrations preparadas
- [x] SQL Server pronto para produção

### Fase 2: Message Broker (Próxima)
- [ ] Configurar RabbitMQ/Azure Service Bus
- [ ] Implementar eventos de domínio
- [ ] Publicar UsuarioCriadoEvent
- [ ] Publicar EmailConfirmadoEvent
- [ ] Publicar UsuarioDesativadoEvent

### Fase 3: Sincronização
- [ ] Outros microsserviços consumem eventos
- [ ] Implementar UserSummaries nos outros DBs
- [ ] Garantir eventual consistency

### Fase 4: Produção
- [ ] Deploy em SQL Server dedicado
- [ ] Configurar backup automatizado
- [ ] Implementar monitoring
- [ ] Setup de disaster recovery

## 📊 Estimativa de Recursos

### Ambiente de Produção

| Componente | Especificação | Custo Estimado (Azure) |
|------------|---------------|------------------------|
| **Azure SQL Database** | Basic (2GB) | ~$5/mês |
| **Azure SQL Database** | Standard S2 (50GB) | ~$150/mês |
| **Backup Automático** | Incluído | Grátis |
| **Connection Pool** | Incluído | Grátis |

### Escala Esperada

| Métrica | Desenvolvimento | Produção Inicial | Produção Escala |
|---------|-----------------|------------------|-----------------|
| Usuários | 10-50 | 10.000 | 100.000+ |
| Tamanho DB | <1 MB | ~500 MB | ~5 GB |
| Queries/seg | <10 | ~100 | ~1.000 |
| DTU/Compute | N/A | 50 | 200+ |

## 🔍 Monitoramento do Banco

### Métricas Importantes

```csharp
// Health Check do banco
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    
    public async Task<HealthCheckResult> CheckHealthAsync(...)
    {
        try
        {
            await _context.Database.CanConnectAsync();
            var userCount = await _context.Usuarios.CountAsync();
            
            return HealthCheckResult.Healthy(
                $"Database is healthy. Total users: {userCount}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection failed", ex);
        }
    }
}
```

### Logs Importantes

- Tempo de resposta de queries
- Conexões ativas no pool
- Deadlocks
- Slow queries (>1 segundo)
- Failed connections

## 💡 Conclusão

A **Users API possui seu próprio banco de dados dedicado (FcgUsersDb)**, seguindo as melhores práticas de arquitetura de microsserviços. Outros serviços acessam dados de usuários apenas através da API HTTP ou consumindo eventos, nunca acessando o banco diretamente.

Esta abordagem garante:
- ✅ Autonomia do serviço
- ✅ Isolamento de falhas
- ✅ Escalabilidade independente
- ✅ Evolução autônoma do schema
- ✅ Segurança aprimorada
