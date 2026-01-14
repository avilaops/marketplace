# MarketplaceBuilder - Runbook Local

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)

## Setup Inicial

### 1. Clonar o Repositório

```bash
git clone https://github.com/avilaops/manager.git
cd manager
```

### 2. Configurar Variáveis de Ambiente

```bash
# Copiar o arquivo de exemplo
cp .env.example .env

# Editar .env com suas configurações locais (opcional para dev)
```

### 3. Iniciar Infraestrutura (Docker Compose)

```bash
cd infra
docker compose up -d

# Verificar se os containers estão rodando
docker compose ps

# Ver logs (opcional)
docker compose logs -f
```

Serviços disponíveis:
- **PostgreSQL**: `localhost:5432`
  - User: `marketplace`
  - Password: `marketplace_dev_password`
  - Database: `marketplacebuilder`
- **Redis**: `localhost:6379`
  - Password: `redis_dev_password`
- **MinIO**: 
  - API: `localhost:9000`
  - Console: `http://localhost:9001`
  - User: `minioadmin`
  - Password: `minioadmin123`
- **Seq**: `http://localhost:5341` (logs estruturados)

### 4. Restaurar Dependências

```bash
cd ..
dotnet restore src/MarketplaceBuilder.slnx
```

### 5. Executar a API

```bash
cd src/MarketplaceBuilder.Api
dotnet run
```

A API estará disponível em:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### 6. Verificar Health Check

```bash
# Windows PowerShell
Invoke-WebRequest -Uri http://localhost:5000/health

# Linux/Mac
curl http://localhost:5000/health
```

Resposta esperada: `Healthy`

## Comandos Úteis

### Build

```bash
# Build completo
dotnet build src/MarketplaceBuilder.slnx

# Build em modo Release
dotnet build src/MarketplaceBuilder.slnx --configuration Release
```

### Testes

```bash
# Todos os testes
dotnet test src/MarketplaceBuilder.slnx

# Apenas testes unitários
dotnet test tests/MarketplaceBuilder.Tests.Unit/MarketplaceBuilder.Tests.Unit.csproj

# Apenas testes de integração
dotnet test tests/MarketplaceBuilder.Tests.Integration/MarketplaceBuilder.Tests.Integration.csproj

# Com cobertura (requer coverlet)
dotnet test src/MarketplaceBuilder.slnx /p:CollectCoverage=true
```

### Docker Compose

```bash
cd infra

# Iniciar todos os serviços
docker compose up -d

# Parar todos os serviços
docker compose down

# Parar e remover volumes (CUIDADO: apaga dados)
docker compose down -v

# Ver logs
docker compose logs -f [service_name]

# Reiniciar um serviço específico
docker compose restart postgres
```

### Limpar Build

```bash
# Limpar artefatos de build
dotnet clean src/MarketplaceBuilder.slnx

# Limpar e rebuild
dotnet clean src/MarketplaceBuilder.slnx && dotnet build src/MarketplaceBuilder.slnx
```

## Troubleshooting

### API não inicia

1. Verificar se as portas 5000/5001 não estão em uso:
   ```bash
   netstat -an | findstr "5000"
   ```

2. Verificar logs da API:
   ```bash
   cd src/MarketplaceBuilder.Api
   dotnet run --verbosity detailed
   ```

### Erro de conexão com PostgreSQL

1. Verificar se o container está rodando:
   ```bash
   docker ps | findstr postgres
   ```

2. Testar conexão manual:
   ```bash
   docker exec -it marketplace-postgres psql -U marketplace -d marketplacebuilder
   ```

3. Verificar connection string em `appsettings.Development.json`

### Erro de conexão com Redis

1. Verificar se o container está rodando:
   ```bash
   docker ps | findstr redis
   ```

2. Testar conexão manual:
   ```bash
   docker exec -it marketplace-redis redis-cli -a redis_dev_password ping
   ```

### Limpar Estado Completo

```bash
# Parar todos os containers e remover volumes
cd infra
docker compose down -v

# Limpar build
cd ..
dotnet clean src/MarketplaceBuilder.slnx

# Remover pasta bin/obj
Get-ChildItem -Recurse -Directory -Filter "bin" | Remove-Item -Recurse -Force
Get-ChildItem -Recurse -Directory -Filter "obj" | Remove-Item -Recurse -Force

# Reiniciar
cd infra
docker compose up -d
cd ../src/MarketplaceBuilder.Api
dotnet run
```

## Estrutura do Projeto

```
/src
  /MarketplaceBuilder.Api          - Web API (Minimal APIs)
  /MarketplaceBuilder.Core         - Domain models, interfaces
  /MarketplaceBuilder.Infrastructure - EF Core, repositories
  /MarketplaceBuilder.Worker       - Background jobs (Hangfire)
/tests
  /MarketplaceBuilder.Tests.Unit
  /MarketplaceBuilder.Tests.Integration
/infra
  /docker-compose.yml              - Infraestrutura local
/docs
  /decisions                       - ADRs (Architecture Decision Records)
  /runbooks                        - Procedimentos operacionais
```

## Próximos Passos

Após validar o setup:
1. ✅ `docker compose up` funciona
2. ✅ `dotnet run` inicia a API
3. ✅ `/health` responde `Healthy`
4. ✅ Testes passam

**Prosseguir para FASE 1**: Multi-tenant por domínio
