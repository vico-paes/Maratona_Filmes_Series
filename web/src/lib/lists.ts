import type { Home } from "./types";

export const homeLists = [
  { slug: "em-cartaz", api: "now-playing", key: "nowPlaying", title: "Em cartaz" },
  { slug: "filmes-populares", api: "popular-movies", key: "popularMovies", title: "Filmes populares" },
  { slug: "series-no-ar", api: "on-the-air", key: "onTheAir", title: "Séries no ar" },
  { slug: "series-populares", api: "popular-series", key: "popularSeries", title: "Séries populares" },
] as const satisfies readonly { slug: string; api: string; key: keyof Home; title: string }[];

export function findHomeList(slug: string) {
  return homeLists.find((list) => list.slug === slug);
}
