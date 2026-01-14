# ADR 0003: Modelo de Catálogo e Storage de Imagens

**Status:** Aceito  
**Data:** 2025-01-14  
**Decisor:** Tech Lead  
**Relacionado:** ADR 0001 (Stack Base), ADR 0002 (UI Stack)

## Contexto

Para a Fase 3 (Catálogo), precisamos definir:
1. Como armazenar preços de produtos (precisão, moeda)
2. Como fazer upload de imagens de produtos
3. Como gerar e armazenar URLs públicas das imagens
4. Estrutura de rotas do storefront público

## Decisões

### A) Modelo de Preço: Minor Units (Integer)

**Escolha:** Armazenar preço como `long price_amount` + `string currency(3)`

**Justificativa:**
- **Precisão**: long (int64) evita problemas de arredondamento de float/decimal
- **Minor units**: armazenar centavos/cents (ex: €10.50 = 10050)
- **Compatibilidade**: Stripe, PayPal, bancos usam este padrão
- **Internacionalização**: currency ISO 4217 (EUR, USD, BRL, JPY)

**Exemplos:**
```csharp
// €10.50 EUR
price_amount = 1050
currency = "EUR"

// $25.99 USD
price_amount = 2599
currency = "USD"

// ¥1000 JPY (sem decimais)
price_amount = 1000
currency = "JPY"
```

**Formatação no frontend:**
```csharp
decimal displayPrice = price_amount / 100.0m; // 1050 -> 10.50
string formatted = displayPrice.ToString("C", new CultureInfo("pt-PT")); // "10,50 €"
```

**Alternativas rejeitadas:**
- `decimal(18,2)`: pode ter problemas de serialização JSON, menos portável
- `float/double`: perde precisão, inadequado para dinheiro

### B) Upload de Imagens: Multipart Form-Data

**Escolha:** Upload direto via `multipart/form-data` no endpoint Admin

**Justificativa:**
- **Simplicidade**: sem necessidade de presigned URLs, menos complexidade
- **Segurança**: upload apenas em endpoint protegido (Admin/Staff)
- **Tamanho**: adequado para imagens (< 10MB typical)
- **Validação**: servidor valida content-type, tamanho, dimensões antes de salvar

**Fluxo:**
```
1. Admin UI: <input type="file" /> + FormData
2. POST /api/admin/products/{productId}/images
3. API valida (MIME type, size)
4. Salva no S3/MinIO
5. Cria ProductImage com object_key + public_url
6. Retorna { imageId, publicUrl }
```

**Validações:**
- Content-Type: `image/jpeg`, `image/png`, `image/webp`
- Max size: 5MB
- (Opcional futuro: resize server-side via ImageSharp)

**Alternativas rejeitadas:**
- **Presigned URLs**: mais complexo, requer client-side AWS SDK ou lógica extra
- **Base64**: payload grande, ineficiente

### C) URLs de Imagens: Object Key + Public Base URL

**Escolha:** Salvar `object_key` (S3 path) + gerar `public_url` dinamicamente

**Estrutura:**
```
object_key: tenants/{tenantId}/products/{productId}/{uuid}.{ext}
public_url: {PublicBaseUrl}/{object_key}
```

**Exemplo (Dev - MinIO):**
```
object_key: tenants/a1b2c3/products/d4e5f6/7890abcd.jpg
public_url: http://localhost:9000/marketplace/tenants/a1b2c3/products/d4e5f6/7890abcd.jpg
```

**Exemplo (Prod - CloudFront):**
```
object_key: tenants/a1b2c3/products/d4e5f6/7890abcd.jpg
public_url: https://cdn.marketplace.com/marketplace/tenants/a1b2c3/products/d4e5f6/7890abcd.jpg
```

**Configuração:**
```json
{
  "Storage": {
    "Provider": "S3",
    "Endpoint": "http://localhost:9000",
    "Bucket": "marketplace",
    "Region": "us-east-1",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin123",
    "PublicBaseUrl": "http://localhost:9000/marketplace"
  }
}
```

**Vantagens:**
- **Flexibilidade**: trocar CDN sem alterar DB (só config)
- **Backup**: object_key permite reconstruir URL
- **Tenant isolation**: path inclui tenantId

**Alternativas rejeitadas:**
- Salvar apenas URL completa: inflexível se mudar CDN
- Salvar apenas key: precisa regenerar URL em toda query

### D) Rotas do Storefront: /products e /products/{slug}

**Escolha:** 
- Lista: `GET /products`
- Detalhe: `GET /products/{slug}`

**Justificativa:**
- **SEO-friendly**: URLs limpas, slug descritivo
- **Convenção**: padrão e-commerce (Shopify, WooCommerce)
- **Slug único por tenant**: índice `(tenant_id, slug)` garante unicidade

**Exemplos:**
```
https://minhaloja.localtest.me:5003/products
https://minhaloja.localtest.me:5003/products/camiseta-branca-m
```

**Geração de slug:**
```csharp
// "Camiseta Branca M" -> "camiseta-branca-m"
slug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-")
    .Trim('-');
```

**Validações:**
- 3-80 caracteres
- `[a-z0-9-]`
- Único por tenant

**Alternativas rejeitadas:**
- `/products/{id}` (GUID): não SEO-friendly
- `/p/{slug}`: menos semântico

## Status de Produto

**Enum:** `Draft | Active | Archived`

- **Draft**: criado mas não visível no storefront
- **Active**: visível e comprável no storefront
- **Archived**: não visível, mas mantido no histórico (não deletado)

## Consequências

### Positivo
- **Preço**: sem problemas de arredondamento, compatível com gateways de pagamento
- **Upload**: simples, seguro, adequado para Admin
- **URLs**: flexível para CDN, backup-friendly
- **SEO**: slugs amigáveis, bom ranqueamento

### Negativo
- **Upload direto**: pode ser lento para arquivos grandes (mitigar: limit 5MB)
- **Minor units**: requer conversão no frontend (aceitável, padrão da indústria)

### Riscos Mitigados
- **Precisão monetária**: long garante exatidão
- **Tenant isolation**: path S3 inclui tenantId, impossível vazamento
- **CDN switchability**: PublicBaseUrl configurável

## Validação

Aceite da Fase 3:
- ✅ Admin cria produto com preço €10.50 → salvo como 1050 + "EUR"
- ✅ Upload imagem → gera URL pública funcional
- ✅ Storefront `/products` lista produtos Active
- ✅ Storefront `/products/camiseta-branca` exibe detalhe com imagens

## Futuras Melhorias (Fora de Escopo Fase 3)

- **Resize images**: ImageSharp ou Lambda@Edge para thumbs
- **Presigned URLs**: para uploads grandes (>10MB)
- **WebP conversion**: otimização automática
- **Image CDN**: Cloudinary/Imgix para transformations on-the-fly
