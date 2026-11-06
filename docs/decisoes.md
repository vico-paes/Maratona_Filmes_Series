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
4. Autenticação e funcionalidades de usuário — concluída
5. Front-end — concluída

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

Notas: cada nota guarda o valor e o estilo com que foi dada (`rating_style` em library_entries e season_entries). Número: 0 a 10, de 0,5 em 0,5. Estrelas: 0,5 a 5, de meia em meia. O valor fica na escala do próprio estilo (4,5 estrelas é guardado como 4.5).

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
| `GET /home/{list}?page=` | lista completa paginada (`now-playing`, `popular-movies`, `on-the-air`, `popular-series`), até 20 páginas, cache de 1 hora por página |
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

## Autenticação e proteções

- Senha com `PasswordHasher` do ASP.NET Identity (PBKDF2). Username e e-mail salvos em minúsculo. Nomes reservados: search, me, admin.
- JWT HS256 com `JWT_SECRET` (mínimo 32 caracteres), issuer/audience `maratona`, validade 7 dias, claims `sub` (id) e `unique_name`. Front guarda o token e envia `Authorization: Bearer`.
- Rate limiting: 300 req/min por IP e 120 req/min por usuário logado (encadeados, janela fixa); `/auth/*`, troca de senha e exclusão de conta: 10/min por IP. Resposta 429.
- IP real atrás do proxy do Render via `X-Forwarded-For` (`UseForwardedHeaders`).
- CORS só para `FRONTEND_URL` (aceita várias separadas por vírgula). Sem a variável, nenhuma origem é liberada.
- Validação com DataAnnotations + `AddValidation()`; erros em 400 no formato ValidationProblem. JSON inválido também vira 400.

## Regras de usuário

- Estilo da nota: o perfil tem o estilo padrão (`users.rating_style`). A nota rápida usa o padrão; a janela de review tem um botão que troca o estilo só naquela nota (`style` no corpo da requisição). Toda nota é exibida, para qualquer pessoa, no estilo com que foi dada; nunca é convertida na exibição.
- Trocar o estilo no perfil: o front pergunta se a pessoa quer converter as notas antigas e mostra antes a prévia de `GET /me/rating-conversion?to=stars|numeric` (quantidade, até 3 notas reais da pessoa e exemplos fixos). Se confirmar, envia `convertRatings: true` no `PUT /me/profile`. Estrelas → número: × 2. Número → estrelas: ÷ 2, arredondado para a meia estrela mais próxima (empate para cima), mínimo meia estrela (7 → 3,5; 8,5 → 4,5; 0 → 0,5).
- Ordenar por "minha nota" compara estrelas × 2 com números.
- Marcar como assistido tira do "quero ver". Entrada sem watchlist, sem assistido e sem nota é apagada.
- Dar nota numérica ou escrever review marca como assistido automaticamente (no título ou na temporada).
- Desmarcar "assistido" tira o título da Biblioteca, mas a nota numérica, o review e a Nota privada continuam salvos, e o review continua no perfil com a nota.
- Marcar todas as temporadas (número > 0) como assistidas marca a série como assistida. Desmarcar qualquer temporada tira o "assistido" da série; as outras temporadas e as notas continuam (`titleWatched` na resposta).
- Temporada nova (`GET /me/new-seasons` e `newSeasons` no card da Biblioteca): só para séries que a pessoa terminou (série marcada como assistida). Conta toda temporada com número > 0, não assistida, com data de estreia depois do dia em que a série foi marcada como assistida. `released` = false quando a estreia ainda não chegou ("em breve"). Séries em andamento não geram aviso.
- Data em que assistiu: `PUT .../watched` aceita `date` (yyyy-mm-dd, de 1900 até hoje) e grava ao meio-dia UTC daquele dia. Sem `date`, mantém a data que já existia ou usa agora. Serve para catalogar algo visto há tempo.
- Assistindo: série com pelo menos uma temporada assistida e ainda não marcada como assistida. Ao marcar uma temporada, a série ganha uma entrada na biblioteca (mesmo sem watchlist); ela só some quando não há watchlist, assistido, nota nem temporada assistida. Aparece em "Tudo" e no filtro `watching`, com `watching: true` no card.
- O job `tmdb-sync` atualiza diariamente as séries no ar que alguém assistiu, se o cache tiver mais de 3 dias, para detectar temporadas novas.
- Um review por usuário por título (principal) e por temporada; salvar de novo edita. Review só tem texto e spoiler.
- Nota privada: uma por título, por temporada ou por episódio; salvar com texto vazio apaga. Nunca aparece em rotas públicas.
- Favoritos: substituição completa, até 4 filmes e 4 séries, no máximo uma coroa em cada.
- Listas: até 100 por usuário e 500 itens por lista; privadas só aparecem para o dono (404 para os outros).
- Estatísticas do perfil: horas = soma da duração dos filmes assistidos + episódios × duração média das temporadas assistidas.

### Endpoints

