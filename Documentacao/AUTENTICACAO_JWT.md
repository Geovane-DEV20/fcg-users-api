# 🔐 Autenticação JWT - FCG Users API

## ✅ Implementação Completa!

A API agora possui **autenticação JWT completa** com autorização baseada em perfis.

## 🎯 Novos Endpoints de Autenticação

### 1. Login (Gerar Token)

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "joao@example.com",
  "senha": "SenhaForte123!"
}
```

**Resposta (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjNmYTg1ZjY0LTU3MTctNDU2Mi1iM2ZjLTJjOTYzZjY2YWZhNiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJKb8OjbyBTaWx2YSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6ImpvYW9AZXhhbXBsZS5jb20iLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJVc3VhcmlvIiwicGVyZmlsIjoiVXN1YXJpbyIsInN0YXR1cyI6IkF0aXZhIiwiZXhwIjoxNzAwMDAwMDAwLCJpc3MiOiJGQ0ctVXNlcnMtQVBJIiwiYXVkIjoiRkNHLVVzZXJzLUFQSSJ9.abc123xyz",
  "expiresIn": 3600,
  "usuario": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "nome": "João Silva",
    "email": "joao@example.com",
    "perfil": "Usuario",
    "status": "Ativa",
    "criadoEm": "2024-01-15T10:30:00Z"
  }
}
```

### 2. Obter Usuário Atual (Autenticado)

```http
GET /api/auth/me
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
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

### 3. Logout

```http
POST /api/auth/logout
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Resposta:** 204 No Content

> 💡 **Nota:** JWT é stateless, então o logout é feito no cliente descartando o token.

## 🔒 Proteção dos Endpoints

### Endpoints Públicos (Não Requerem Autenticação)

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/api/usuarios` | Cadastrar usuário |
| POST | `/api/usuarios/ativacao` | Confirmar email |
| POST | `/api/auth/login` | Fazer login |

### Endpoints Autenticados (Requerem Token)

| Método | Endpoint | Perfil | Descrição |
|--------|----------|--------|-----------|
| GET | `/api/auth/me` | Qualquer | Ver seu próprio perfil |
| GET | `/api/usuarios/{id}` | Qualquer | Ver usuário específico |
| PUT | `/api/usuarios/{id}` | Qualquer | Atualizar dados |
| PUT | `/api/usuarios/{id}/senha` | Qualquer | Alterar senha |

### Endpoints Apenas para Administradores

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/usuarios` | Listar todos os usuários |
| PATCH | `/api/usuarios/{id}/perfil` | Alterar perfil de usuário |

## 🧪 Como Testar no Swagger

### Passo 1: Cadastrar um Usuário

```json
POST /api/usuarios
{
  "nome": "Admin User",
  "email": "admin@example.com",
  "senha": "Admin@123!"
}
```

### Passo 2: Confirmar Email

```json
POST /api/usuarios/ativacao
{
  "token": "TOKEN_RETORNADO_NO_CADASTRO"
}
```

### Passo 3: Fazer Login

```json
POST /api/auth/login
{
  "email": "admin@example.com",
  "senha": "Admin@123!"
}
```

**Copie o token retornado!**

### Passo 4: Autorizar no Swagger

1. Clique no botão **"Authorize"** 🔓 (canto superior direito)
2. Digite: `Bearer SEU_TOKEN_AQUI`
3. Clique em **"Authorize"**
4. Clique em **"Close"**

**Agora você está autenticado! 🔐**

### Passo 5: Testar Endpoint Protegido

```http
GET /api/auth/me
```

Deve retornar seus dados!

### Passo 6: Promover para Admin (Opcional)

Para testar endpoints Admin, você precisa ser Administrador:

```sql
-- Manualmente no banco (desenvolvimento)
UPDATE Usuarios 
SET Perfil = 2 
WHERE Email = 'admin@example.com'
```

Depois faça login novamente para obter novo token com role Admin.

## 🔑 Claims do JWT Token

O token contém as seguintes informações (claims):

```json
{
  "nameid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",  // ID do usuário
  "unique_name": "João Silva",                       // Nome
  "email": "joao@example.com",                       // Email
  "role": "Usuario",                                 // Perfil (para autorização)
  "perfil": "Usuario",                               // Perfil (claim customizada)
  "status": "Ativa",                                 // Status da conta
  "exp": 1700000000,                                 // Expiração (timestamp)
  "iss": "FCG-Users-API",                            // Issuer
  "aud": "FCG-Users-API"                             // Audience
}
```

## ⚙️ Configuração JWT

### appsettings.Development.json

