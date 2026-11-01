# 🏗️ Diagrama de Microsserviços - FCG Platform

## 📊 Arquitetura Completa (Planejada)

```
                            ┌─────────────────────────────────────┐
                            │      API Gateway / Load Balancer    │
                            │       (Nginx / Azure API Gateway)   │
                            └──────────────┬──────────────────────┘
                                           │
                    ┌──────────────────────┼──────────────────────┐
                    │                      │                      │
                    ▼                      ▼                      ▼
        ┌───────────────────┐  ┌───────────────────┐  ┌──────────────────┐
        │   Users API       │  │   Games API       │  │  Library API     │
        │   (Port 5001)     │  │   (Port 5002)     │  │  (Port 5003)     │
        │                   │  │                   │  │                  │
        │ • Cadastro        │  │ • Catálogo jogos  │  │ • Biblioteca     │
        │ • Autenticação    │  │ • Compras         │  │ • Downloads      │
        │ • Perfis          │  │ • Avaliações      │  │ • Wishlist       │
        └─────────┬─────────┘  └─────────┬─────────┘  └────────┬─────────┘
                  │                      │                      │
                  │ ┌────────────────────┼──────────────────────┘
                  │ │                    │
                  ▼ ▼                    ▼
        ┌─────────────────────────────────────────────────────┐
        │          Message Broker (Event Bus)                  │
        │       RabbitMQ / Azure Service Bus / Kafka          │
        │                                                      │
        │  Events:                                            │
        │  • UsuarioCriado                                    │
        │  • EmailConfirmado                                  │
        │  • JogoComprado                                     │
        │  • ItemAdicionadoBiblioteca                         │
        └─────────────────────────────────────────────────────┘
                  │                      │                      │
                  ▼                      ▼                      ▼
        ┌───────────────────┐  ┌───────────────────┐  ┌──────────────────┐
        │   FcgUsersDb      │  │   FcgGamesDb      │  │  FcgLibraryDb    │
        │   (SQL Server)    │  │   (SQL Server)    │  │  (SQL Server)    │
        │                   │  │                   │  │                  │
        │ Tables:           │  │ Tables:           │  │ Tables:          │
        │ • Usuarios        │  │ • Games           │  │ • Bibliotecas    │
        │                   │  │ • Compras         │  │ • Items          │
        │                   │  │ • Avaliacoes      │  │ • Downloads      │
        │                   │  │ • UserSummaries*  │  │ • UserSummaries* │
        └───────────────────┘  └───────────────────┘  └──────────────────┘

        * UserSummaries = Cópia desnormalizada de dados básicos do usuário
```

## 🎯 Bancos de Dados Separados

### 1️⃣ FcgUsersDb (Users API)

**Propósito:** Gerenciamento de identidade e acesso

```sql
┌─────────────────────────────────────────┐
│            FcgUsersDb                    │
├─────────────────────────────────────────┤
│                                         │
│  Usuarios                               │
│  ├── Id (PK)                            │
│  ├── Nome                               │
│  ├── Email (UNIQUE)                     │
│  ├── SenhaHash                          │
│  ├── Perfil (Usuario/Admin)            │
│  ├── Status                             │
│  ├── TokenAtivacao                      │
│  ├── TokenAtivacaoExpiraEm             │
│  ├── TentativasFalhasConsecutivas      │
│  ├── BloqueadaAte                       │
│  └── CriadoEm                           │
│                                         │
└─────────────────────────────────────────┘

Acesso: EXCLUSIVO da Users API
Tamanho: ~500 MB (10k usuários)
Backup: Diário
```

### 2️⃣ FcgGamesDb (Games API)

**Propósito:** Catálogo e marketplace de jogos

```sql
┌─────────────────────────────────────────┐
│            FcgGamesDb                    │
├─────────────────────────────────────────┤
│                                         │
│  Games                                  │
│  ├── Id (PK)                            │
│  ├── Titulo                             │
│  ├── Descricao                          │
│  ├── Preco                              │
│  ├── Categoria                          │
│  └── ...                                │
│                                         │
│  Compras                                │
│  ├── Id (PK)                            │
│  ├── UsuarioId (FK virtual)            │
│  ├── GameId (FK)                        │
│  ├── Valor                              │
│  └── DataCompra                         │
│                                         │
│  Avaliacoes                             │
│  ├── Id (PK)                            │
│  ├── UsuarioId (FK virtual)            │
│  ├── GameId (FK)                        │
│  ├── Nota                               │
│  └── Comentario                         │
│                                         │
│  UserSummaries (Cache)                  │
│  ├── Id (PK) = UsuarioId               │
│  ├── Nome                               │
│  └── Email                              │
│                                         │
└─────────────────────────────────────────┘

Acesso: EXCLUSIVO da Games API
Tamanho: ~5 GB (1M compras)
Backup: Diário
```

