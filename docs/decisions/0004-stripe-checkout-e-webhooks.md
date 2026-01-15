# ADR 0004: Stripe Checkout e Webhooks para Pagamento

**Status:** Aceito  
**Data:** 2025-01-14  
**Decisor:** Tech Lead  
**Relacionado:** ADR 0001 (Stack), ADR 0002 (UI), ADR 0003 (Catálogo)

## Contexto

Para a Fase 4, precisamos implementar checkout end-to-end com pagamento real via Stripe. Decisões críticas:
1. Qual API do Stripe usar (Checkout vs PaymentIntent direto)
2. Como armazenar carrinho de compras
3. Como lidar com webhooks (assinatura, idempotência)
4. Multi-tenancy: uma conta Stripe ou várias

## Decisões

### A) Stripe Mode: Checkout Sessions

**Escolha:** Stripe Checkout Sessions (hosted checkout page)

**Justificativa:**
- **Simplicidade**: Stripe gerencia UI do pagamento (PCI compliance automático)
- **Menos código**: não precisamos construir formulário de cartão
- **Conversão**: UI otimizada do Stripe (testada por milhões)
- **Métodos**: suporta cartão, Apple Pay, Google Pay out-of-the-box
- **Mobile-friendly**: responsivo por padrão

**Flow:**
```
1. Cliente clica "Pagar" no carrinho
2. Backend cria Checkout Session (line_items + metadata)
3. Backend retorna checkoutUrl
4. Frontend redireciona para checkout.stripe.com
5. Cliente paga no Stripe
6. Stripe redireciona para success_url
7. Webhook confirma pagamento (async)
```

**Trade-offs:**
- ❌ Menos controle sobre UI do checkout
- ✅ Menos risco de segurança (PCI DSS Level 1 certified)
- ✅ Implementação rápida (~2-3 horas vs 1-2 dias)

**Alternativa rejeitada:**
- **PaymentIntent + Elements**: requer UI custom, mais código, mais risco PCI

### B) Multi-Tenancy: Single Stripe Account (Platform Mode)

**Escolha:** Uma conta Stripe para toda a plataforma (por enquanto)

**Justificativa:**
- **MVP**: acelera implementação (sem Stripe Connect)
- **Simplicidade**: um webhook endpoint, um secret key
- **Reconciliação**: metadata do Checkout Session contém tenant_id
- **Futuro**: migrar para Stripe Connect quando necessário (Fase 6+)

**Metadata em cada Checkout Session:**
```json
{
  "tenant_id": "guid-do-tenant",
  "order_id": "guid-do-pedido",
  "store_name": "Nome da Loja"
}
```

**Como distribuir pagamentos futuramente:**
- **Fase 6**: Implementar Stripe Connect (cada tenant = connected account)
- **Split**: plataforma fica com fee, tenant recebe net amount
- **Payout**: automático via Transfer API

**Alternativa rejeitada:**
- **Stripe Connect agora**: overhead de onboarding, KYC, muito complexo para MVP

### C) Cart Storage: Cookie-Based (Client-Side)

**Escolha:** Armazenar carrinho em cookie JSON no browser

**Estrutura:**
```json
{
  "items": [
    { "variantId": "guid", "quantity": 2 },
    { "variantId": "guid", "quantity": 1 }
  ]
}
```

**Justificativa:**
- **Simplicidade**: sem necessidade de login/sessão server-side
- **Performance**: não sobrecarrega DB/Redis
- **Privacy**: não armazenamos dados do cliente antes do checkout
- **Tenant-scoped**: cookie só vale para o domain do tenant

**Validação:**
- ⚠️ **Nunca confiar no cliente**: backend recalcula totais server-side
- ⚠️ **Verificar tenant**: variantId deve pertencer ao tenant da request
- ⚠️ **Verificar estoque**: antes de criar Checkout Session

**Cookie config:**
```csharp
HttpOnly = false  // precisa ser acessível por JS
Secure = true     // apenas HTTPS em prod
SameSite = Lax    // CSRF protection
MaxAge = 7 days   // carrinho expira em 1 semana
```

**Alternativas rejeitadas:**
- **Redis/DB**: overhead desnecessário, requer login
- **LocalStorage**: mesma coisa que cookie, mas menos seguro (XSS)

### D) URLs de Success/Cancel: Tenant-Aware

**Escolha:** Gerar URLs dinâmicas baseadas no Host do tenant

