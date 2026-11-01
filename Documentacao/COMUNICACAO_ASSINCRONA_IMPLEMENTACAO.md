# Implementação de Comunicação Assíncrona com RabbitMQ

## 📋 Visão Geral

Este documento detalha a implementação completa da **comunicação assíncrona** na API FCG Users usando **RabbitMQ** e **eventos de domínio**.

## 🎯 Objetivo

Implementar o padrão de comunicação 100% assíncrona entre microsserviços usando eventos de domínio publicados via RabbitMQ.

---

## 📦 Arquitetura de Eventos

### Eventos de Domínio Implementados

| Evento | Routing Key | Quando é Disparado | Payload |
|--------|-------------|-------------------|---------|
| `UsuarioCriadoEvent` | `users.created` | Novo usuário se cadastra | `UsuarioId`, `Nome`, `Email`, `Perfil`, `CriadoEm` |
| `ContaAtivadaEvent` | `users.activated` | Usuário confirma email | `UsuarioId`, `Email`, `AtivadaEm` |
| `SenhaAlteradaEvent` | `users.password.changed` | Usuário altera senha | `UsuarioId`, `Email`, `AlteradaEm` |
| `PerfilAlteradoEvent` | `users.profile.changed` | Admin altera perfil | `UsuarioId`, `Email`, `PerfilAntigo`, `PerfilNovo`, `AlteradoPor` |

### Estrutura de um Evento

Todos os eventos implementam a interface `IDomainEvent`:

```csharp
public interface IDomainEvent
{
    Guid EventId { get; }        // ID único do evento
    DateTime OcorridoEm { get; }  // Timestamp UTC
    string EventType { get; }     // Nome do tipo de evento
}
```

---

## 🏗️ Componentes Implementados

### 1. **Interface IEventPublisher**
```csharp
public interface IEventPublisher
{
    Task PublicarAsync<TEvent>(TEvent evento, CancellationToken cancellationToken = default) 
        where TEvent : IDomainEvent;
    
    Task PublicarBatchAsync(IEnumerable<IDomainEvent> eventos, CancellationToken cancellationToken = default);
}
```

### 2. **RabbitMQEventPublisher** (Implementação)

**Características:**
- ✅ Conexão persistente com auto-recovery
- ✅ Mensagens persistentes (durable)
- ✅ Exchange tipo `topic` para roteamento flexível
- ✅ Serialização JSON com camelCase
- ✅ Tratamento de erros com logging
- ✅ Suporte a batch publishing

**Configuração:**
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "fcg.users.events"
  }
}
```

### 3. **RabbitMQHealthCheck**

Health check para monitorar a conexão com o RabbitMQ:

```
GET /health
```

**Resposta:**
```json
{
  "status": "Healthy",
  "results": {
    "rabbitmq": {
      "status": "Healthy",
      "description": "RabbitMQ está acessível e funcionando"
    }
  }
}
```

---

## 🔄 Fluxo de Publicação de Eventos

### Exemplo: Cadastro de Usuário

```
1. Controller recebe POST /api/usuarios
   ↓
2. CadastroService.CadastrarUsuarioAsync()
   ↓
3. Usuario.CadastrarConta() - cria agregado
   ↓
4. Repository.AdicionarAsync() - persiste no banco
   ↓
5. Repository.SalvarAlteracoesAsync() - commit
   ↓
6. EventPublisher.PublicarAsync(UsuarioCriadoEvent)
   ↓
7. RabbitMQ recebe mensagem com routing key "users.created"
   ↓
8. Outros microsserviços recebem o evento via consumer
```

### Eventual Consistency

⚠️ **Importante:** A publicação de eventos **não falha a operação** se ocorrer erro:

```csharp
try
{
    var evento = new UsuarioCriadoEvent(...);
    await _eventPublisher.PublicarAsync(evento, cancellationToken);
    _logger.LogInformation("Evento publicado com sucesso");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Erro ao publicar evento");
    // Operação continua mesmo com falha na publicação
}
```

**Benefícios:**
- ✅ Alta disponibilidade - API não fica travada se RabbitMQ cair
- ✅ Eventual consistency - eventos podem ser republicados depois
- ✅ Resiliência - falhas temporárias não impactam usuário

---

## 📊 Routing Keys

### Padrão de Nomenclatura

```
{recurso}.{acao}[.{detalhe}]
```

**Exemplos:**
- `users.created` - Usuário criado
- `users.activated` - Conta ativada
- `users.profile.changed` - Perfil alterado
- `users.password.changed` - Senha alterada

### Vantagens do Exchange Topic

✅ **Roteamento Flexível:**
```
# Consumer 1: Recebe TODOS os eventos de usuários
users.*

# Consumer 2: Recebe apenas alterações (perfil ou senha)
users.*.changed

