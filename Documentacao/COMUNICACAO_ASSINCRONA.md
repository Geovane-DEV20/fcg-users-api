# 🔄 Comunicação Assíncrona entre Microsserviços

## 🎯 Princípio Fundamental

**TODOS os microsserviços se comunicam EXCLUSIVAMENTE via eventos assíncronos através do Message Broker.**

```
❌ NUNCA fazer chamadas HTTP entre microsserviços
❌ NUNCA acessar banco de dados de outro serviço
✅ SEMPRE publicar e consumir eventos
✅ SEMPRE manter dados locais via eventos
```

## 🏗️ Arquitetura Event-Driven

```
┌─────────────────────────────────────────────────────────────────┐
│                      Message Broker                             │
│                   (RabbitMQ / Azure Service Bus)                │
│                                                                 │
│   Topics/Exchanges:                                             │
│   ├─ users.events      (eventos de usuários)                   │
│   ├─ games.events      (eventos de jogos)                      │
│   ├─ library.events    (eventos de biblioteca)                 │
│   └─ orders.events     (eventos de pedidos)                    │
└────────┬────────────────────────┬────────────────┬─────────────┘
         │                        │                │
         │ Publish                │ Subscribe      │ Subscribe
         │                        │                │
┌────────▼─────────┐    ┌────────▼──────┐   ┌────▼──────────┐
│   Users API      │    │  Games API    │   │  Library API  │
│                  │    │               │   │               │
│ Publica:         │    │ Consome:      │   │ Consome:      │
│ • UsuarioCriado  │    │ • UsuarioCriado│   │ • UsuarioCriado│
│ • EmailConfirmado│    │ • EmailConfirm│   │ • EmailConfirm │
│ • UsuarioAtualiz │    │                │   │               │
│                  │    │ Publica:       │   │ Publica:      │
│                  │    │ • JogoComprado │   │ • ItemBaixado │
└──────────────────┘    └────────────────┘   └───────────────┘
```

## 📨 Eventos da Users API

### 1. UsuarioCriadoEvent

**Quando:** Usuário completa cadastro

```csharp
public record UsuarioCriadoEvent
{
    public Guid UsuarioId { get; init; }
    public string Nome { get; init; }
    public string Email { get; init; }
    public string Perfil { get; init; } // "Usuario" ou "Administrador"
    public DateTime Timestamp { get; init; }
}
```

**Publicação:**
```csharp
public class CadastroService
{
    private readonly IMessageBus _messageBus;
    
    public async Task<CadastrarUsuarioResponse> CadastrarUsuarioAsync(
        CadastrarUsuarioRequest request, 
        CancellationToken cancellationToken = default)
    {
        var email = new Email(request.Email);
        
        if (await _usuarioRepository.ExisteComEmailAsync(email, cancellationToken))
            throw new RecursoDuplicadoException(...);

        var usuario = Usuario.CadastrarConta(request.Nome, email, request.Senha, DateTime.UtcNow);
        
        await _usuarioRepository.AdicionarAsync(usuario, cancellationToken);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        // Publicar evento (fire-and-forget, não espera resposta)
        await _messageBus.PublishAsync(new UsuarioCriadoEvent
        {
            UsuarioId = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email.Valor,
            Perfil = usuario.Perfil.ToString(),
            Timestamp = DateTime.UtcNow
        }, topic: "users.events");

        _logger.LogInformation("Usuário {UsuarioId} cadastrado e evento publicado", usuario.Id);

        // Retorna imediatamente, não espera outros serviços
        return new CadastrarUsuarioResponse(usuario.Id);
    }
}
```

**Consumidores:**

