Você é um Arquiteto de Software corporativo e um Tech Lead full-stack. Projete e detalhe (com nível de implementação) um SaaS “Marketplace Builder” que permite ao cliente criar o próprio marketplace/loja online em poucos cliques. O sistema deve suportar 20.000+ clientes (tenants) globalmente, com alta disponibilidade, segurança e observabilidade.

OBJETIVO DO PRODUTO
- Entregar um “site de criação de marketplace” (plataforma SaaS) onde:
  1) O cliente cria conta
  2) Escolhe template/tema
  3) Define nome, nicho, moeda, idioma, país
  4) Conecta domínio próprio ou usa subdomínio
  5) Cadastra produtos (manual e importação)
  6) Configura pagamentos, frete, impostos
  7) Publica o marketplace e começa a vender
- “Poucos cliques” = fluxo guiado (wizard) + defaults inteligentes + validação automática.

ENTIDADES E PAPÉIS
- Super Admin (plataforma): gerencia tenants, billing, templates, suporte, auditoria.
- Tenant Admin (cliente): configura a loja, usuários internos, permissões, integrações.
- Staff do Tenant: catálogo, pedidos, atendimento.
- Comprador final: navega, compra, acompanha pedido.

REQUISITOS FUNCIONAIS (MVP + Evolução)
1) Onboarding & Wizard (essencial)
- Cadastro/login (email+senha, magic link, OAuth opcional).
- Wizard em etapas:
  - Identidade (nome/branding, logo, cores)
  - Template/tema (pré-visualização)
  - Catálogo (criar produto, variações, fotos)
  - Pagamentos (Stripe/Mercado Pago/Adyen; começar com Stripe)
  - Entrega (faixas, transportadoras, pickup)
  - Domínio (subdomínio imediato + domínio próprio com verificação DNS)
  - Publicar (toggle “live”)
- Geração instantânea do storefront (sem “deploy manual”).

2) Catálogo
- Produtos: título, descrição, SEO, tags, categoria, preço, promo, estoque, variações (tamanho/cor), SKUs.
- Mídia: upload de imagens (resize), limites e CDN.
- Importação: CSV/Excel (MVP) + integrações futuras.
- Coleções/categorias e filtros.

3) Checkout & Pedidos
- Carrinho, cupons, cálculo de frete, taxas/impostos por região.
- Pagamento externo PCI (ex.: Stripe Checkout / Payment Intents).
- Pedido: status (criado/pago/enviado/entregue/cancelado/refund).
- Notificações: email e WhatsApp (via provedor).

4) Painel do Cliente (Tenant Admin)
- Dashboard: vendas, pedidos, conversão, produtos.
- Gestão de pedidos: packing slip, tracking, reembolso.
- Usuários e permissões (RBAC).
- Configurações: moedas/idiomas, políticas, páginas institucionais (CMS simples).
- Integrações: pagamentos, email, analytics.

5) Storefront (site final do cliente)
- Página inicial, busca, vitrine, produto, carrinho, checkout, conta do comprador.
- SEO, sitemap, robots, URLs amigáveis.
- Multi-idioma e multi-moeda (começar com i18n + moeda base).
- Performance: TTFB baixo via CDN/cache.

6) Billing da Plataforma (SaaS)
- Planos, limites (produtos, pedidos/mês, usuários).
- Cobrança recorrente (Stripe Billing).
- Trial, upgrade/downgrade, inadimplência, bloqueio gracioso.

7) Suporte e Auditoria
- Logs de auditoria por tenant (quem fez o quê).
- Central de ajuda + tickets (MVP simples).
- Exportação de dados (GDPR).

REQUISITOS NÃO-FUNCIONAIS (OBRIGATÓRIOS)
- Multi-tenant seguro: isolamento lógico forte (tenant_id + RLS) e opção futura de “DB dedicado” para enterprise.
- Disponibilidade: 99,9%+ (alvo).
- Escalabilidade: suportar 20.000 tenants; picos de tráfego e campanhas.
- Latência global: CDN/edge para conteúdo estático e cache.
- Segurança: criptografia em trânsito e repouso; segredos em vault; RBAC; rate limiting; WAF; proteção DDoS; auditoria.
- Observabilidade: métricas, logs estruturados, tracing, alertas.
- Backups e DR: RPO/RTO definidos.
- Conformidade: GDPR; PCI via provedor de pagamento (não armazenar dados de cartão).