# Consumer 3: Recebe apenas eventos de criação e ativação
users.created, users.activated
```

---

## 🚀 Como Consumir os Eventos (Outros Microsserviços)

### Exemplo: Microsserviço de Notificações

```csharp
public class UsuarioCriadoConsumer : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        // Declara fila
        channel.QueueDeclare(
            queue: "notifications.users.created",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        // Bind com routing key
        channel.QueueBind(
            queue: "notifications.users.created",
            exchange: "fcg.users.events",
            routingKey: "users.created"
        );

        // Consumer
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var evento = JsonSerializer.Deserialize<UsuarioCriadoEvent>(json);

            // Envia email de ativação
            await _emailService.EnviarEmailAtivacao(
                evento.Email,
                evento.Nome,
                evento.TokenAtivacao
            );

            channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume(
            queue: "notifications.users.created",
            autoAck: false,
            consumer: consumer
        );

        return Task.CompletedTask;
    }
}
```

---

## 🐳 Docker Compose - RabbitMQ

Para facilitar o desenvolvimento local:

```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3.12-management-alpine
    container_name: fcg-rabbitmq
    ports:
      - "5672:5672"    # AMQP
      - "15672:15672"  # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    networks:
      - fcg-network

volumes:
  rabbitmq_data:

networks:
  fcg-network:
    driver: bridge
```

**Executar:**
```bash
docker-compose up -d
```

**Acessar Management UI:**
```
http://localhost:15672
Usuário: guest
Senha: guest
```

---

## 🔍 Monitoramento e Observabilidade

### Logs Estruturados

Todos os eventos publicados geram logs:

```
info: fcg_users_api.Events.RabbitMQ.RabbitMQEventPublisher[0]
      Evento publicado: UsuarioCriado | ID: 3fa85f64-5717-4562-b3fc-2c963f66afa6 | RoutingKey: users.created
```

### Métricas Recomendadas

| Métrica | Descrição | Alerta |
|---------|-----------|--------|
| `events_published_total` | Total de eventos publicados | < 1/min (pode indicar problema) |
| `events_failed_total` | Total de falhas na publicação | > 5/min |
| `rabbitmq_connection_status` | Status da conexão | Unhealthy > 30s |
| `event_publish_duration_ms` | Tempo médio de publicação | > 500ms |

---

## 🧪 Testes

### Teste Manual (Swagger)

1. **Cadastrar usuário** via `POST /api/usuarios`
2. **Verificar logs** no console da aplicação
3. **Acessar RabbitMQ Management** (http://localhost:15672)
4. **Verificar exchange** `fcg.users.events`
5. **Verificar mensagem** na aba "Get messages"

### Teste com Consumer Fake

```csharp
// Consumer de teste para visualizar eventos
public class TestConsumer
{
    public void Start()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare("test.all.events", true, false, false);
        channel.QueueBind("test.all.events", "fcg.users.events", "users.*");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"[EVENTO RECEBIDO] {message}");
            channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume("test.all.events", false, consumer);
        Console.ReadLine();
    }
}
```

---

## 📚 Próximos Passos

### Melhorias Futuras

- [ ] **Retry Policy** com exponential backoff
- [ ] **Dead Letter Queue (DLQ)** para eventos com falha
- [ ] **Event Sourcing** completo (armazenar todos os eventos)
- [ ] **Outbox Pattern** (garantir publicação após commit DB)
- [ ] **Idempotência** nos consumers
- [ ] **Telemetria** com OpenTelemetry
- [ ] **Schema Registry** para versionamento de eventos

### Outros Microsserviços a Criar

| Microsserviço | Eventos Consumidos | Responsabilidade |
|---------------|-------------------|------------------|
| **Notifications API** | `users.created`, `users.activated`, `users.password.changed` | Enviar emails/SMS |
| **Library API** | `users.activated` | Provisionar biblioteca vazia |
| **Audit API** | `users.*` | Log de auditoria |
| **Analytics API** | `users.created`, `users.activated` | Métricas e BI |

---

## ⚠️ Considerações de Produção

### Segurança

- ✅ Usar TLS/SSL para conexões RabbitMQ
- ✅ Usuário/senha em **variáveis de ambiente** (não em appsettings)
- ✅ VirtualHost dedicado por ambiente
- ✅ Políticas de acesso restritivas

### Alta Disponibilidade

- ✅ RabbitMQ em **cluster** (3+ nodes)
- ✅ Filas **mirrored** (ha-mode: all)
- ✅ Load balancer na frente do cluster

### Escalabilidade

- ✅ Múltiplas instâncias da API (stateless)
- ✅ Particionamento de filas por routing key
- ✅ Consumer groups para processamento paralelo

---

## 📖 Referências

- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Domain Events Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
- [Eventual Consistency](https://martinfowler.com/articles/microservices.html#EventualConsistency)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)

---

**Implementado em:** 21/10/2026  
**Versão da API:** 1.0  
**Stack:** .NET 8, RabbitMQ 3.12, C# 12