```csharp
// Games API - Cria cópia local do usuário
public class UsuarioCriadoEventHandler : IEventHandler<UsuarioCriadoEvent>
{
    private readonly GamesDbContext _context;
    private readonly ILogger<UsuarioCriadoEventHandler> _logger;

    public async Task HandleAsync(UsuarioCriadoEvent evento)
    {
        _logger.LogInformation("Processando UsuarioCriadoEvent para {UsuarioId}", evento.UsuarioId);

        var userSummary = new UserSummary
        {
            Id = evento.UsuarioId,
            Nome = evento.Nome,
            Email = evento.Email,
            SincronizadoEm = DateTime.UtcNow
        };

        await _context.UserSummaries.AddAsync(userSummary);
        await _context.SaveChangesAsync();

        _logger.LogInformation("UserSummary criado para {UsuarioId}", evento.UsuarioId);
    }
}

// Library API - Provisiona biblioteca inicial
public class UsuarioCriadoEventHandler : IEventHandler<UsuarioCriadoEvent>
{
    private readonly LibraryDbContext _context;
    private readonly ILogger<UsuarioCriadoEventHandler> _logger;

    public async Task HandleAsync(UsuarioCriadoEvent evento)
    {
        _logger.LogInformation("Criando biblioteca para usuário {UsuarioId}", evento.UsuarioId);

        // Criar biblioteca vazia
        var biblioteca = new Biblioteca
        {
            Id = Guid.NewGuid(),
            UsuarioId = evento.UsuarioId,
            CriadaEm = DateTime.UtcNow
        };

        await _context.Bibliotecas.AddAsync(biblioteca);

        // Também manter cópia do usuário
        var userSummary = new UserSummary
        {
            Id = evento.UsuarioId,
            Nome = evento.Nome
        };

        await _context.UserSummaries.AddAsync(userSummary);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Biblioteca criada para {UsuarioId}", evento.UsuarioId);
    }
}
```

### 2. EmailConfirmadoEvent

**Quando:** Usuário confirma email via token

```csharp
public record EmailConfirmadoEvent
{
    public Guid UsuarioId { get; init; }
    public string Email { get; init; }
    public DateTime ConfirmadoEm { get; init; }
}
```

**Publicação:**
```csharp
public async Task ConfirmarEmailAsync(ConfirmarEmailRequest request, CancellationToken cancellationToken = default)
{
    var usuario = await _usuarioRepository.ObterPorTokenDeAtivacaoAsync(request.Token, cancellationToken)
        ?? throw new DomainException("TOKEN_ATIVACAO_INVALIDO", "...");

    usuario.ConfirmarEmail(request.Token, DateTime.UtcNow);
    await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

    // Publicar evento
    await _messageBus.PublishAsync(new EmailConfirmadoEvent
    {
        UsuarioId = usuario.Id,
        Email = usuario.Email.Valor,
        ConfirmadoEm = DateTime.UtcNow
    }, topic: "users.events");

    _logger.LogInformation("Email confirmado para {UsuarioId}", usuario.Id);
}
```

**Consumidores:**
- **Games API**: Ativa acesso ao marketplace
- **Library API**: Libera downloads
- **Email Service**: Envia email de boas-vindas

### 3. UsuarioAtualizadoEvent

**Quando:** Dados do usuário são alterados

```csharp
public record UsuarioAtualizadoEvent
{
    public Guid UsuarioId { get; init; }
    public string NovoNome { get; init; }
    public DateTime AtualizadoEm { get; init; }
}
```

**Consumidores:** Atualizam cópia local (UserSummary)

### 4. SenhaAlteradaEvent

**Quando:** Usuário troca a senha

```csharp
public record SenhaAlteradaEvent
{
    public Guid UsuarioId { get; init; }
    public string Email { get; init; }
    public DateTime AlteradaEm { get; init; }
    public string OrigemIP { get; init; }
}
```

**Consumidores:**
- **Security Service**: Log de auditoria
- **Email Service**: Notifica usuário sobre alteração
- **Monitoring**: Detecta atividade suspeita

### 5. UsuarioDesativadoEvent

**Quando:** Conta é desativada

```csharp
public record UsuarioDesativadoEvent
{
    public Guid UsuarioId { get; init; }
    public string Motivo { get; init; }
    public DateTime DesativadoEm { get; init; }
}
```

**Consumidores:**
- **Games API**: Remove acesso ao marketplace
- **Library API**: Bloqueia downloads
- **Orders API**: Cancela pedidos pendentes

### 6. ContaBloqueadaEvent

**Quando:** Conta bloqueada por tentativas falhadas

```csharp
public record ContaBloqueadaEvent
{
    public Guid UsuarioId { get; init; }
    public string Email { get; init; }
    public DateTime BloqueadaAte { get; init; }
    public int TentativasFalhas { get; init; }
    public string OrigemIP { get; init; }
}
```

**Consumidores:**
- **Security Service**: Log de segurança
- **Email Service**: Notifica usuário
- **Monitoring**: Alerta para possível ataque

