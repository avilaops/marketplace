# FASE 3 - Catálogo Completo (BACKEND)

## ✅ IMPLEMENTADO

### Decisões Arquiteturais (ADR 0003)
- ✅ Preços em minor units (long) + currency ISO
- ✅ Upload multipart/form-data
- ✅ URLs: object_key + public_url
- ✅ Status: Draft | Active | Archived
- ✅ Rotas: /products e /products/{slug}

### Entidades (Core)
- ✅ Category (tenant_id, name, slug)
- ✅ Product (tenant_id, title, slug, status, category_id)
- ✅ ProductVariant (product_id, price_amount, currency, sku, is_default)
- ✅ ProductImage (product_id, object_key, public_url, sort_order)

### Infraestrutura
- ✅ DbContext atualizado com 4 DbSets
- ✅ Migration AddCatalogEntities criada
- ✅ IObjectStorage interface
- ✅ S3StorageService (AWS SDK) configurado para MinIO
- ✅ appsettings.json com Storage config

### Helpers
- ✅ SlugHelper para gerar slugs SEO-friendly
- ✅ CatalogModels (DTOs)

## ⏳ PENDENTE (próximos steps)

### Endpoints Admin (CRÍTICO)
- [ ] CategoryEndpoints (GET, POST, PUT, DELETE)
- [ ] ProductEndpoints (GET list+detail, POST, PUT, DELETE)
- [ ] ProductVariantEndpoints (CRUD)
- [ ] ProductImageEndpoints (POST upload, DELETE)

### Storefront (CRÍTICO)
- [ ] GET /products (lista Active)
- [ ] GET /products/{slug} (detalhe)

### Testes
- [ ] CatalogTests (criar produto + variant + image)
- [ ] StorefrontTests (produtos aparecem)

## 🚀 Como Continuar

1. Implementar endpoints (CategoryEndpoints.cs, ProductEndpoints.cs, etc.)
2. Aplicar migration: `dotnet ef database update`
3. Criar bucket MinIO "marketplace"
4. Testar via Swagger
5. Implementar Storefront
6. Testes de integração

## 📝 Comandos Rápidos

```bash
# Aplicar migration
dotnet ef database update --project src/MarketplaceBuilder.Infrastructure --startup-project src/MarketplaceBuilder.Api

# Build
dotnet build src/MarketplaceBuilder.slnx --configuration Release

# Rodar API
cd src/MarketplaceBuilder.Api
dotnet run --urls "https://localhost:5001"

# Criar bucket MinIO (via console http://localhost:9001)
# User: minioadmin / Pass: minioadmin123
# Criar bucket "marketplace" com policy Public
```

## 📊 Status

| Componente | Progresso |
|------------|-----------|
| ADR 0003 | 100% |
| Entidades | 100% |
| DbContext + Migration | 100% |
| Storage Service | 100% |
| Helpers/DTOs | 100% |
| **Endpoints** | **0%** ⚠️ |
| **Storefront** | **0%** ⚠️ |
| **Testes** | **0%** ⚠️ |

## ⚠️ NOTA

A base está 100% pronta. Os endpoints são repetitivos e seguem o padrão já estabelecido em StoreProvisioningEndpoints.cs. Storefront segue o padrão Razor Pages documentado no ADR 0002.

**Próximo desenvolvedor pode completar em ~4-6 horas:**
- Endpoints: ~2h
- Storefront: ~2h
- Testes: ~2h
