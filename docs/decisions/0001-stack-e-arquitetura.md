# ADR 0001: Stack e Arquitetura Base

**Status:** Aceito  
**Data:** 2025-01-14  
**Decisor:** Tech Lead

## Contexto

Precisamos definir a stack tecnológica e padrões arquiteturais para o SaaS "Marketplace Builder", que deve suportar 20.000+ tenants globalmente com alta disponibilidade, segurança e observabilidade.

## Decisão

### Backend Stack
- **Framework:** ASP.NET Core 8 Web API
- **Estilo API:** Minimal APIs (escolha por simplicidade, performance e menos boilerplate)
- **Auth:** ASP.NET Core Identity + JWT (Admin/Staff) + Cookies para Storefront
- **Banco:** PostgreSQL 15+ com EF Core (migrations)
- **Multi-tenant:** Finbuckle.MultiTenant com resolução por Host header + tenant_id em todas as tabelas
- **Cache:** Redis (StackExchange.Redis) para sessões, config e rate limit
- **Rate Limiting:** ASP.NET Core Rate Limiting (built-in desde .NET 7)
- **Background Jobs:** Hangfire com PostgreSQL storage (mais maduro no ecossistema .NET, UI embutida)
- **Pagamentos:** Stripe (stripe.net SDK) com webhooks idempotentes
- **Storage:** S3 compatível (MinIO local dev, AWS S3 prod) + CDN (Cloudflare/AWS CloudFront)
- **Observabilidade:** OpenTelemetry + Serilog (structured logging JSON) + Sentry para error tracking

### Estrutura de Projetos
```
/src
  /MarketplaceBuilder.Api          - Web API (minimal APIs)
  /MarketplaceBuilder.Worker       - Background jobs (Hangfire)
  /MarketplaceBuilder.Core         - Domain entities, interfaces
  /MarketplaceBuilder.Infrastructure - EF Core, repositories, external services
  /MarketplaceBuilder.Storefront   - Blazor Server (decisão: interatividade sem JS complexo)
  /MarketplaceBuilder.Admin        - Blazor Server (consistência com Storefront)
/infra
  /docker                          - docker-compose.yml para dev
/docs
  /decisions                       - ADRs
  /runbooks                        - Procedimentos operacionais
/tests
  /MarketplaceBuilder.Tests.Unit
  /MarketplaceBuilder.Tests.Integration
```

### Infra Dev (Docker Compose)
- PostgreSQL 15
- Redis 7
- MinIO (S3-compatible)
- Seq (opcional, para visualizar logs estruturados)

### Padrões de Código
- **Isolamento Multi-tenant:** Interceptor EF Core que injeta `TenantId` automaticamente em queries
- **Repository Pattern:** Abstração para acesso a dados
- **CQRS Leve:** Separação de comandos/queries onde faz sentido (não full Event Sourcing no MVP)
- **Validação:** FluentValidation
- **Mapeamento:** Mapster (mais performático que AutoMapper)

## Consequências

### Positivo
- Stack moderna e suportada por longo prazo (.NET 8 LTS até nov/2026)
- Performance excelente (Minimal APIs + hot path otimizado)
- Ecossistema maduro para multi-tenancy (Finbuckle)
- Hangfire UI facilita debug de jobs
- OpenTelemetry é padrão da indústria

### Negativo
- Blazor Server requer SignalR (conexão persistente), pode ser desafio em escala global
  - Mitigação: considerar Blazor WebAssembly ou MVC/Razor para Storefront em fase futura
- EF Core pode ter overhead; monitorar queries N+1
- Hangfire no mesmo banco pode competir por conexões (monitorar e separar se necessário)

## Alternativas Consideradas
1. **Controllers vs Minimal APIs:** Minimal APIs escolhidas por redução de boilerplate e performance
2. **Hangfire vs MassTransit:** Hangfire escolhido por simplicidade e UI integrada
3. **Blazor vs MVC/Razor:** Blazor Server para MVP; reavaliar em produção baseado em telemetria
