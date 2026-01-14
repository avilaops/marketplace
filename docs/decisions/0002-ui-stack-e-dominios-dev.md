# ADR 0002: UI Stack e Domínios para Desenvolvimento

**Status:** Aceito  
**Data:** 2025-01-14  
**Decisor:** Tech Lead  
**Relacionado:** ADR 0001 (Stack e Arquitetura Base)

## Contexto

Para a Fase 2 (Wizard + Publicação), precisamos definir:
1. Qual framework UI usar para o painel Admin
2. Qual framework UI usar para o Storefront (vitrine pública)
3. Como gerenciar subdomínios em ambiente de desenvolvimento local

## Decisão

### A) Admin UI: Blazor Server

**Escolha:** Blazor Server (interactive server-side)

**Justificativa:**
- Consistência com ADR 0001 que já mencionou Blazor
- Ideal para painel administrativo: interatividade rica sem complexidade de SPA
- Não requer API REST completa para cada operação (pode usar serviços diretamente via DI)
- SignalR já está no runtime do ASP.NET Core
- Autenticação integrada: cookies + JWT para API calls
- Menor superfície de ataque (lógica no servidor)

**Trade-offs:**
- ❌ Requer conexão persistente (SignalR WebSocket)
- ✅ Desenvolvimento rápido, menos código boilerplate
- ✅ Validação e lógica no servidor (mais seguro)

### B) Storefront UI: Razor Pages

**Escolha:** Razor Pages (server-side rendering)

**Justificativa:**
- **SEO-friendly**: conteúdo renderizado no servidor, crucial para marketplaces
- **Performance**: sem overhead de SPA, carrega mais rápido (importante para conversão)
- **Simplicidade**: model-binding nativo, routing por convenção
- **Progressive Enhancement**: pode adicionar JS incrementalmente (HTMX, Alpine.js)
- **Cache-friendly**: output cache do ASP.NET Core 8 funciona out-of-the-box
- **Multi-tenant natural**: cada request resolve tenant, renderiza página

**Alternativas consideradas:**
- Blazor Server: overhead desnecessário para vitrine pública (muitos visitantes anônimos)
- MVC: Razor Pages é MVC simplificado, mais moderno
- Blazor WASM: payload grande, SEO complexo, latência inicial

### C) Base Domain para Desenvolvimento: localtest.me

**Escolha:** `localtest.me` como base domain padrão em dev

**Justificativa:**
- **Zero configuração**: `*.localtest.me` aponta para `127.0.0.1` via DNS público
- **Testes de subdomínios**: permite testar multi-tenancy localmente sem editar `/etc/hosts`
- **Consistência**: todos os devs têm o mesmo setup
- **Exemplo:** 
  - Admin: `admin.localtest.me:5002`
  - API: `api.localtest.me:5001`
  - Tenant "minhaloja": `minhaloja.localtest.me:5003`

**Configuração:**
```json
{
  "Platform": {
    "BaseDomain": "localtest.me",
    "AdminSubdomain": "admin",
    "ApiSubdomain": "api"
  }
}
```

**Em produção:** trocar para domínio real (`example.com`), subdomínios resolvem via DNS real.

### Portas Padrão (Development)

| Serviço      | Porta | URL Dev                        |
|--------------|-------|--------------------------------|
| API          | 5001  | https://api.localtest.me:5001  |
| Admin        | 5002  | https://admin.localtest.me:5002|
| Storefront   | 5003  | https://{tenant}.localtest.me:5003 |

## Consequências

### Positivo
- Stack coesa: tudo ASP.NET Core 8
- SEO do Storefront garantido (crítico para marketplaces)
- Dev experience simplificado (localtest.me)
- Admin seguro e rápido (Blazor Server)

### Negativo
- Blazor Server: escalar globalmente requer sticky sessions (mitigar com Redis backplane)
- Razor Pages: interatividade rica requer JS adicional (aceito, vitrine é majoritariamente leitura)

### Riscos Mitigados
- **Escala global do Admin:** poucos usuários simultâneos (admins de tenants), Blazor Server aceitável
- **Cache do Storefront:** Razor Pages + Output Cache resolve 90% das requests sem tocar DB
- **SEO:** Razor Pages entrega HTML pronto, crawlers amam

## Alternativas para Fase Futura

Se telemetria mostrar gargalos:
- **Admin:** migrar para Blazor WebAssembly + API REST completa
- **Storefront:** adicionar SSG (Static Site Generation) para páginas de produto via pré-render

## Validação

Aceite da Fase 2:
- ✅ Admin Wizard cria tenant via Blazor Server
- ✅ Storefront resolve `{tenant}.localtest.me` e exibe vitrine
- ✅ Devs conseguem rodar 3 apps simultaneamente sem editar hosts
