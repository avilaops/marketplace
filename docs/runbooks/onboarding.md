# Onboarding Runbook

Este documento descreve os passos para executar o onboarding completo de uma nova loja no MarketplaceBuilder.

## Pré-requisitos

- Docker e Docker Compose instalados
- .NET 8.0 SDK
- Acesso ao repositório GitHub

## Passos para Executar Onboarding

### 1. Configurar Ambiente Local

```bash
# Clonar repositório
git clone https://github.com/avx/MarketplaceBuilder.git
cd MarketplaceBuilder

# Iniciar infraestrutura
docker compose up -d

# Aguardar containers ficarem prontos
sleep 10

# Verificar conexão com banco
docker exec marketplace-postgres psql -h localhost -U marketplace -d marketplacebuilder -c "SELECT 1;"

# Aplicar migrações
cd src/MarketplaceBuilder.Api
dotnet ef database update
```

### 2. Executar API

```bash
# No diretório src/MarketplaceBuilder.Api
dotnet run --environment Development
```

A API estará disponível em `https://localhost:5001`.

### 3. Executar Admin Interface

```bash
# Em outro terminal, no diretório src/MarketplaceBuilder.Admin
dotnet run --environment Development
```

A interface Admin estará disponível em `https://localhost:5000`.

### 4. Executar Onboarding

1. Acesse `https://localhost:5000` no navegador
2. Faça login (credenciais padrão ou configure conforme necessário)
3. Navegue para a seção de Onboarding
4. Siga os passos do wizard:
   - **Step 1**: Definir identidade da loja (nome)
   - **Step 2**: Escolher tema visual
   - **Step 3**: Configurar locale e moeda
   - **Step 4**: Definir subdomínio e publicar
   - **Complete**: Ver resumo e acessar storefront

### 5. Verificar Storefront

Após completar o onboarding, a loja estará disponível em `https://{subdomain}.localtest.me`.

## Troubleshooting

### Erro: Subdomain já em uso
- Escolha um subdomínio diferente
- Adicione números ou modifique o nome
- Verifique se não está usando palavras reservadas (admin, api, www, etc.)

### Erro: Loja não encontrada
- Verifique se o tenantId está correto
- Confirme que a loja foi criada no Step 1

### Erro: Domain not found
- Certifique-se de que o subdomínio foi definido antes de publicar
- Execute os passos em ordem

### Problemas de Conexão
- Verifique se os containers Docker estão rodando: `docker compose ps`
- Confirme portas 5000 (Admin) e 5001 (API) estão livres
- Verifique logs: `docker compose logs`

## Testes

Para executar testes:

```bash
# Testes unitários
dotnet test tests/MarketplaceBuilder.Tests.Unit/

# Testes de integração
dotnet test tests/MarketplaceBuilder.Tests.Integration/

# Testes de store provisioning
dotnet test tests/MarketplaceBuilder.Tests.StoreProvisioning/
```

## Estrutura do Projeto

- `src/MarketplaceBuilder.Admin`: Interface de administração
- `src/MarketplaceBuilder.Api`: API backend
- `src/MarketplaceBuilder.Storefront`: Interface da loja
- `infra/`: Configurações Docker
- `docs/`: Documentação
- `tests/`: Testes automatizados