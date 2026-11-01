# 📝 Changelog - Implementação de Eventos de Domínio

## Versão 1.1.0 - Comunicação Assíncrona (21/10/2026)

### ✨ Novas Funcionalidades

#### 🐰 Integração com RabbitMQ

**Arquivos Criados:**
- `Events/IEventPublisher.cs` - Interface para publicação de eventos
- `Events/RabbitMQ/RabbitMQEventPublisher.cs` - Implementação do publisher
- `Events/RabbitMQ/RabbitMQHealthCheck.cs` - Health check da conexão
- `Events/DomainEvents/IDomainEvent.cs` - Interface base para eventos
- `Events/DomainEvents/UsuarioCriadoEvent.cs` - Evento de usuário criado
- `Events/DomainEvents/ContaAtivadaEvent.cs` - Evento de conta ativada
- `Events/DomainEvents/SenhaAlteradaEvent.cs` - Evento de senha alterada
- `Events/DomainEvents/PerfilAlteradoEvent.cs` - Evento de perfil alterado

**Funcionalidades:**
- ✅ Publicação automática de eventos de domínio via RabbitMQ
- ✅ Exchange tipo `topic` para roteamento flexível
- ✅ Mensagens persistentes (durable)
- ✅ Auto-recovery da conexão
- ✅ Health check endpoint `/health` para monitorar RabbitMQ
- ✅ Eventual consistency (operações não falham se RabbitMQ estiver offline)

#### 📦 Eventos de Domínio Publicados

| Evento | Routing Key | Gatilho |
|--------|-------------|---------|
| **UsuarioCriadoEvent** | `users.created` | Novo usuário se cadastra |
| **ContaAtivadaEvent** | `users.activated` | Usuário confirma email |
| **SenhaAlteradaEvent** | `users.password.changed` | Usuário altera senha |
| **PerfilAlteradoEvent** | `users.profile.changed` | Admin altera perfil |

#### 🔧 Modificações em Arquivos Existentes

**CadastroService.cs:**
- Adicionado `IEventPublisher` via injeção de dependência
- Publicação de `UsuarioCriadoEvent` após cadastro
- Publicação de `ContaAtivadaEvent` após ativação
- Publicação de `SenhaAlteradaEvent` após alteração de senha
- Publicação de `PerfilAlteradoEvent` após mudança de perfil
- Tratamento de erros com logging (não interrompe operação)

**Program.cs:**
- Registrado `RabbitMQEventPublisher` como Singleton
- Adicionado Health Check para RabbitMQ
- Configurado endpoint `/health`

**appsettings.json:**
```json
"RabbitMQ": {
  "HostName": "localhost",
  "Port": "5672",
  "UserName": "guest",
  "Password": "guest",
  "VirtualHost": "/",
  "ExchangeName": "fcg.users.events"
}
```

**appsettings.Development.json:**
- Exchange dedicado para dev: `fcg.users.events.dev`
- Log level `Debug` para namespace `fcg_users_api.Events`

**fcg-users-api.csproj:**
- Adicionado pacote `RabbitMQ.Client` versão 6.8.1

#### 📄 Documentação

**Novos Arquivos:**
- `GUIA_RABBITMQ.md` - Guia rápido de uso
- `Documentacao/COMUNICACAO_ASSINCRONA_IMPLEMENTACAO.md` - Documentação técnica completa
- `docker-compose.rabbitmq.yml` - Docker Compose para RabbitMQ local
- `Exemplos/ConsumerExemplo.cs` - Exemplos de consumers

---

## 🏗️ Arquitetura Implementada

### Padrões Utilizados

1. **Domain Events Pattern**
   - Eventos representam fatos que ocorreram no domínio
   - Desacoplamento entre agregados
   - Eventual consistency

2. **Publisher/Subscriber**
   - API publica eventos via RabbitMQ
   - Outros microsserviços subscrevem aos eventos desejados
   - Comunicação 100% assíncrona

3. **Topic Exchange**
   - Roteamento flexível via routing keys
   - Wildcards (`users.*`) para múltiplos eventos
   - Fácil adicionar novos consumers

### Fluxo de Eventos

```
Usuario se cadastra
    ↓
CadastroService salva no DB
    ↓
EventPublisher.PublicarAsync(UsuarioCriadoEvent)
    ↓
RabbitMQ recebe com routing key "users.created"
    ↓
Consumers interessados processam o evento
```

---

## 🔍 Como Testar

### 1. Subir RabbitMQ
```bash
docker-compose -f docker-compose.rabbitmq.yml up -d
```

### 2. Verificar RabbitMQ Management
- URL: http://localhost:15672
- Usuário: `guest` / Senha: `guest`

### 3. Executar a API
```bash
dotnet run
```

### 4. Cadastrar um Usuário (Swagger)
```
POST /api/usuarios
{
  "nome": "Teste",
  "email": "teste@example.com",
  "senha": "Senha123!",
  "perfil": "Usuario"
}
```

### 5. Verificar Logs
```
info: fcg_users_api.Events.RabbitMQ.RabbitMQEventPublisher[0]
      Evento publicado: UsuarioCriado | ID: ... | RoutingKey: users.created
```

### 6. Verificar no RabbitMQ
- Exchanges → `fcg.users.events.dev`
- Ver mensagens publicadas

---

## 📊 Estatísticas da Implementação

**Arquivos Criados:** 13  
**Arquivos Modificados:** 5  
**Linhas de Código:** ~1.200+  
**Pacotes Adicionados:** 1 (RabbitMQ.Client)  
**Eventos Implementados:** 4  
**Endpoints Novos:** 1 (/health)

---

## 🚀 Próximas Etapas Sugeridas

### Fase 1: Consumers (Próxima Sprint)
- [ ] Microsserviço de Notificações (enviar emails)
- [ ] Microsserviço de Library (provisionar biblioteca)
- [ ] Microsserviço de Audit (log de auditoria)

### Fase 2: Resiliência
- [ ] Implementar Dead Letter Queue (DLQ)
- [ ] Retry policy com exponential backoff
- [ ] Outbox pattern (publicação transacional)

### Fase 3: Observabilidade
- [ ] OpenTelemetry para tracing distribuído
- [ ] Métricas customizadas (eventos publicados, falhas, latência)
- [ ] Dashboards no Grafana

### Fase 4: Governança
- [ ] Schema Registry para versionamento de eventos
- [ ] Validação de eventos com JSON Schema
- [ ] Catálogo de eventos corporativo

---

## ⚠️ Breaking Changes

**Nenhum breaking change nesta versão.**

A implementação de eventos foi feita de forma **aditiva** (não quebra código existente):
- ✅ Todos os endpoints continuam funcionando
- ✅ API funciona mesmo se RabbitMQ estiver offline
- ✅ Logs informativos, não erros críticos

---

## 🐛 Bugs Conhecidos

**Warning no Build:**
```
warning CS0618: "IBasicPublishBatch.Add(...)" é obsoleto
```

**Status:** Conhecido e sem impacto  
**Causa:** API do RabbitMQ.Client está deprecando método em batch  
**Ação:** Será corrigido quando atualizar para RabbitMQ.Client 7.x

---

## 📚 Referências

- [Domain Events - Microsoft](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html)

---

**Data:** 21/10/2026  
**Autor:** Kiro AI Assistant  
**Versão:** 1.1.0  
**Commit:** (pendente)
