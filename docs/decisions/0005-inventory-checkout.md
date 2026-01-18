# ADR 0005: Estratégia de Decremento de Estoque no Checkout

**Status:** Aceito
**Data:** 2026-01-18
**Decisor:** Tech Lead
**Relacionado:** PR #5 - Estoque + Regras de Compra

## Contexto

No processo de checkout, precisamos decidir quando decrementar o estoque para evitar vendas acima do disponível, mantendo consistência e boa experiência do usuário.

## Opções Consideradas

### Opção A: Decrementar no Order Pending (Reserva Imediata)
- **Quando:** Ao criar Order com status Pending
- **Vantagens:**
  - Garante estoque disponível até o pagamento
  - Evita race conditions entre usuários
  - Simples de implementar
- **Desvantagens:**
  - Usuário pode abandonar checkout (estoque fica "preso")
  - Requer job de limpeza para liberar estoque de orders abandonados
  - Complexidade adicional de timeout

### Opção B: Decrementar no Order Paid (Via Webhook)
- **Quando:** Quando Stripe confirma pagamento (webhook checkout.session.completed)
- **Vantagens:**
  - Estoque só decrementa quando pagamento confirmado
  - Não há estoque "preso" em checkouts abandonados
  - Simples de implementar (sem jobs de limpeza)
- **Desvantagens:**
  - Possível race condition se múltiplos usuários tentarem comprar simultaneamente
  - Usuário pode ver "estoque disponível" mas perder a compra

## Decisão

**Escolhido:** Opção B - Decrementar no Order Paid

**Justificativa:**
- **Simplicidade:** Não requer jobs de limpeza ou timeout logic
- **Confiabilidade:** Só decrementa quando pagamento confirmado
- **Experiência:** Menos chance de "estoque preso" frustrando usuários
- **Mitigação de Race Condition:** Em cenários high-traffic, implementar optimistic locking

**Implementação:**
1. Validar estoque suficiente no checkout (sem decrementar)
2. Criar Order Pending
3. No webhook `checkout.session.completed`: decrementar estoque
4. No webhook `charge.refunded`: reverter estoque

## Consequências

### Positivo
- Checkout mais confiável (não perde vendas por estoque "preso")
- Menos complexidade operacional
- Boa experiência para merchants (estoque não some misteriosamente)

### Negativo
- Possível over-selling em cenários de alta concorrência
- Requer implementação de webhooks robusta

### Mitigação
- Monitorar taxas de over-selling
- Se necessário, implementar reserva temporária com timeout
- Alertas quando estoque ficar crítico

## Validação

Testes necessários:
- Checkout com estoque suficiente → sucesso
- Checkout com estoque insuficiente → 409 error
- Webhook payment success → estoque decrementado
- Webhook refund → estoque revertido