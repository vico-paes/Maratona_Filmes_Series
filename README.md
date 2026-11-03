# Maratona — Filmes e Séries

Projeto pessoal, gratuito e sem fins comerciais, inspirado no Letterboxd, que cataloga **filmes e séries**. Permite marcar o que já assistiu, montar uma lista "quero ver", dar notas, escrever reviews, marcar temporadas assistidas, criar listas personalizadas e acompanhar um feed de reviews.

> This product uses the TMDB API but is not endorsed or certified by TMDB.

## Stack

- **API:** ASP.NET Core (.NET 10), Entity Framework Core, PostgreSQL
- **Web:** Next.js com TypeScript
- **Dados:** [TMDB](https://www.themoviedb.org/) (filmes, séries, elenco, temporadas), [OMDb](https://www.omdbapi.com/) (notas IMDb, Rotten Tomatoes, Metacritic) e [Wikidata](https://www.wikidata.org/) (Oscar e Emmy)

```
/api    API ASP.NET Core
/web    Front-end Next.js
/docs   Decisões do projeto
```

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js LTS](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para o Postgres local)
- Ferramenta do EF Core: `dotnet tool install -g dotnet-ef`

## Como obter as chaves gratuitas

### TMDB

1. Crie uma conta em [themoviedb.org](https://www.themoviedb.org/signup).
2. Acesse **Configurações → API** ([link direto](https://www.themoviedb.org/settings/api)) e solicite uma chave do tipo **Developer**, informando que é um projeto pessoal sem fins comerciais.
3. Copie a **API Key (v3)**. Ela será a variável `TMDB_API_KEY`.

### OMDb

1. Acesse [omdbapi.com/apikey.aspx](https://www.omdbapi.com/apikey.aspx) e escolha o plano **FREE** (1.000 requisições por dia).
2. Confirme o e-mail recebido para ativar a chave.
3. A chave será a variável `OMDB_API_KEY`.

## Variáveis de ambiente

Veja os nomes em [.env.example](.env.example).

| Variável | Uso |
|---|---|
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` | Postgres local do docker-compose |
| `ConnectionStrings__Default` | Connection string da API |
| `TMDB_API_KEY` | Chave v3 do TMDB |
| `OMDB_API_KEY` | Chave da OMDb |
| `OMDB_ENABLED` | `false` desliga totalmente a OMDb |
| `OMDB_DAILY_LIMIT_DEMAND` | Limite diário de chamadas sob demanda (padrão 700) |
| `OMDB_DAILY_LIMIT_JOB` | Limite diário de chamadas do job (padrão 300) |
| `JWT_SECRET` | Segredo para assinar os tokens (mínimo 32 caracteres) |
| `FRONTEND_URL` | URL do front liberada no CORS |
| `NEXT_PUBLIC_API_URL` | URL da API usada pelo front |

## Rodando localmente

### 1. Banco de dados

Copie `.env.example` para `.env` na raiz e preencha `POSTGRES_USER`, `POSTGRES_PASSWORD` e `POSTGRES_DB`. Depois:

```bash
docker compose up -d
```

### 2. API

No desenvolvimento as chaves ficam no **user-secrets** do .NET (fora do repositório):

```bash
cd api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=SEU_DB;Username=SEU_USUARIO;Password=SUA_SENHA"
dotnet user-secrets set "TMDB_API_KEY" "sua-chave-tmdb"
dotnet user-secrets set "OMDB_API_KEY" "sua-chave-omdb"
dotnet user-secrets set "JWT_SECRET" "um-segredo-longo-com-pelo-menos-32-caracteres"
dotnet user-secrets set "FRONTEND_URL" "http://localhost:3000"
dotnet run
```

A API sobe em `http://localhost:5000` e aplica as migrations automaticamente. Teste em `http://localhost:5000/health`.

### 3. Web

```bash
cd web
npm install
npm run dev
```

Crie `web/.env.local` com `NEXT_PUBLIC_API_URL=http://localhost:5000`. O front sobe em `http://localhost:3000`.

## Deploy

### Banco — Neon

1. Crie um projeto gratuito em [neon.tech](https://neon.tech/).
2. Copie os dados de conexão e monte a connection string no formato do .NET:
   `Host=SEU_HOST.neon.tech;Database=SEU_DB;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require`

### API — Render

1. Em [render.com](https://render.com/), crie um **Web Service** a partir deste repositório.
2. Escolha **Docker** como runtime e `api` como **Root Directory**.
3. Cadastre as variáveis: `ConnectionStrings__Default`, `TMDB_API_KEY`, `OMDB_API_KEY`, `OMDB_ENABLED`, `JWT_SECRET`, `FRONTEND_URL` (URL da Vercel).
4. A API escuta na porta `8080`.

No plano gratuito o Render "dorme" após um tempo sem acesso. Os jobs diários guardam a última execução no banco e rodam assim que a API acorda, caso estejam atrasados. Para mantê-la acordada, pode-se usar um serviço gratuito de ping (como o cron-job.org) chamando `/health`.

### Web — Vercel

1. Em [vercel.com](https://vercel.com/), importe o repositório e defina `web` como **Root Directory**.
2. Cadastre `NEXT_PUBLIC_API_URL` com a URL da API no Render.

## Créditos

- Dados de filmes e séries: [TMDB](https://www.themoviedb.org/). This product uses the TMDB API but is not endorsed or certified by TMDB.
- Notas externas: [OMDb API](https://www.omdbapi.com/).
- Prêmios: [Wikidata](https://www.wikidata.org/).

## Licença

[MIT](LICENSE)
