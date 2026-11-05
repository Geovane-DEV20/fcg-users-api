# 🔐 Configuração de Secrets e Variáveis de Ambiente

## 🎯 Objetivo

As configurações sensíveis (senhas, chaves, connection strings) **NÃO devem estar no código-fonte** por segurança.

Esta API suporta **3 métodos** de configuração:

---

## 📋 Métodos de Configuração

### 1️⃣ User Secrets (Desenvolvimento Local - RECOMENDADO)

**Vantagens:**
- ✅ Seguro (fora do repositório)
- ✅ Fácil de usar
- ✅ Padrão do .NET

**Como configurar:**

```bash
# Na pasta do projeto
cd "d:\ProjetoFiap\Mcrossserviços NEW FIAP\fcg-users-api"

# Inicializar user secrets (só precisa fazer 1x)
dotnet user-secrets init

# Configurar Database
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=FcgUsersDb;User Id=sa;Password=SuaSenha123!;TrustServerCertificate=True"

# Configurar JWT
dotnet user-secrets set "JwtSettings:SecretKey" "sua-chave-secreta-super-segura-com-no-minimo-32-caracteres"

# Configurar RabbitMQ
dotnet user-secrets set "RabbitMQ:HostName" "localhost"
dotnet user-secrets set "RabbitMQ:UserName" "guest"
dotnet user-secrets set "RabbitMQ:Password" "guest"
```

**Listar secrets configurados:**
```bash
dotnet user-secrets list
```

**Remover um secret:**
```bash
dotnet user-secrets remove "JwtSettings:SecretKey"
```

**Limpar todos:**
```bash
dotnet user-secrets clear
```

---

### 2️⃣ Variáveis de Ambiente (Produção)

**Quando usar:**
- ✅ Produção (Kubernetes, Docker, Azure, AWS)
- ✅ CI/CD pipelines
- ✅ Containers

**Formato:**
```bash
# Linux/Mac
export ConnectionStrings__DefaultConnection="Server=prod-db;Database=FcgUsersDb;..."
export JwtSettings__SecretKey="chave-super-segura-aqui"
export RabbitMQ__HostName="rabbitmq-cluster.internal"
export RabbitMQ__UserName="fcg-user"
export RabbitMQ__Password="senha-segura"

# Windows PowerShell
$env:ConnectionStrings__DefaultConnection="Server=prod-db;..."
$env:JwtSettings__SecretKey="chave-super-segura-aqui"
$env:RabbitMQ__HostName="rabbitmq-cluster.internal"
$env:RabbitMQ__UserName="fcg-user"
$env:RabbitMQ__Password="senha-segura"

# Windows CMD
set ConnectionStrings__DefaultConnection=Server=prod-db;...
set JwtSettings__SecretKey=chave-super-segura-aqui
```

**Docker:**
```yaml
version: '3.8'
services:
  fcg-users-api:
    image: fcg-users-api:latest
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=FcgUsersDb;User Id=sa;Password=${DB_PASSWORD}
      - JwtSettings__SecretKey=${JWT_SECRET}
      - RabbitMQ__HostName=rabbitmq
      - RabbitMQ__UserName=${RABBITMQ_USER}
      - RabbitMQ__Password=${RABBITMQ_PASSWORD}
```

**Kubernetes:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: fcg-users-secrets
type: Opaque
stringData:
  connection-string: "Server=..."
  jwt-secret: "chave-segura"
  rabbitmq-password: "senha"
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: fcg-users-api
spec:
  template:
    spec:
      containers:
      - name: api
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: fcg-users-secrets
              key: connection-string
        - name: JwtSettings__SecretKey
          valueFrom:
            secretKeyRef:
              name: fcg-users-secrets
              key: jwt-secret
        - name: RabbitMQ__Password
          valueFrom:
            secretKeyRef:
              name: fcg-users-secrets
              key: rabbitmq-password
```

---

### 3️⃣ Arquivo .env (Alternativa - NÃO RECOMENDADO)

**⚠️ Atenção:** Este método requer biblioteca adicional e o arquivo `.env` NUNCA deve ser commitado.

**Instalação:**
```bash
dotnet add package DotNetEnv
```

**Uso no Program.cs:**
```csharp
// No início do Program.cs
DotNetEnv.Env.Load();
```

**Criar .env:**
```bash
cp .env.example .env
# Editar .env com valores reais
```

**Adicionar ao .gitignore:**
```
.env
*.env
```

---

## 🔑 Gerando Chaves Seguras

### JWT Secret Key

**PowerShell:**
```powershell
# Gerar chave de 64 caracteres
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

**Bash/Linux:**
```bash
openssl rand -base64 48
```

**Online (use com cuidado):**
- https://www.grc.com/passwords.htm
- https://randomkeygen.com/

---

## 📊 Ordem de Precedência

O .NET carrega configurações nesta ordem (última sobrescreve):

1. appsettings.json
2. appsettings.{Environment}.json
3. User Secrets (apenas Development)
4. Variáveis de Ambiente
5. Command-line arguments

**Exemplo:**
```
appsettings.json         → JwtSettings:SecretKey = ""
appsettings.Development  → (não definido)
User Secrets             → JwtSettings:SecretKey = "dev-key-123"
Environment Variable     → JWT_SECRET = "prod-key-456"

Resultado: "prod-key-456" (env var vence)
```

---

## ✅ Checklist de Segurança

Antes de fazer deploy:

- [ ] ❌ Removi todas as senhas/chaves hardcoded do código
- [ ] ❌ `.env` está no `.gitignore`
- [ ] ❌ `appsettings.json` não tem valores sensíveis
- [ ] ✅ Secrets estão em User Secrets (dev) ou Env Vars (prod)
- [ ] ✅ Chave JWT tem pelo menos 32 caracteres
- [ ] ✅ Senhas de produção são complexas e únicas
- [ ] ✅ RabbitMQ não usa credenciais padrão (guest/guest) em prod

---

## 🚨 Valores Default para Desenvolvimento

Se você **não configurar** nenhum secret, a API vai:

1. ✅ **Usar InMemory Database** (não precisa SQL Server)
2. ❌ **Falhar ao iniciar** (JWT SecretKey obrigatória)
3. ❌ **Falhar ao publicar eventos** (RabbitMQ obrigatório)

**Configuração mínima para rodar local:**
```bash
dotnet user-secrets set "JwtSettings:SecretKey" "chave-de-desenvolvimento-min-32-chars-abcdefg123456"
dotnet user-secrets set "RabbitMQ:HostName" "localhost"
dotnet user-secrets set "RabbitMQ:UserName" "guest"
dotnet user-secrets set "RabbitMQ:Password" "guest"
```

---

## 📚 Mais Informações

- [Safe storage of app secrets - Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Environment Variables](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#environment-variables)

---

**Última atualização:** 21/10/2026  
**Versão da API:** 1.1.0
