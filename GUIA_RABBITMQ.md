# 🐰 Guia Rápido - RabbitMQ e Comunicação Assíncrona

## 🎯 Visão Geral

A API FCG Users agora publica **eventos de domínio** via RabbitMQ sempre que ações importantes acontecem:

✅ **UsuarioCriado** - quando um novo usuário se cadastra  
✅ **ContaAtivada** - quando o usuário confirma o email  
✅ **SenhaAlterada** - quando o usuário altera a senha  
✅ **PerfilAlterado** - quando um admin altera o perfil do usuário

---

## 🚀 Como Executar Localmente

### 1️⃣ Subir o RabbitMQ (Docker)

```bash
# Na pasta raiz do projeto
docker-compose -f docker-compose.rabbitmq.yml up -d
```

**Verificar se está rodando:**
```bash
docker ps | findstr rabbitmq
```

**Acessar Management UI:**
- URL: http://localhost:15672
- Usuário: `guest`
- Senha: `guest`

### 2️⃣ Executar a API

```bash
dotnet run
```

A API vai conectar automaticamente no RabbitMQ e criar o exchange `fcg.users.events.dev`.

### 3️⃣ Testar a Publicação de Eventos

**No Swagger (http://localhost:62306/swagger):**

1. **Cadastrar usuário** → POST `/api/usuarios`
   ```json
   {
     "nome": "João Silva",
     "email": "joao@example.com",
     "senha": "SenhaForte123!",
     "perfil": "Usuario"
   }
   ```

2. **Verificar no console da API:**
   ```
   info: fcg_users_api.Events.RabbitMQ.RabbitMQEventPublisher[0]
         Evento publicado: UsuarioCriado | ID: ... | RoutingKey: users.created
   ```

3. **Verificar no RabbitMQ Management:**
   - Aba **Exchanges** → `fcg.users.events.dev`
   - Ver mensagem publicada

---

## 🔍 Visualizar Eventos em Tempo Real

### Opção 1: RabbitMQ Management UI

1. Acesse http://localhost:15672
2. Vá em **Exchanges** → `fcg.users.events.dev`
3. Clique em **Get messages**
4. Routing key: `users.*`
5. Clique em **Get Message(s)**

### Opção 2: Consumer de Teste (CLI)

Execute este comando para escutar todos os eventos:

```bash
# Instalar ferramenta CLI do RabbitMQ (opcional)
docker exec -it fcg-rabbitmq rabbitmqadmin get queue=test.all.events
```

### Opção 3: Consumer Programático

Veja o arquivo `Exemplos/ConsumerExemplo.cs` para exemplos completos de consumers em C#.

---

## 📊 Estrutura dos Eventos

### 1. UsuarioCriadoEvent
```json
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "ocorridoEm": "2026-10-21T10:30:00Z",
  "eventType": "UsuarioCriado",
  "usuarioId": "...",
  "nome": "João Silva",
  "email": "joao@example.com",
  "perfil": "Usuario",
  "criadoEm": "2026-10-21T10:30:00Z"
}
```

### 2. ContaAtivadaEvent
```json
{
  "eventId": "...",
  "ocorridoEm": "2026-10-21T10:35:00Z",
  "eventType": "ContaAtivada",
  "usuarioId": "...",
  "email": "joao@example.com",
  "ativadaEm": "2026-10-21T10:35:00Z"
}
```

### 3. SenhaAlteradaEvent
```json
{
  "eventId": "...",
  "ocorridoEm": "2026-10-21T11:00:00Z",
  "eventType": "SenhaAlterada",
  "usuarioId": "...",
  "email": "joao@example.com",
  "alteradaEm": "2026-10-21T11:00:00Z"
}
```

### 4. PerfilAlteradoEvent
```json
{
  "eventId": "...",
  "ocorridoEm": "2026-10-21T12:00:00Z",
  "eventType": "PerfilAlterado",
  "usuarioId": "...",
  "email": "joao@example.com",
  "perfilAntigo": "Usuario",
  "perfilNovo": "Administrador",
  "alteradoPor": "admin@example.com"
}
```

---

## 🔗 Routing Keys

| Evento | Routing Key | Descrição |
|--------|-------------|-----------|
| UsuarioCriadoEvent | `users.created` | Novo usuário cadastrado |
| ContaAtivadaEvent | `users.activated` | Conta ativada com sucesso |
| SenhaAlteradaEvent | `users.password.changed` | Senha alterada |
| PerfilAlteradoEvent | `users.profile.changed` | Perfil modificado |

**Para receber TODOS os eventos de usuários:** use routing key `users.*`

---

## 🛠️ Como Criar um Consumer em Outro Microsserviço

### Exemplo: Microsserviço de Notificações

```csharp
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class NotificationsService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory 
        { 
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        var connection = factory.CreateConnection("Notifications-Service");
        var channel = connection.CreateModel();

        // Declarar fila exclusiva para notificações
        channel.QueueDeclare(
            queue: "notifications.users",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        // Bind para eventos que requerem email
        channel.QueueBind("notifications.users", "fcg.users.events", "users.created");
        channel.QueueBind("notifications.users", "fcg.users.events", "users.activated");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            
            if (ea.RoutingKey == "users.created")
            {
                var evento = JsonSerializer.Deserialize<UsuarioCriadoEvent>(json);
                await EnviarEmailBoasVindas(evento.Email, evento.Nome);
            }
            
            channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume("notifications.users", false, consumer);
        
        return Task.CompletedTask;
    }
}
```

---

## ✅ Checklist de Implementação

Para criar um novo microsserviço que consome eventos:

- [ ] Adicionar pacote `RabbitMQ.Client`
- [ ] Criar ConnectionFactory com mesmas credenciais
- [ ] Declarar fila dedicada com nome único
- [ ] Fazer bind para routing keys desejadas
- [ ] Implementar consumer com lógica de negócio
- [ ] Usar `BasicAck` para confirmar processamento
- [ ] Tratar erros com `BasicNack` (requeue)
- [ ] Implementar idempotência (processar evento apenas 1x)

---

## 🚨 Troubleshooting

### API não conecta ao RabbitMQ

**Erro:** `Connection failed: Cannot connect to RabbitMQ`

**Solução:**
```bash
# Verificar se RabbitMQ está rodando
docker ps | findstr rabbitmq

# Se não estiver, subir novamente
docker-compose -f docker-compose.rabbitmq.yml up -d

# Verificar logs
docker logs fcg-rabbitmq
```

### Eventos não aparecem no RabbitMQ

**Verificar:**
1. API conectou com sucesso? (verificar logs no console)
2. Exchange foi criado? (verificar em http://localhost:15672)
3. Há consumidores conectados? (aba Queues)

### Consumer não recebe mensagens

**Causas comuns:**
- Routing key incorreta no bind
- Fila não está conectada ao exchange
- AutoAck = true (eventos confirmados antes de processar)

---

## 📚 Próximos Passos

### Microsserviços Sugeridos

| Serviço | Eventos | Função |
|---------|---------|--------|
| **Notifications API** | `users.created`, `users.activated` | Enviar emails |
| **Library API** | `users.activated` | Criar biblioteca vazia |
| **Audit API** | `users.*` | Log de auditoria |
| **Analytics API** | `users.created` | Métricas e BI |

### Melhorias Futuras

- [ ] Implementar **Dead Letter Queue (DLQ)** para erros
- [ ] Adicionar **Retry Policy** com exponential backoff
- [ ] Implementar **Outbox Pattern** (publicação após commit DB)
- [ ] Adicionar **Telemetria** com OpenTelemetry
- [ ] Implementar **Schema Registry** para versionamento

---

## 📖 Documentação Adicional

- [COMUNICACAO_ASSINCRONA_IMPLEMENTACAO.md](./Documentacao/COMUNICACAO_ASSINCRONA_IMPLEMENTACAO.md) - Detalhes técnicos completos
- [ConsumerExemplo.cs](./Exemplos/ConsumerExemplo.cs) - Exemplos de código
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)

---

**Implementado em:** 21/10/2026  
**Versão:** 1.1.0  
**Stack:** .NET 8 + RabbitMQ 3.12
