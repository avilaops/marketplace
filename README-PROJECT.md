# MarketplaceBuilder SaaS

Sistema SaaS para criação de marketplaces/lojas online em poucos cliques. Permite que clientes criem, configurem e publiquem suas próprias lojas online com suporte a multi-tenancy, pagamentos, catálogo e gestão completa.

## 🎯 Status do Projeto

- ✅ **FASE 0 - Bootstrap** (PR #1) - CONCLUÍDO
- 🔄 **FASE 1 - Multi-tenant** (PR #2) - PRÓXIMO
- ⏳ **FASE 2 - Wizard** (PR #3)
- ⏳ **FASE 3 - Catálogo** (PR #4)
- ⏳ **FASE 4 - Checkout** (PR #5)
- ⏳ **FASE 5 - Billing** (PR #6)
- ⏳ **FASE 6 - Observabilidade** (PR #7)

## 🏗️ Arquitetura

### Stack Tecnológica

- **Backend**: ASP.NET Core 8 (Minimal APIs)
- **Database**: PostgreSQL 15 + EF Core
- **Cache**: Redis 7
- **Storage**: MinIO (dev) / S3 (prod)
- **Multi-tenant**: Finbuckle.MultiTenant
- **Background Jobs**: Hangfire
- **Auth**: ASP.NET Core Identity + JWT
- **Payments**: Stripe
- **Observability**: OpenTelemetry + Serilog + Sentry

### Estrutura

```
/src
  /MarketplaceBuilder.Api          - Web API
  /MarketplaceBuilder.Core         - Domain
  /MarketplaceBuilder.Infrastructure - Data Access
  /MarketplaceBuilder.Worker       - Background Jobs
/tests
  /MarketplaceBuilder.Tests.Unit
  /MarketplaceBuilder.Tests.Integration
/infra
  /docker-compose.yml              - Dev infrastructure
/docs
  /decisions                       - ADRs
  /runbooks                        - Ops procedures
```

## 🚀 Quick Start

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Instalação

```bash
# 1. Clonar repositório
git clone https://github.com/avilaops/manager.git
cd manager

# 2. Copiar variáveis de ambiente
cp .env.example .env

# 3. Iniciar infraestrutura
cd infra
docker compose up -d

# 4. Executar API
cd ../src/MarketplaceBuilder.Api
dotnet run
```

### Verificar Instalação

```bash
# Health check
curl http://localhost:5000/health
# Resposta esperada: Healthy

# Info da API
curl http://localhost:5000/
```

### Serviços Disponíveis

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| API | http://localhost:5000 | - |
| PostgreSQL | localhost:5432 | user: `marketplace` / pass: `marketplace_dev_password` |
| Redis | localhost:6379 | pass: `redis_dev_password` |
| MinIO Console | http://localhost:9001 | user: `minioadmin` / pass: `minioadmin123` |
| Seq (Logs) | http://localhost:5341 | - |

## 📖 Documentação

- [Runbook - Desenvolvimento Local](docs/runbooks/local-development.md)
- [ADR 0001 - Stack e Arquitetura](docs/decisions/0001-stack-e-arquitetura.md)

## 🧪 Testes

```bash
# Todos os testes
dotnet test src/MarketplaceBuilder.slnx

# Apenas unit tests
dotnet test tests/MarketplaceBuilder.Tests.Unit/MarketplaceBuilder.Tests.Unit.csproj

# Apenas integration tests
dotnet test tests/MarketplaceBuilder.Tests.Integration/MarketplaceBuilder.Tests.Integration.csproj
```

## 📋 Comandos Úteis

```bash
# Build
dotnet build src/MarketplaceBuilder.slnx

# Limpar
dotnet clean src/MarketplaceBuilder.slnx

# Restaurar dependências
dotnet restore src/MarketplaceBuilder.slnx

# Rodar API em watch mode
cd src/MarketplaceBuilder.Api
dotnet watch run
```

## 🐳 Docker Compose

```bash
cd infra

# Iniciar todos os serviços
docker compose up -d

# Ver logs
docker compose logs -f

# Parar serviços
docker compose down

# Parar e remover volumes (ATENÇÃO: apaga dados)
docker compose down -v
```

## 🏛️ Decisões Arquiteturais

Todas as decisões arquiteturais estão documentadas em [ADRs](docs/decisions/) seguindo o padrão:
- **0001** - Stack e Arquitetura Base
- (Próximos ADRs serão adicionados conforme necessário)

## 🔒 Segurança

- ⚠️ **NÃO** commitar arquivo `.env` (já está no .gitignore)
- 🔑 Usar `.env.example` como template
- 🔐 Trocar senhas e secrets em produção
- 🛡️ JWT secrets devem ter no mínimo 32 caracteres

## 📦 Próximas Features (Roadmap)

### FASE 1 - Multi-tenant (PR #2)
- [ ] Tabelas: Tenants, Domains, Users
- [ ] Resolver tenant por Host header
- [ ] Interceptor EF Core para isolamento
- [ ] Auditoria básica

### FASE 2 - Wizard (PR #3)
- [ ] Fluxo de criação de loja
- [ ] Configuração de tema
- [ ] Publicação de storefront

### FASE 3 - Catálogo (PR #4)
- [ ] CRUD Produtos
- [ ] Upload de imagens
- [ ] Categorias

### FASE 4 - Checkout (PR #5)
- [ ] Carrinho
- [ ] Integração Stripe
- [ ] Webhooks idempotentes

### FASE 5 - Billing SaaS (PR #6)
- [ ] Planos e limites
- [ ] Stripe Billing
- [ ] Bloqueio por inadimplência

### FASE 6 - Observabilidade (PR #7)
- [ ] OpenTelemetry tracing
- [ ] Logs estruturados com tenant_id
- [ ] Rate limiting
- [ ] RBAC + 2FA

## 🤝 Contribuindo

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📝 License

[MIT](LICENSE)

## 👥 Time

- Tech Lead: [@avilaops](https://github.com/avilaops)

---

**Última atualização**: 2025-01-14 (FASE 0 - Bootstrap concluída)