### 3️⃣ FcgLibraryDb (Library API)

**Propósito:** Biblioteca pessoal e downloads

```sql
┌─────────────────────────────────────────┐
│          FcgLibraryDb                    │
├─────────────────────────────────────────┤
│                                         │
│  Bibliotecas                            │
│  ├── Id (PK)                            │
│  ├── UsuarioId (FK virtual)            │
│  └── CriadaEm                           │
│                                         │
│  Items                                  │
│  ├── Id (PK)                            │
│  ├── BibliotecaId (FK)                 │
│  ├── GameId (FK virtual)               │
│  ├── DataAdicao                         │
│  └── Status                             │
│                                         │
│  Downloads                              │
│  ├── Id (PK)                            │
│  ├── ItemId (FK)                        │
│  ├── DataDownload                       │
│  └── Versao                             │
│                                         │
│  UserSummaries (Cache)                  │
│  ├── Id (PK) = UsuarioId               │
│  ├── Nome                               │
│  └── Email                              │
│                                         │
└─────────────────────────────────────────┘

Acesso: EXCLUSIVO da Library API
Tamanho: ~2 GB (500k itens)
Backup: Diário
```

## 🔄 Fluxos de Comunicação (100% Assíncrono)

### Fluxo 1: Compra de Jogo (Saga Assíncrona)

```
Cliente                 Games API        Message Broker       Library API
  │                        │                    │                    │
  │  POST /games/123/buy   │                    │                    │
  ├───────────────────────>│                    │                    │
  │                        │                    │                    │
  │                        │ Valida Compra      │                    │
  │                        │ (UserSummary local)│                    │
  │                        │                    │                    │
  │                        │ Cria Compra        │                    │
  │                        │ (FcgGamesDb)       │                    │
  │                        │                    │                    │
  │  <─ Success 200        │                    │                    │
  │<───────────────────────┤                    │                    │
  │                        │                    │                    │
  │                        │ JogoCompradoEvent  │                    │
  │                        ├───────────────────>│                    │
  │                        │                    │                    │
  │                        │                    │ JogoCompradoEvent  │
  │                        │                    ├───────────────────>│
  │                        │                    │                    │
  │                        │                    │                    │ Adiciona à Biblioteca
  │                        │                    │                    │ (FcgLibraryDb)
  │                        │                    │                    │
  │                        │                    │ BibliotecaAtualizadaEvent
  │                        │                    │<───────────────────┤
```

**Importante:** O cliente recebe resposta ANTES da biblioteca ser atualizada (eventual consistency).

### Fluxo 2: Criação de Usuário (com propagação)

```
Cliente       Users API       Message Broker      Games API        Library API
  │               │                  │                  │                 │
  │ POST /usuarios│                  │                  │                 │
  ├──────────────>│                  │                  │                 │
  │               │                  │                  │                 │
  │               │ Cria Usuario     │                  │                 │
  │               │ (FcgUsersDb)     │                  │                 │
  │               │                  │                  │                 │
  │               │ UsuarioCriadoEvent                  │                 │
  │               ├─────────────────>│                  │                 │
  │               │                  │                  │                 │
  │               │                  │ UsuarioCriadoEvent                 │
  │               │                  ├─────────────────>│                 │
  │               │                  │                  │                 │
  │               │                  │                  │ Cria UserSummary│
  │               │                  │                  │  (FcgGamesDb)   │
  │               │                  │                  │                 │
  │               │                  │ UsuarioCriadoEvent                 │
  │               │                  ├─────────────────────────────────>  │
  │               │                  │                  │                 │
  │               │                  │                  │  Cria Biblioteca│
  │               │                  │                  │   & UserSummary │
  │               │                  │                  │  (FcgLibraryDb) │
  │               │                  │                  │                 │
  │ <─ ID Usuario │                  │                  │                 │
  │<──────────────┤                  │                  │                 │
```

## 🔐 Isolamento e Segurança

### Regras de Acesso ao Banco