**Pattern (DEV):**
```
success_url: http://{tenantHost}.localtest.me:5003/checkout/success?orderId={orderId}
cancel_url:  http://{tenantHost}.localtest.me:5003/cart
```

**Pattern (PROD):**
```
success_url: https://{custom-domain}/checkout/success?orderId={orderId}
cancel_url:  https://{custom-domain}/cart
```

**Implementação:**
```csharp
var tenantHost = HttpContext.Request.Host.Host; // sem porta
var successUrl = $"http://{tenantHost}:{storefrontPort}/checkout/success?orderId={orderId}";
```

**Justificativa:**
- **Multi-tenant**: cada tenant volta pro seu próprio storefront
- **Isolamento**: não mistura tenants
- **UX**: cliente permanece no contexto da loja

**Importante:**
- ⚠️ Storefront API/5001 != Storefront/5003 (portas diferentes em dev)
- ⚠️ Resolver tenant por HttpContext.Request.Host.Host (ignore porta)

## Webhook: Assinatura + Idempotência

### Validação de Assinatura

**Obrigatório:** Verificar header `Stripe-Signature`

```csharp
var json = await new StreamReader(Request.Body).ReadToEndAsync();
var stripeEvent = EventUtility.ConstructEvent(
    json,
    Request.Headers["Stripe-Signature"],
    webhookSecret
);
```

**Por quê:**
- **Segurança**: evita webhooks falsos (replay attacks)
- **Integridade**: garante que payload não foi alterado
- **Autenticação**: só Stripe pode gerar assinatura válida

### Idempotência

**Problema:** Stripe pode enviar mesmo evento múltiplas vezes (retry, network issue)

**Solução:** Tabela `StripeWebhookEvents` com `stripe_event_id` unique

**Flow:**
```csharp
1. Receber webhook
2. Validar assinatura
3. Checar se stripe_event_id já existe no DB
4. SE existe: retornar 200 OK (não processar)
5. SE não: processar + salvar event_id
6. Sempre retornar 200 (se assinatura válida)
```

**Eventos suportados (mínimo):**
- `checkout.session.completed` → Order.status = Paid
- `payment_intent.payment_failed` → Order.status = Failed
- `charge.refunded` → Order.status = Refunded

## Order Snapshots

**Decisão:** Armazenar snapshot dos dados do produto no OrderItem

**Por quê:**
- **Histórico**: produto pode mudar preço/nome depois
- **Auditoria**: garantir que pedido reflete o momento da compra
- **Imutabilidade**: pedido não muda se produto for deletado

**Campos snapshot:**
```csharp
OrderItem {
  title_snapshot: string       // "iPhone 15 Pro"
  sku_snapshot: string?        // "APPLE-IP15-128-BLACK"
  unit_price_amount: long      // 129900 (€1299.00 no momento)
  quantity: int                // 2
  currency: string             // "EUR"
  line_total_amount: long      // 259800 (unit_price * quantity)
}
```

## Consequências

### Positivo
- **Rápido de implementar**: Checkout Sessions pronto em horas
- **Seguro**: PCI compliance via Stripe, não tocamos em dados de cartão
- **Escalável**: cookie-based cart não sobrecarrega servidor
- **Idempotente**: webhooks duplicados não quebram sistema
- **Tenant-safe**: URLs dinâmicas isolam tenants

### Negativo
- **Menos controle UI**: checkout é no Stripe (aceito para MVP)
- **Single account**: platform não recebe fee ainda (futuro: Connect)
- **Cookie limits**: carrinho grande pode exceder 4KB (mitigar: máx 20 items)

### Riscos Mitigados
- **Webhook replay**: assinatura + idempotência
- **Price manipulation**: backend recalcula totais (nunca confia no cliente)
- **Tenant leakage**: metadata + validação server-side
- **Stock race condition**: verificar estoque antes de criar Checkout Session

## Validação

Aceite da Fase 4:
- ✅ Carrinho adiciona items via cookie
- ✅ Checkout cria Order Pending com totals server-side
- ✅ Webhook marca Order como Paid
- ✅ Idempotência: evento duplicado não processa 2x
- ✅ Multi-tenant: tenant A não vê pedidos de tenant B

## Futuras Melhorias (Fora de Escopo Fase 4)

- **Fase 5**: Stripe Connect para split payments
- **Fase 6**: 3D Secure (SCA) obrigatório Europa
- **Fase 7**: Assinaturas recorrentes
- **Fase 8**: Multi-currency (um Checkout Session por moeda)
- **Fase 9**: Abandoned cart recovery (email automation)
