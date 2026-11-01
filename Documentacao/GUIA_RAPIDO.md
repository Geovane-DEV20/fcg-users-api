# 🚀 Guia Rápido - FCG Users API

## ⚡ Início Rápido

### 1. Executar a API

```powershell
# Restaurar dependências
dotnet restore

# Compilar o projeto
dotnet build

# Executar a API
dotnet run
```

A API estará disponível em:
- HTTPS: https://localhost:5001
- Swagger: https://localhost:5001/swagger

### 2. Testar o Cadastro de Usuário

**Requisição:**
```bash
curl -X POST https://localhost:5001/api/usuarios \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João Silva",
    "email": "joao@example.com",
    "senha": "SenhaForte123!"
  }'
```

**Resposta (201 Created):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### 3. Confirmar Email

No cadastro, um token é gerado. Para ativação:

**Requisição:**
```bash
curl -X POST https://localhost:5001/api/usuarios/ativacao \
  -H "Content-Type: application/json" \
  -d '{
    "token": "abc123def456"
  }'
```

**Resposta:** 204 No Content

### 4. Obter Usuário

```bash
curl https://localhost:5001/api/usuarios/3fa85f64-5717-4562-b3fc-2c963f66afa6
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

## 📋 Regras de Validação

### Email
✅ Formato válido: `usuario@dominio.com`
❌ Inválido: `email-sem-arroba`, `@semdominio`, `sem@dominio`

### Senha
✅ Válida: `SenhaForte123!`, `Abcd@1234`, `Teste#99X`

Requisitos:
- Mínimo 8 caracteres
- Pelo menos uma letra
- Pelo menos um número
- Pelo menos um caractere especial

❌ Inválida: `123456` (sem letra), `abcdefgh` (sem número), `abc123` (sem especial)

### Nome
✅ Válido: Qualquer texto não vazio
❌ Inválido: String vazia ou apenas espaços

## 🔄 Fluxo de Cadastro

```
1. Cliente envia POST /api/usuarios
   ↓
2. API valida email e senha
   ↓
3. API verifica se email já existe
   ↓
4. API cria usuário com status "PendenteAtivacao"
   ↓
5. API gera token de ativação (válido por 24h)
   ↓
6. API retorna ID do usuário
   ↓
7. [TODO] Sistema envia email com token
   ↓
8. Cliente envia POST /api/usuarios/ativacao com token
   ↓
9. API valida token e muda status para "Ativa"
   ↓
10. Usuário pode fazer login
```

## 🛑 Códigos de Erro Comuns

| Código | Mensagem | Status HTTP |
|--------|----------|-------------|
| `EMAIL_INVALIDO` | E-mail formato inválido | 400 |
| `EMAIL_DUPLICADO_DETECTADO` | Email já cadastrado | 409 |
| `SENHA_FRACA` | Senha não atende requisitos | 400 |
| `NOME_INVALIDO` | Nome vazio | 400 |
| `TOKEN_ATIVACAO_INVALIDO` | Token não encontrado | 400 |
| `TOKEN_ATIVACAO_EXPIRADO` | Token vencido (>24h) | 410 |
| `USUARIO_NAO_ENCONTRADO` | ID não existe | 404 |
| `CONTA_JA_ATIVADA` | Conta já ativada antes | 422 |

## 📊 Exemplos de Erro

**Email duplicado (409):**
```json
{
  "codigo": "EMAIL_DUPLICADO_DETECTADO",
  "mensagem": "Já existe uma conta cadastrada com este e-mail."
}
```

**Senha fraca (400):**
```json
{
  "codigo": "SENHA_FRACA",
  "mensagem": "A senha deve conter letra, número e caractere especial."
}
```

**Token expirado (410):**
```json
{
  "codigo": "TOKEN_ATIVACAO_EXPIRADO",
  "mensagem": "O token de ativação expirou."
}
```

