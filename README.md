# Maratona — Filmes e Séries

Projeto pessoal, gratuito e sem fins comerciais, inspirado no Letterboxd, que cataloga **filmes e séries**. Permite marcar o que já assistiu, montar uma lista "quero ver", dar notas, escrever reviews, marcar temporadas assistidas, criar listas personalizadas e acompanhar um feed de reviews.

> This product uses the TMDB API but is not endorsed or certified by TMDB.

## Telas

- **Início:** lançamentos, populares, séries no ar e o aviso de novas temporadas das séries que você já terminou. Cada lista tem "Ver mais" com a lista completa. A busca fica no topo, mostra sugestões enquanto você digita e separa filmes, séries e pessoas.
- **Filme ou série:** sinopse, direção ou criação, roteiro, os 5 atores principais, notas do IMDb, Rotten Tomatoes e Metacritic, selo e lista de Oscars ou Emmys, e as reviews de quem já assistiu. Nas séries, cada temporada abre a lista de episódios.
- **Review:** marca como assistido ou "quero ver", dá a nota (estrelas ou 0 a 10, com troca só daquela nota), guarda a data em que você assistiu (dá para mudar, para catalogar algo visto há tempo) e publica o texto, com aviso de spoiler. Nas séries, a review pode ser da série inteira ou de uma temporada.
- **🔒 Nota:** anotação privada, separada da review, que salva sozinha enquanto você escreve. Existe para o filme, para a série e para cada episódio, e nunca é publicada.
- **Biblioteca:** filtros Todos/Filmes/Séries e Tudo/Quero ver/Assistindo/Assistidos, com as ordenações por status, data, nota, IMDb, ano e A–Z. O olhinho marca o que já foi assistido.
- **Feed:** reviews recentes de todo mundo ou só de quem você segue.
- **Perfil:** capa, foto, bio, seguidores, números (filmes, séries, temporadas, horas, reviews), 4 filmes e 4 séries favoritos com uma coroa em cada grupo, assistidos recentemente, reviews e listas.
- **Configurações:** dados do perfil, estilo de nota (com prévia da conversão das notas antigas antes de confirmar), senha e conta.

## Stack

- **API:** ASP.NET Core (.NET 10), Entity Framework Core, PostgreSQL
- **Web:** Next.js com TypeScript
- **Dados:** [TMDB](https://www.themoviedb.org/) (filmes, séries, elenco, temporadas), [OMDb](https://www.omdbapi.com/) (notas IMDb, Rotten Tomatoes, Metacritic), [oscar_data](https://github.com/DLu/oscar_data) (Oscar) e [Wikidata](https://www.wikidata.org/) (Emmy)

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

Crie `web/.env.local` com `NEXT_PUBLIC_API_URL=http://localhost:5000`. O front sobe em `http://localhost:3000`, que precisa ser o mesmo endereço cadastrado em `FRONTEND_URL` na API.

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
- Oscar: [DLu/oscar_data](https://github.com/DLu/oscar_data), licença BSD-2-Clause, baseado na base oficial da Academia.
- Emmy: [Wikidata](https://www.wikidata.org/) (CC0).

## Segurança

- Senhas guardadas com hash (PBKDF2 do ASP.NET Identity) e login com token JWT.
- Limite de requisições: 300 por minuto por IP, 120 por minuto por usuário e 10 por minuto por IP no login, cadastro e alterações de conta.
- CORS liberado apenas para a URL do front definida em `FRONTEND_URL`.
- Todas as entradas são validadas, e as notas privadas nunca aparecem em rotas públicas.
- No front, o token fica no `localStorage` do navegador e é apagado ao sair ou quando expira.

## Como funcionam notas e prêmios

- As notas externas são buscadas na OMDb só quando um usuário logado abre um título que ainda não tem nota. Um job diário processa a fila de pedidos e renova as notas antigas, sem passar da cota gratuita (1.000 por dia, divididas entre sob demanda e job).
- O selo do Oscar (filmes) e do Emmy (séries) aparece vazado quando o título foi só indicado, dourado quando venceu e dourado brilhando quando venceu uma categoria principal.

### Limitações conhecidas dos prêmios

Não são bugs. Elas vêm das fontes de dados gratuitas usadas pelo projeto.

- **Filmes (Oscar):** os dados vêm de um dataset completo da Academia, então vitórias, indicações, anos e categorias estão corretos. Um Oscar recente só aparece depois que o dataset é atualizado após a cerimônia, e o app importa essa atualização uma vez por semana.
- **Séries (Emmy):** não existe uma fonte gratuita e completa dos Emmys. O **número de vitórias é confiável**, porque vem da OMDb, que usa os dados do IMDb. Por isso o app destaca as vitórias. Já o **número de indicações pode aparecer menor que o real** nas séries que venceram algum Emmy. Nesses casos a OMDb só informa as vitórias, e a lista de indicações vem do Wikidata, que é incompleta. Nas séries que só foram indicadas, sem vitória, o número de indicações vem da OMDb e está correto.
- **Lista de prêmios da série:** a lista com categoria, ano e pessoa vem do Wikidata e pode não ter todos os prêmios, mesmo quando o total de vitórias está certo.
- **Brilho de categoria principal nas séries:** depende de o Wikidata registrar a vitória em uma categoria principal (Série de Drama, Comédia ou Limitada, ou Ator e Atriz Principal). Se o registro faltar, a série aparece dourada, sem brilho.
- **Títulos sem IMDb ID no TMDB:** não exibem notas externas nem selo de prêmios.
- **Resultados de busca:** o selo aparece para os títulos que já estão salvos no banco. Um título que aparece pela primeira vez é salvo em segundo plano e passa a mostrar o selo na próxima vez que surgir.

## Licença

[MIT](LICENSE)
