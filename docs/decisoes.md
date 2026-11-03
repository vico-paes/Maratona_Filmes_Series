# Decisões do projeto

Arquivo de referência para continuar o projeto em qualquer máquina ou sessão nova. Mantenha atualizado a cada etapa.

## Regras gerais

- Monorepo: `/api` (ASP.NET Core .NET 10 + EF Core + PostgreSQL) e `/web` (Next.js + TypeScript).
- Nenhum comentário no código, em nenhum arquivo (inclusive migrations: as linhas `//` geradas pelo EF são removidas).
- Código simples, sem over-engineering. API organizada por pasta de funcionalidade, minimal APIs.
- Nenhum segredo no código: `TMDB_API_KEY`, `OMDB_API_KEY`, `JWT_SECRET`, `ConnectionStrings__Default` vêm de variáveis de ambiente ou user-secrets.
- Nomes no banco em snake_case (`EFCore.NamingConventions`). Enums salvos como texto.
- Migrations aplicadas automaticamente ao iniciar a API.

## Etapas

1. Estrutura e banco — concluída
2. Integração TMDB com cache — concluída
3. OMDb com fila e job
4. Autenticação e funcionalidades de usuário
5. Front-end (pedir prints da Biblioteca e do Perfil)

## Modelo de dados

| Tabela | Observações |
|---|---|
| users | username e email únicos (salvos em minúsculo), `rating_style` = Stars ou Numeric, `avatar_url`/`cover_url` só URL externa https (sem upload) |
| follows | PK (follower, followed), não pode seguir a si mesmo |
| titles | chave natural `tmdb_id + media_type` (ids do TMDB colidem entre filme e série). `credits` em jsonb (diretores, criadores, roteiristas, top 5 elenco). `atualizado_em` controla o cache |
| seasons | lista de temporadas vem com o título. `atualizado_em` (nulo até abrir) indica quando os episódios foram buscados |
| episodes | buscados sob demanda ao abrir a temporada; atualizados por `(season_id, episode_number)` para manter o id estável |
| external_ratings | notas OMDb (IMDb, Rotten Tomatoes, Metacritic opcional), `not_found`, `atualizado_em`, `next_refresh_at` |
| omdb_queue | fila de pedidos de nota com `request_count` |
| api_usage | contador diário persistido por `bucket` (ex.: omdb-demand, omdb-job) |
| job_runs | última execução de cada job, para rodar ao acordar se estiver atrasado (Render free dorme) |
| awards | Oscar (filmes) e Emmy (séries), ano, categoria, pessoa, `won`, `is_major` |
| library_entries | fonte única da nota do filme/série inteira, watchlist, assistido |
| season_entries | fonte única da nota da temporada, temporada assistida |
| reviews | só texto + spoiler. `season_id` nulo = review principal. Um review por usuário por título/temporada. A nota exibida vem de library_entries/season_entries |
| review_likes | curtidas |
| private_notes | a "Nota" privada com salvamento automático, por título, temporada ou episódio. Nunca pública |
| lists / list_items | listas personalizadas |
| favorites | 4 filmes + 4 séries (slot 1–4), uma coroa por tipo |

Notas: armazenadas de 0 a 10 com passo 0.5. Estrelas = nota / 2.

## TMDB

- Idioma `pt-BR`, região `BR`. Se a sinopse vier vazia em português, usa a de `en-US`.
- Cache por título: ao abrir, se `atualizado_em` > 7 dias, busca de novo. Se o TMDB falhar e houver cache, devolve o cache.
- Episódios: buscados ao abrir a temporada (`/titles/tv/{id}/seasons/{n}`), com o mesmo limite de 7 dias. Se o número de episódios da temporada mudar no TMDB, `seasons.atualizado_em` volta a nulo e força nova busca.
- Busca e listas da home não são gravadas no banco; os cards são enriquecidos com notas e prêmios do que já está no banco. Listas da home ficam 1 hora em cache de memória.
- Nada pode passar de 6 meses sem atualização: o job renova até 500 títulos por dia com mais de 90 dias.
- Job diário `tmdb-sync` (classe base `DailyJob`, checa a cada 30 min se passou 1 dia desde `job_runs.last_run_at`): 2 páginas de em cartaz, em breve, populares, séries no ar, no ar hoje e séries populares, mais `/changes` dos últimos 2 dias (só títulos que já estão no banco), mais os antigos.
- Logs do `HttpClient` em Warning para a chave da API não aparecer em log.

### Endpoints

| Rota | Descrição |
|---|---|
| `GET /home` | `nowPlaying`, `popularMovies`, `onTheAir`, `popularSeries` (cards) |
| `GET /titles/search?q=&page=` | `{ movies, series }` separados |
| `GET /titles/movie/{tmdbId}` | detalhes do filme |
| `GET /titles/tv/{tmdbId}` | detalhes da série com lista de temporadas |
| `GET /titles/tv/{tmdbId}/seasons/{n}` | temporada com episódios |

Card: `tmdbId, mediaType, name, year, posterPath, ratings, award`. `award.status` = `nominated`, `won` ou `major`.
- Imagens nunca baixadas: só `poster_path`, URL montada com a CDN do TMDB.
- Exibir logo do TMDB e o texto: "This product uses the TMDB API but is not endorsed or certified by TMDB."

## OMDb

- `imdb_id` vem do TMDB (detalhes do filme ou `external_ids` da série).
- Busca sob demanda só para usuário logado e só quando o título ainda não tem nota. Visitante vê as notas já salvas.
- Cota diária configurável: cerca de 700 sob demanda e 300 para o job. `OMDB_ENABLED=false` desliga tudo.
- Renovação: filmes antigos a cada 3–6 meses; lançamentos e séries no ar a cada 1–2 semanas no primeiro mês, depois espaçando. Job renova do mais antigo para o mais novo até acabar a cota.
- Metacritic pode faltar: tratar como nulo.

## Prêmios

- Fonte: Wikidata (SPARQL público, sem conta), ligação pelo `imdb_id`, job semanal. User-Agent com nome do projeto e link do GitHub.
- Selo: indicado = contorno vazio; venceu = dourado; venceu categoria principal = dourado brilhando.
- Principais Oscar: Filme, Direção, Ator, Atriz, Ator Coadjuvante, Atriz Coadjuvante, Roteiro Original, Roteiro Adaptado.
- Principais Emmy: Série de Drama, Série de Comédia, Série Limitada, Ator Principal, Atriz Principal.

## Front-end

- Abas: Início, Biblioteca (com sub-aba Listas), Feed, Perfil, Configurações.
- Busca separa filmes e séries.
- Biblioteca: filtro Tudo/Filmes/Séries; ordenação: não assistidos primeiro, assistidos primeiro, adicionados recentemente, assistidos recentemente, minha nota (maior/menor), nota IMDb, ano, A–Z.
- Feed: cards de review (pôster, título, usuário, nota, trecho, spoiler borrado), seletor Seguindo | Todos, rolagem infinita.
- Janela de escrita: seletor `🔒 Nota | Review`. Nota salva sozinha. Review tem botão Postar e opção de spoiler. Botão pequeno para trocar estrelas/número.
- Nota rápida usa o estilo escolhido no perfil.
- Séries: temporada abre em acordeão com episódios; Nota privada por episódio; review por temporada; review principal da série.
- Perfil: bio, nome, avatar, capa, 4+4 favoritos com coroa, vistos recentes, reviews recentes, estatísticas (vistos, horas, séries concluídas), seguidores/seguindo. Próprio perfil com botões de editar; de outra pessoa com seguir.
