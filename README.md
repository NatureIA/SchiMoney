# SchiMoney

Sistema financeiro online com **dois módulos independentes** dentro da mesma aplicação:

- **Pessoal** — receitas, despesas, categorias, formas de pagamento, parcelamentos e visão financeira individual.
- **Barbearia** — faturamento, vendas, despesas, serviços, margem, ticket médio e projeção operacional.

Os módulos não funcionam como filtro. Cada ambiente possui navegação, regras e dados próprios.

## Stack

- ASP.NET Core MVC / .NET 8
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- GitHub Actions
- MonsterASP via WebDeploy

## Estrutura

```
src/SchiMoney.Web/
├── Areas/
│   ├── Personal/
│   └── Barbershop/
├── Controllers/
├── Data/
├── Models/
├── Views/
└── wwwroot/
```

## Variáveis obrigatórias em produção

Configure no ambiente do MonsterASP:

```
ConnectionStrings__DefaultConnection
SCHIMONEY_ADMIN_EMAIL
SCHIMONEY_ADMIN_PASSWORD
```

A senha do administrador deve ter pelo menos 10 caracteres, letra maiúscula, minúscula, número e símbolo.

## Deploy

O workflow `.github/workflows/publish.yml` compila o projeto a cada push. O deploy para MonsterASP é executado automaticamente quando os quatro secrets abaixo estiverem cadastrados no GitHub:

```
WEBSITE_NAME
SERVER_COMPUTER_NAME
SERVER_USERNAME
SERVER_PASSWORD
```

O banco é criado automaticamente na primeira inicialização usando a conexão SQL Server configurada.