## 🔄 Fluxos Completos

### Fluxo 1: Cadastro de Usuário

```
┌─────────┐          ┌───────────┐          ┌──────────┐          ┌──────────┐
│ Cliente │          │Users API  │          │  Broker  │          │Games API │
└────┬────┘          └─────┬─────┘          └────┬─────┘          └────┬─────┘
     │                     │                     │                     │
     │ POST /usuarios      │                     │                     │
     ├────────────────────>│                     │                     │
     │                     │                     │                     │
     │                     │ Salva no DB         │                     │
     │                     │ (FcgUsersDb)        │                     │
     │                     │                     │                     │
     │                     │ Publish Event       │                     │
     │                     ├────────────────────>│                     │
     │                     │                     │                     │
     │ 201 Created         │                     │ UsuarioCriadoEvent  │
     │<────────────────────┤                     ├────────────────────>│
     │ {id: "..."}         │                     │                     │
     │                     │                     │                     │
     │                     │                     │                     │ Cria UserSummary
     │                     │                     │                     │ (FcgGamesDb)
     │                     │                     │                     │
     │                     │                     │                     │ ACK
     │                     │                     │<────────────────────┤
```

**Importante:** O cliente recebe resposta **antes** dos outros serviços processarem o evento!

### Fluxo 2: Compra de Jogo (Saga Assíncrona)

```
┌─────────┐     ┌──────────┐     ┌─────────┐     ┌──────────┐     ┌──────────┐
│ Cliente │     │Games API │     │ Broker  │     │Users API │     │Library API│
└────┬────┘     └────┬─────┘     └────┬────┘     └────┬─────┘     └────┬─────┘
     │               │                │               │                │
     │ POST /compra  │                │               │                │
     ├──────────────>│                │               │                │
     │               │                │               │                │
     │               │ Valida localmente              │                │
     │               │ (UserSummary)  │               │                │
     │               │                │               │                │
     │               │ Cria Compra    │               │                │
     │               │ (FcgGamesDb)   │               │                │
     │               │                │               │                │
     │               │ JogoCompradoEvent              │                │
     │               ├───────────────>│               │                │
     │               │                │               │                │
     │ 200 OK        │                │               │                │
     │<──────────────┤                │               │                │
     │               │                │               │                │
     │               │                │ JogoCompradoEvent              │
     │               │                ├───────────────┼───────────────>│
     │               │                │               │                │
     │               │                │               │                │ Adiciona à Biblioteca
     │               │                │               │                │ (FcgLibraryDb)
     │               │                │               │                │
     │               │                │ BibliotecaAtualizadaEvent      │
     │               │                │<───────────────────────────────┤
```

## ⚡ Vantagens da Comunicação 100% Assíncrona

### ✅ 1. Desacoplamento Total

```
Users API não sabe:
├─ Quem consome seus eventos
├─ Quantos consumidores existem
├─ Se consumidores estão online
└─ Como consumidores processam eventos

Games API pode ser:
├─ Adicionado sem modificar Users API
├─ Removido sem afetar Users API
├─ Substituído por outra tecnologia
└─ Escalado independentemente
```

### ✅ 2. Resiliência Extrema

```
Cenário: Games API está offline

1. Cliente cadastra usuário na Users API ✅
2. Users API publica UsuarioCriadoEvent ✅
3. Evento fica na fila aguardando ⏳
4. Games API volta online 🔄
5. Games API processa eventos pendentes ✅
6. Sistema volta ao estado consistente ✅

Resultado: Users API NUNCA foi afetada!
```

### ✅ 3. Performance Otimizada

```
Sem eventos (síncrono):
├─ Client -> Users API (50ms)
├─ Users API -> Games API (30ms) 
├─ Users API -> Library API (30ms)
├─ Users API -> Orders API (30ms)
└─ Total: 140ms + risco de timeout

Com eventos (assíncrono):
├─ Client -> Users API (50ms)
├─ Users API -> Broker (5ms)
└─ Total: 55ms + processamento em background

Ganho: 60% mais rápido! 🚀
```

### ✅ 4. Escalabilidade Ilimitada