```json
{
  "JwtSettings": {
    "SecretKey": "chave-de-desenvolvimento-nao-usar-em-producao-min-32-chars-abcdefghijklmnopqrstuvwxyz123456",
    "Issuer": "FCG-Users-API-Dev",
    "Audience": "FCG-Users-API-Dev",
    "ExpiracaoEmMinutos": 120
  }
}
```

### appsettings.json (Produção)

```json
{
  "JwtSettings": {
    "SecretKey": "sua-chave-secreta-super-segura-com-no-minimo-32-caracteres-para-producao-usar-variavel-de-ambiente",
    "Issuer": "FCG-Users-API",
    "Audience": "FCG-Users-API",
    "ExpiracaoEmMinutos": 60
  }
}
```

> ⚠️ **IMPORTANTE:** Em produção, **NUNCA** coloque a SecretKey no código!
> Use variáveis de ambiente ou Azure Key Vault.

## 🔐 Segurança

### Algoritmo de Assinatura

- **Algoritmo:** HMAC SHA256 (HS256)
- **Chave Simétrica:** Mínimo 32 caracteres
- **Expiração:** 60 minutos (produção) / 120 minutos (desenvolvimento)

### Validações Aplicadas

✅ **ValidateIssuerSigningKey** - Valida assinatura do token  
✅ **ValidateIssuer** - Valida emissor  
✅ **ValidateAudience** - Valida audiência  
✅ **ValidateLifetime** - Valida expiração  
✅ **ClockSkew = Zero** - Sem tolerância de tempo

## 📊 Autorização Baseada em Perfis

### Políticas Configuradas

```csharp
options.AddPolicy("AdminOnly", policy => 
    policy.RequireRole("Administrador"));

options.AddPolicy("UserOrAdmin", policy => 
    policy.RequireRole("Usuario", "Administrador"));
```

### Uso nos Controllers

```csharp
[Authorize]                              // Qualquer usuário autenticado
[Authorize(Roles = "Administrador")]     // Apenas Admin
[Authorize(Policy = "AdminOnly")]        // Usando política
[AllowAnonymous]                         // Público
```

## 🔄 Fluxo Completo

```
┌──────────┐                  ┌──────────────┐
│ Cliente  │                  │   Users API  │
└─────┬────┘                  └──────┬───────┘
      │                              │
      │ 1. POST /api/usuarios        │
      ├─────────────────────────────>│
      │                              │
      │ <─ ID do usuário             │
      │<──────────────────────────── │
      │                              │
      │ 2. POST /usuarios/ativacao   │
      ├─────────────────────────────>│
      │                              │
      │ <─ 204 No Content            │
      │<──────────────────────────── │
      │                              │
      │ 3. POST /api/auth/login      │
      ├─────────────────────────────>│
      │                              │
      │ <─ JWT Token                 │
      │<──────────────────────────── │
      │                              │
      │ 4. GET /api/auth/me          │
      │    Authorization: Bearer ... │
      ├─────────────────────────────>│
      │                              │
      │ <─ Dados do usuário          │
      │<──────────────────────────── │
```

## ❌ Erros Comuns

### 401 Unauthorized

**Causa:** Token não enviado, inválido ou expirado

**Solução:**
```http
Authorization: Bearer SEU_TOKEN_AQUI
```

### 403 Forbidden

**Causa:** Usuário autenticado mas sem permissão (não é Admin)

**Solução:** Promover usuário para Administrador

### Token Expirado

**Causa:** Token passou da validade (60/120 minutos)

**Solução:** Fazer login novamente

## 🧪 Exemplo com cURL

### 1. Login
```bash
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","senha":"Admin@123!"}'
```

### 2. Usar Token
```bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

curl -X GET "http://localhost:5000/api/auth/me" \
  -H "Authorization: Bearer $TOKEN"
```

### 3. Endpoint Admin
```bash
curl -X GET "http://localhost:5000/api/usuarios?pagina=1&tamanhoPagina=10" \
  -H "Authorization: Bearer $TOKEN"
```

## 📝 Próximas Melhorias (Futuras)

- [ ] Refresh Tokens para renovação automática
- [ ] Blacklist de tokens (logout real)
- [ ] Múltiplos devices por usuário
- [ ] Two-Factor Authentication (2FA)
- [ ] Social Login (Google, Facebook)
- [ ] Rate Limiting por usuário
- [ ] Audit Log de acessos

## 💡 Conclusão

A API agora está **100% protegida** com:

✅ **Autenticação JWT** completa  
✅ **Autorização** baseada em perfis  
✅ **Endpoints protegidos** por roles  
✅ **Swagger integrado** com autenticação  
✅ **Pronta para produção** (com variáveis de ambiente)

**Segurança implementada! 🔐**