| Rota | Auth | Descrição |
|---|---|---|
| `POST /auth/register`, `POST /auth/login` | não | `{ token, user }` |
| `GET /me` | sim | usuário logado |
| `PUT /me/password`, `POST /me/delete` | sim | configurações da conta |
| `PUT /me/profile` | sim | nome, bio, avatar/capa (https), `ratingStyle` stars/numeric, `convertRatings` |
| `GET /me/rating-conversion?to=` | sim | prévia da conversão de notas antes de trocar o estilo |
| `PUT /me/favorites` | sim | `{ movies: [{titleId, crown}], series: [...] }` |
| `GET /me/library?type=&status=&sort=&page=` | sim | type all/movie/tv; status all/watchlist/watching/watched; sort unwatched, watched, added, recent, rating_desc, rating_asc, imdb, year, az |
| `GET /me/new-seasons` | sim | temporadas novas das séries que a pessoa terminou |
| `GET /me/titles/{titleId}` | sim | estado do título: entrada, temporadas, meus reviews, minhas notas privadas |
| `PUT /me/titles/{titleId}/watchlist` `/watched` `/rating` | sim | `{ value }`, `{ value, date? }` ou `{ rating, style? }` (null limpa; sem `style` usa o do perfil) |
| `PUT /me/seasons/{seasonId}/watched` `/rating` | sim | temporada (`watched` também aceita `date`) |
| `PUT /me/titles/{titleId}/review` | sim | `{ seasonId?, body, spoiler }` |
| `DELETE /me/reviews/{id}` | sim | apaga o próprio review |
| `PUT /me/titles/{titleId}/notes` | sim | `{ seasonId?, episodeId?, body }` (autosave) |
| `GET /titles/{titleId}/reviews?seasonId=&page=` | não | reviews públicos |
| `POST/DELETE /reviews/{id}/like` | sim | curtir |
| `GET /feed?scope=all\|following&page=` | following exige | reviews recentes com card do título |
| `GET /users/search?q=` | não | buscar pessoas |
| `GET /users/{username}` | não | perfil (isMe, isFollowing quando logado); `recentlyWatched` traz card, data e nota |
| `GET /users/{username}/reviews?page=` | não | todas as reviews da pessoa, com card do título |
| `GET /users/{username}/followers` `/following` | não | listas paginadas |
| `POST/DELETE /users/{username}/follow` | sim | seguir |
| `GET /users/{username}/lists`, `GET /lists/{id}` | não | listas públicas (privadas só para o dono) |
| `POST /me/lists`, `PUT/DELETE /me/lists/{id}`, `POST /me/lists/{id}/items`, `DELETE /me/lists/{id}/items/{titleId}` | sim | gerenciar listas |

Os ids usados nas rotas de usuário são os ids internos (`id` do título e das temporadas em `/titles/...`), não os do TMDB.

## Front-end

- Next.js (App Router) + TypeScript + Tailwind CSS 4 + ícones `lucide-react`. Páginas são componentes de cliente que falam direto com a API (`src/lib/api.ts`); sem biblioteca de estado ou de fetch.
- Visual escuro com roxo (`--color-accent`); o dourado fica reservado para os selos de prêmio e a coroa. Pensado primeiro para celular: barra de abas embaixo no celular, abas no topo no computador.
- Token JWT no `localStorage`; resposta 401 apaga o token e a pessoa volta a ser visitante.
- Imagens com `<img>` direto da CDN do TMDB (sem otimização da Vercel); avatar e capa são links https externos.
- Rotas: `/` (Início), `/explorar/{em-cartaz|filmes-populares|series-no-ar|series-populares}`, `/busca?q=`, `/filme/{tmdbId}`, `/serie/{tmdbId}`, `/biblioteca`, `/feed`, `/u/{username}`, `/listas/{id}`, `/configuracoes`, `/entrar`, `/cadastro`.
- Abas: Início, Biblioteca, Feed, Perfil, Configurações.
- Busca separa filmes, séries e pessoas. Enquanto digita (a partir de 2 letras, 0,3 s depois de parar), mostra sugestões com etiqueta Filme/Série; setas e Enter navegam, Esc fecha, Enter sem escolha abre a busca completa.
- Início: cada fileira tem "Ver mais" (página `/explorar/...` com grade e "Carregar mais") e setas para rolar no computador.
- Selos de prêmio: ícones próprios e genéricos (silhueta de estatueta), não os logos oficiais do Oscar e do Emmy, que são marcas registradas.
- Biblioteca: Todos/Filmes/Séries, Tudo/Quero ver/Assistindo/Assistidos e as ordenações. Marcadores no pôster: olho (assistido), play (assistindo), marcador (quero ver) e "NOVA" para temporada nova.
- Feed: Todos | Seguindo, cards com pôster, nota no estilo de quem deu, spoiler borrado com botão para mostrar, curtir e "carregar mais".
- Review e Nota são separadas (decisão do usuário). Janela de review: Assisti / Quero ver, nota com botão pequeno Estrelas | 0–10 que vale só para aquela nota (converte o valor ao trocar), "Assistido em" com a data de hoje preenchida e editável, texto, spoiler, Publicar/Salvar. Nas séries, um seletor escolhe série inteira ou temporada.
- Nota privada: painel lateral (embaixo no celular) que salva sozinho 0,8 s depois de parar de digitar e ao fechar. Existe para o título e para cada episódio.
- Página do título: nota rápida no estilo do perfil (ou no estilo em que já foi dada), salva 0,5 s depois da última mudança.
- Séries: temporadas em acordeão com episódios, olho para marcar a temporada e lápis para a review/nota da temporada.
- Perfil: capa, avatar, nome, bio, seguindo/seguidores (abrem lista), editar perfil ou seguir, números, abas Visão geral / Reviews / Listas, 4+4 favoritos com coroa (editor com busca no próprio perfil), assistidos recentemente com nota e data, carrossel de reviews.
- Configurações: perfil com prévia, estilo de nota com a janela de conversão, senha, sair e excluir conta.