ARQUITETURA RECOMENDADA (SEPARAR CONTROL PLANE vs DATA PLANE)
A) Control Plane (plataforma SaaS)
- Autenticação do tenant admin
- Billing
- Templates/temas
- Configurações de loja
- Provisionamento de domínio
- Gestão de limites e features flags

B) Data Plane (runtime do storefront + APIs de comércio)
- APIs: catálogo, carrinho, checkout, pedidos
- Storefront web (multi-tenant)
- Webhooks de pagamento
- Workers de background (emails, sync, indexação, imagens)

DECISÃO DE MULTI-TENANCY (EXIGÊNCIA)
- Storefront multi-tenant por host:
  - {tenant}.sua-plataforma.com e domínio próprio (CNAME/ALIAS).
  - Resolução de tenant por Host header -> cache -> DB.
- Banco:
  - Postgres principal com tabela por domínio e tenant_id.
  - Row-Level Security (RLS) para garantir isolamento.
  - Particionamento por tenant_id quando necessário.
  - Read replicas para escala de leitura.
- Cache:
  - Redis para sessões, rate limit, caching de config e carrinho.
- Busca:
  - OpenSearch/Elasticsearch (ou Postgres full-text no MVP) com index por tenant.
- Arquivos:
  - S3 compatível + CDN (Cloudflare R2 + CDN ou AWS S3+CloudFront).
- Filas:
  - Kafka / RabbitMQ / SQS (escolher 1) para eventos e jobs.
- Eventos:
  - “OrderCreated”, “PaymentConfirmed”, “ProductUpdated”, etc.

STACK DE TECNOLOGIA (ESCOLHA UMA LINHA E PADRONIZE)
Opção 1 (rápida e mainstream):
- Front/storefront: Next.js (App Router) + TypeScript + Tailwind
- Admin (painel): Next.js + component library (ex.: Radix/UI)
- Backend APIs: NestJS (TypeScript) ou Fastify + Zod
- ORM: Prisma (ou Drizzle) + Postgres
- Auth: Auth.js / Keycloak (se enterprise) / Cognito (se AWS)
- Payments: Stripe (Checkout + Billing + Webhooks)
- Cache/Queue: Redis + BullMQ (MVP) / ou Redis+RabbitMQ
- Search: OpenSearch (V1) / Postgres FTS (MVP)
- Observabilidade: OpenTelemetry + Prometheus + Grafana + Loki + Tempo + Sentry

Opção 2 (alto desempenho e “core” sólido):
- Backend: Rust (axum) + SQLx + Postgres + Redis
- Workers: Rust + queues (SQS/Rabbit)
- Front/storefront: Next.js (TS)
- Observabilidade igual

INFRA PARA 20.000+ CLIENTES (GLOBAL)
- Cloud: AWS ou GCP ou Azure (escolher 1). Exemplo AWS:
  - EKS (Kubernetes) para serviços (API, storefront SSR, workers)
  - RDS Postgres (Multi-AZ) + read replicas
  - ElastiCache Redis
  - S3 + CloudFront (ou Cloudflare CDN)
  - SQS/SNS (ou MSK Kafka, se necessário)
  - WAF + Shield (DDoS)
  - Secrets Manager + KMS
- Multi-região:
  - Região primária (writes) + read replicas em regiões secundárias
  - CDN global para assets e páginas cacheáveis
  - Estratégia de failover (Route53/Cloudflare) e runbooks

DOMÍNIOS E SSL (OBRIGATÓRIO)
- Gestão automática de domínio:
  - Subdomínio instantâneo na plataforma
  - Domínio próprio via CNAME e validação