## 🔍 Listar Usuários

```bash
curl "https://localhost:5001/api/usuarios?pagina=1&tamanhoPagina=10"
```

**Resposta:**
```json
{
  "usuarios": [
    {
      "id": "...",
      "nome": "João Silva",
      "email": "joao@example.com",
      "perfil": "Usuario",
      "status": "Ativa",
      "criadoEm": "2024-01-15T10:30:00Z"
    }
  ],
  "total": 50,
  "pagina": 1,
  "tamanhoPagina": 10,
  "totalPaginas": 5
}
```

## ⚙️ Configuração de Banco de Dados

### Desenvolvimento (InMemory - Padrão)
Sem configuração necessária. Dados perdidos ao reiniciar.

### Produção (SQL Server)

1. Edite `Program.cs`:
```csharp
// Comente esta linha:
// options.UseInMemoryDatabase("FcgUsersDb");

// Descomente esta linha:
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
```

2. Configure `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=seu-servidor;Database=FcgUsersDb;..."
  }
}
```

3. Crie a migration:
```powershell
dotnet ef migrations add InitialCreate
```

4. Aplique no banco:
```powershell
dotnet ef database update
```

## 🐳 Docker (Opcional)

O projeto já possui `Dockerfile`. Para usar:

```powershell
# Build da imagem
docker build -t fcg-users-api .

# Executar container
docker run -p 5001:8080 fcg-users-api
```

## 📝 Testando com Arquivo HTTP

Use o arquivo `Exemplos/requisicoes.http` com extensões como:
- REST Client (VS Code)
- HTTP Client (IntelliJ/Rider)

## 🔐 Status de Conta

| Status | Descrição | Pode logar? |
|--------|-----------|-------------|
| `PendenteAtivacao` | Recém cadastrado | ❌ Não |
| `Ativa` | Email confirmado | ✅ Sim |
| `Desativada` | Conta desabilitada | ❌ Não |

## 🛡️ Segurança de Senha

### Hash PBKDF2
- **Algoritmo:** SHA256
- **Iterações:** 100.000
- **Salt:** 16 bytes (aleatório por senha)
- **Hash:** 32 bytes

**Formato armazenado:**
```
{salt-base64}.{hash-base64}
```

**Exemplo:**
```
nO8qK4vR2tM=.8bYVJK9xPm4WnT2L5fZeR1Qp3Xc7Sv6Hu0Gi=
```

## 📚 Documentação Adicional

- [`README.md`](../README.md) - Visão geral completa
- [`ARQUITETURA.md`](ARQUITETURA.md) - Detalhes técnicos
- [`COMPARACAO_MONOLITO.md`](COMPARACAO_MONOLITO.md) - Diferenças com o monolito
- Swagger UI - https://localhost:5001/swagger

## 💡 Dicas

### Ver Logs Detalhados
Edite `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Hot Reload
Use `dotnet watch` para reload automático:
```powershell
dotnet watch run
```

### Verificar Health
A API responde na raiz:
```bash
curl https://localhost:5001/
```

## ❓ Troubleshooting

**Erro: "Port 5001 already in use"**
```powershell
# Mude a porta em launchSettings.json ou:
dotnet run --urls "https://localhost:5002"
```

**Erro: "No such table: Usuarios"**
```powershell
# Se usando SQL Server, execute:
dotnet ef database update
```

**Erro: "Unable to resolve service for type IUsuarioRepository"**
```powershell
# Verifique se Program.cs tem:
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
```

## 🎯 Próximos Endpoints (TODO)

- [ ] `POST /api/auth/login` - Autenticação JWT
- [ ] `POST /api/auth/refresh` - Refresh token
- [ ] `POST /api/auth/logout` - Invalidar token
- [ ] `GET /api/usuarios/me` - Perfil do usuário logado
- [ ] `POST /api/usuarios/{id}/reenviar-ativacao` - Reenviar email