```
Message Broker distribui carga:

UsuarioCriadoEvent
      │
      ├──────> Games API (Instância 1)
      ├──────> Games API (Instância 2)
      ├──────> Games API (Instância 3)
      │
      ├──────> Library API (Instância 1)
      ├──────> Library API (Instância 2)
      │
      └──────> Orders API (Instância 1)
```

## ⚠️ Desafios e Soluções

### Desafio 1: Eventual Consistency

**Problema:** Dados podem estar temporariamente inconsistentes

**Solução:** Aceitar inconsistência temporária de milissegundos

```
t0: Usuario criado em FcgUsersDb ✅
t1: Evento na fila ⏳
t2: UserSummary criado em FcgGamesDb ✅ (100ms depois)

Janela de inconsistência: 100ms (aceitável!)
```

### Desafio 2: Ordem de Eventos

**Problema:** Eventos podem chegar fora de ordem

**Solução:** Incluir timestamp e versão nos eventos

```csharp
public record BaseEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public int Version { get; init; } = 1;
}

// Consumidor verifica versão
public async Task HandleAsync(UsuarioAtualizadoEvent evento)
{
    var atual = await _context.UserSummaries.FindAsync(evento.UsuarioId);
    
    // Ignorar evento antigo
    if (atual?.Versao >= evento.Version)
        return;
    
    // Processar evento mais recente
    atual.Nome = evento.NovoNome;
    atual.Versao = evento.Version;
    await _context.SaveChangesAsync();
}
```

### Desafio 3: Idempotência

**Problema:** Evento pode ser processado múltiplas vezes

**Solução:** Guardar ID do evento processado

```csharp
public async Task HandleAsync(UsuarioCriadoEvent evento)
{
    // Verificar se já processou este evento
    var jaProcessado = await _context.EventosProcessados
        .AnyAsync(e => e.EventId == evento.EventId);
    
    if (jaProcessado)
    {
        _logger.LogWarning("Evento {EventId} já foi processado", evento.EventId);
        return;
    }
    
    // Processar evento
    var userSummary = new UserSummary { ... };
    await _context.UserSummaries.AddAsync(userSummary);
    
    // Marcar como processado
    await _context.EventosProcessados.AddAsync(new EventoProcessado
    {
        EventId = evento.EventId,
        ProcessadoEm = DateTime.UtcNow
    });
    
    await _context.SaveChangesAsync();
}
```

## 🚀 Implementação no Users API

### Interface IMessageBus

```csharp
public interface IMessageBus
{
    Task PublishAsync<T>(T evento, string topic, CancellationToken cancellationToken = default) 
        where T : class;
}
```

### Implementação RabbitMQ

```csharp
public class RabbitMqMessageBus : IMessageBus
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqMessageBus> _logger;

    public async Task PublishAsync<T>(T evento, string topic, CancellationToken cancellationToken = default) 
        where T : class
    {
        using var channel = _connection.CreateModel();
        
        channel.ExchangeDeclare(
            exchange: topic,
            type: ExchangeType.Fanout,
            durable: true);

        var json = JsonSerializer.Serialize(evento);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: topic,
            routingKey: string.Empty,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Evento {EventType} publicado no tópico {Topic}", typeof(T).Name, topic);
    }
}
```

## 📊 Monitoramento de Eventos

### Métricas Importantes

```csharp
// Número de eventos publicados
_metrics.Counter("events_published_total", 1, new KeyValuePair<string, object>("event_type", typeof(T).Name));

// Tamanho da fila
_metrics.Gauge("queue_depth", queueDepth, new KeyValuePair<string, object>("queue", "users.events"));

// Latência de processamento
_metrics.Histogram("event_processing_duration_ms", duration, new KeyValuePair<string, object>("event_type", typeof(T).Name));

// Eventos com erro
_metrics.Counter("events_failed_total", 1, new KeyValuePair<string, object>("event_type", typeof(T).Name));
```

## 💡 Conclusão

A comunicação **100% assíncrona via eventos** garante:

✅ **Desacoplamento** - Serviços completamente independentes  
✅ **Resiliência** - Falhas isoladas, sem efeito cascata  
✅ **Performance** - Resposta imediata ao cliente  
✅ **Escalabilidade** - Cada serviço escala independentemente  
✅ **Flexibilidade** - Novos serviços podem ser adicionados sem modificar código  

**Tradeoff:** Aceitar **eventual consistency** (dados temporariamente inconsistentes por milissegundos)