```
┌─────────────────────────────────────────────────────────────┐
│                    Network Security                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  FcgUsersDb                                                │
│  ├─ Firewall Rules                                        │
│  │  ├─ Allow: Users API IP (10.0.1.10)                   │
│  │  └─ Deny: All other IPs                               │
│  └─ Credentials                                           │
│     └─ User: fcg_users_api (READ/WRITE apenas em Usuarios)│
│                                                             │
│  FcgGamesDb                                                │
│  ├─ Firewall Rules                                        │
│  │  ├─ Allow: Games API IP (10.0.1.20)                   │
│  │  └─ Deny: All other IPs                               │
│  └─ Credentials                                           │
│     └─ User: fcg_games_api (READ/WRITE apenas em Games/*) │
│                                                             │
│  FcgLibraryDb                                              │
│  ├─ Firewall Rules                                        │
│  │  ├─ Allow: Library API IP (10.0.1.30)                 │
│  │  └─ Deny: All other IPs                               │
│  └─ Credentials                                           │
│     └─ User: fcg_library_api (READ/WRITE em Bibliotecas/*)│
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 📈 Escalabilidade por Serviço

### Configuração de Recursos

```
┌───────────────────────────────────────────────────────┐
│                  Scaling Strategy                     │
├───────────────────────────────────────────────────────┤
│                                                       │
│  Users API                                           │
│  ├─ Instâncias: 2-4 (Auto-scale)                    │
│  ├─ Database: Azure SQL S2 (50 DTU)                 │
│  ├─ Cache: Redis 1GB                                 │
│  └─ Prioridade: HIGH (crítico)                       │
│                                                       │
│  Games API                                           │
│  ├─ Instâncias: 4-8 (Auto-scale)                    │
│  ├─ Database: Azure SQL S4 (200 DTU)                │
│  ├─ Cache: Redis 2GB                                 │
│  └─ Prioridade: HIGH (alto tráfego)                  │
│                                                       │
│  Library API                                         │
│  ├─ Instâncias: 2-4 (Auto-scale)                    │
│  ├─ Database: Azure SQL S3 (100 DTU)                │
│  ├─ Cache: Redis 1GB                                 │
│  ├─ CDN: Azure CDN (para downloads)                  │
│  └─ Prioridade: MEDIUM                               │
│                                                       │
└───────────────────────────────────────────────────────┘
```

## 🔄 Replicação de Dados

### UserSummaries (Eventual Consistency)

```
FcgUsersDb                                Other Databases
  Usuarios                              
  ├─ Id: 123                              FcgGamesDb.UserSummaries
  ├─ Nome: "João"                         ├─ Id: 123
  ├─ Email: "joao@..."                    ├─ Nome: "João"
  └─ ...                                  └─ Email: "joao@..."
                                          
        │                                 FcgLibraryDb.UserSummaries
        │ UsuarioCriado                   ├─ Id: 123
        │ Event                           ├─ Nome: "João"
        └──────────────────>              └─ Email: "joao@..."

Quando atualizar:
• Nome alterado → Evento → Atualiza todas as cópias
• Email confirmado → Evento → Atualiza status em cópias
• Perfil alterado → Evento → Atualiza permissões
```

## 🎯 Benefícios Práticos

### ✅ Deployment Independente

```
Cenário: Atualização da Games API

Users API ────────> [Running v1.2.0] ────> FcgUsersDb
                    [Não afetado]

Games API ────────> [Deploying v2.0.0] ──> FcgGamesDb
                    [Nova migration]       [Schema update]

Library API ──────> [Running v1.5.0] ────> FcgLibraryDb
                    [Não afetado]
```

### ✅ Falha Isolada

```
Cenário: FcgGamesDb inoperante

Users API ────────> ✅ FUNCIONANDO ────> FcgUsersDb (OK)
Library API ──────> ✅ FUNCIONANDO ────> FcgLibraryDb (OK)
Games API ────────> ❌ DEGRADADO ──────> FcgGamesDb (DOWN)
                    └─> Catálogo indisponível, mas login OK
```

### ✅ Otimização Específica

```
Users API
└─> FcgUsersDb: Otimizado para READS rápidos
    ├─ Índices em Email, TokenAtivacao
    ├─ Cache de queries frequentes
    └─ Connection pool pequeno (5-10 conexões)

Games API
└─> FcgGamesDb: Otimizado para WRITES e buscas complexas
    ├─ Índices em múltiplas colunas
    ├─ Full-text search
    └─ Connection pool maior (20-50 conexões)
```

## 📖 Conclusão

Cada microsserviço possui seu **banco de dados completamente isolado**, garantindo autonomia, escalabilidade e resiliência. A comunicação entre serviços acontece exclusivamente via:

1. **HTTP REST** (síncrono) - para consultas em tempo real
2. **Message Broker** (assíncrono) - para eventos e sincronização eventual

Esta arquitetura permite que cada time desenvolva, implante e escale seu serviço de forma independente.