- SSL automático:
  - ACME/Let’s Encrypt ou Cloudflare SSL for SaaS
- Armazenar mapeamento: domain -> tenant_id.
- Propagação e status (verificando DNS e emitindo certificado).

SEGURANÇA (CHECKLIST)
- RBAC por tenant + permissões finas.
- 2FA para admins (TOTP).
- Rate limiting por IP + por tenant (Redis).
- WAF rules + bot protection.
- Auditoria imutável (append-only) de ações administrativas.
- Criptografia:
  - TLS 1.2+
  - At-rest (KMS)
- Webhooks assinados (Stripe) e idempotência.

PERFORMANCE (METAS)
- Storefront: TTFB baixo com cache por rota (quando possível).
- SSR apenas onde necessário; preferir SSG/ISR para páginas de catálogo.
- CDN agressiva para imagens (transformações) e estáticos.
- Indexação assíncrona para busca.

OBSERVABILIDADE (OBRIGATÓRIO)
- Logs estruturados (JSON) com correlation_id, tenant_id.
- Tracing distribuído (OpenTelemetry).
- Métricas:
  - RPS, latência p95/p99
  - erros 4xx/5xx
  - fila (lag), jobs falhando
  - DB: conexões, locks, slow queries
- Alertas: pagamento webhooks falhando, filas congestionadas, aumento de 5xx.

CI/CD + IaC
- Repositório mono-repo (ou multi) com:
  - apps/admin
  - apps/storefront
  - services/api
  - services/workers
  - infra/terraform
- GitHub Actions:
  - lint/test
  - build images
  - deploy (Helm)
  - migrations seguras
- Terraform para toda infra; Helm para releases.

BANCO DE DADOS (MODELO INICIAL)
- tenants(id, name, plan, status)
- domains(id, tenant_id, hostname, verified, ssl_status)
- users(id, tenant_id, role, email, 2fa)
- products(id, tenant_id, sku, title, desc, price, currency, stock, status)
- product_variants(id, product_id, tenant_id, attributes_json, price_delta, stock)
- categories(id, tenant_id, name, slug)
- orders(id, tenant_id, buyer_id, status, totals, payment_ref, created_at)
- order_items(id, order_id, tenant_id, product_id, qty, price)
- coupons(id, tenant_id, rules_json)
- audit_logs(id, tenant_id, actor_id, action, payload_json, created_at)
- storefront_config(id, tenant_id, theme_id, branding_json, seo_json)

API (ENDPOINTS EXEMPLO)
- /api/tenants (admin)
- /api/products CRUD
- /api/orders CRUD
- /api/checkout (create session/intents)
- /api/webhooks/stripe
- /api/storefront/config (por host)
- /api/search (por tenant)

CRITÉRIOS DE ACEITE (OBRIGATÓRIO)
- Criar tenant e publicar storefront em < 5 minutos com wizard.
- Subdomínio funcional imediatamente; domínio próprio com status e SSL automático.
- Checkout real com Stripe (sandbox) + webhook idempotente.
- Isolamento multi-tenant validado (nenhum dado vaza entre tenants).
- Observabilidade ativa (logs + métricas + tracing).
- Infra com deploy automatizado (CI/CD) e IaC.

ENTREGÁVEIS QUE VOCÊ DEVE GERAR AGORA
1) Um blueprint completo (arquitetura, componentes, fluxos).
2) Um diagrama textual (C4: Context/Container/Component).
3) Roadmap por fases: MVP (4-6 semanas), V1, V2.
4) Estrutura de repositório + principais pastas/arquivos.
5) Especificação de endpoints + esquema de banco inicial.
6) Plano de escala para 20.000 tenants (cache, DB, filas, CDN, multi-região).
7) Checklist de segurança e compliance.
8) Test strategy (unit, integration, e2e) e testes mínimos para publicar.

Observação: Priorize simplicidade no MVP, porém com decisões que não bloqueiem escala. Evite dependências desnecessárias e sempre defina padrões (naming, versionamento de API, migrations, idempotência, feature flags).
FIM.
