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
3. OMDb com fila e job + prêmios (Oscar/Emmy) — concluída
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
| external_ratings | notas OMDb (IMDb, Rotten Tomatoes, Metacritic opcional), `awards_text` e contagens `oscar_wins/oscar_nominations/emmy_wins/emmy_nominations` extraídas do texto, `not_found`, `atualizado_em`, `next_refresh_at` |
| omdb_queue | fila de pedidos de nota com `request_count` |
| api_usage | contador diário persistido por `bucket` (ex.: omdb-demand, omdb-job) |
| job_runs | última execução de cada job, para rodar ao acordar se estiver atrasado (Render free dorme) |
| awards | ligado por `imdb_id` (não por título), `source` = oscar_data ou wikidata, ano da cerimônia, categoria em português, pessoa, `won`, `is_major` |
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
- Busca sob demanda só para usuário logado (JWT válido) e só quando o título ainda não tem nota. Visitante vê as notas já salvas e não entra na fila.
- Ao abrir sem nota: soma 1 em `omdb_queue.request_count`; se houver cota sob demanda, busca na hora (timeout 5 s) e tira da fila. Sem cota, a página abre sem notas.
- Cota: `api_usage` por dia (UTC) e bucket `omdb-demand` / `omdb-job`. Reserva atômica (`UPDATE ... WHERE calls < limite`) antes de cada chamada. Se a OMDb responder "Request limit reached", os dois buckets do dia são marcados como cheios.
- Limites: `OMDB_DAILY_LIMIT_DEMAND` (padrão 700) e `OMDB_DAILY_LIMIT_JOB` (padrão 300). `OMDB_ENABLED=false` desliga tudo.
- Job diário `omdb`, na ordem, até acabar a cota do job: fila (mais pedidos primeiro), renovações vencidas (`next_refresh_at`, do mais antigo para o mais novo), títulos sem nota (mais populares primeiro).
- Renovação (`RefreshSchedule`): até 30 dias do lançamento = 7 dias; até 90 = 14; filmes até 180 = 30, até 1 ano = 60, depois 120 dias. Séries no ar contam da última exibição: 7 / 14 / 30 dias. Não encontrado = 30 dias.
- Metacritic e Rotten Tomatoes podem faltar: ficam nulos.

## Prêmios

- Testado antes de implementar: o Wikidata tinha 8 das 13 indicações e 4 das 7 vitórias do Oppenheimer, e cobria mal os Emmys (Better Call Saul com 1 de ~53 indicações).
- Oscar: dataset [DLu/oscar_data](https://github.com/DLu/oscar_data) (BSD-2), coluna `FilmId` = IMDb ID. Job `oscar-import` semanal substitui tudo do source `oscar_data`. Linhas com vários filmes (`tt1|tt2`) viram um registro por filme. Ano = ano da cerimônia.
- Emmy: Wikidata, propriedade P345 = IMDb ID, lotes de 10 séries, job diário `emmy-wikidata` para séries não verificadas há 30 dias (`titles.awards_checked_at`). Só categorias "Primetime Emmy Award for Outstanding ...". Registros repetidos agrupados por categoria + ano.
- Contagem do selo: maior valor entre os registros de prêmios e as contagens do texto `Awards` da OMDb (ex.: "Won 16 Primetime Emmys", "Nominated for 53 Primetime Emmys"). Nas séries vencedoras, o número de indicações pode ficar menor que o real.
- Selo (`award`): `kind` oscar/emmy, `status` nominated (vazado) / won (dourado) / major (dourado brilhando), `wins`, `nominations`. `major` só quando um registro confirma vitória em categoria principal.
- Busca e home: títulos que ainda não estão no banco entram numa fila em memória (`TitleWarmupQueue`) e são gravados em segundo plano; o selo aparece a partir da próxima exibição.
- Créditos obrigatórios no README e no rodapé: TMDB, OMDb, oscar_data, Wikidata.
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
