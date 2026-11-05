import type { MediaType, RatingStyle } from "./types";

export function imageUrl(path: string | null | undefined, size: "w92" | "w185" | "w342" | "w500" | "w780" | "w1280" | "original" = "w342") {
  return path ? `https://image.tmdb.org/t/p/${size}${path}` : null;
}

export function titleHref(type: MediaType, tmdbId: number) {
  return `/${type === "movie" ? "filme" : "serie"}/${tmdbId}`;
}

export function formatDate(value: string | null | undefined, withYear = true) {
  if (!value) return "";
  const date = value.length === 10 ? new Date(`${value}T12:00:00`) : new Date(value);
  return date.toLocaleDateString("pt-BR", withYear ? { day: "2-digit", month: "short", year: "numeric" } : { day: "2-digit", month: "short" });
}

export function toDateInput(value: string | null | undefined) {
  const date = value ? new Date(value) : new Date();
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

export function todayInput() {
  return toDateInput(null);
}

export function formatNumber(value: number) {
  return value.toLocaleString("pt-BR", { maximumFractionDigits: 1 });
}

export function formatRating(rating: number, style: RatingStyle) {
  return style === "stars" ? `${formatNumber(rating)} ★` : formatNumber(rating);
}

export function formatRuntime(minutes: number | null) {
  if (!minutes) return "";
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return h ? `${h}h${m ? ` ${m}min` : ""}` : `${m}min`;
}

export function formatVotes(votes: number) {
  if (votes >= 1_000_000) return `${formatNumber(votes / 1_000_000)} mi`;
  return votes >= 1000 ? `${Math.round(votes / 1000)} mil` : String(votes);
}

export function plural(count: number, one: string, many: string) {
  return `${count.toLocaleString("pt-BR")} ${count === 1 ? one : many}`;
}
