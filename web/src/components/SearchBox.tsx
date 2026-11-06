"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { ArrowRight, Film, LoaderCircle, Search, Tv } from "lucide-react";
import { query } from "@/lib/api";
import { imageUrl, titleHref } from "@/lib/format";
import { useApi, useDebounced } from "@/lib/hooks";
import type { TitleCard, UserSummary } from "@/lib/types";
import { AwardBadge } from "./AwardBadge";
import { Avatar, cx } from "./ui";

type Suggestion = { key: string; href: string; title?: TitleCard; user?: UserSummary };

function TypeTag({ type }: { type: TitleCard["mediaType"] }) {
  return (
    <span
      className={cx(
        "inline-flex shrink-0 items-center gap-1 rounded-md px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide",
        type === "movie" ? "bg-accent-soft text-accent" : "bg-sky-500/15 text-sky-300",
      )}
    >
      {type === "movie" ? <Film className="size-3" /> : <Tv className="size-3" />}
      {type === "movie" ? "Filme" : "Série"}
    </span>
  );
}

function TitleRow({ title }: { title: TitleCard }) {
  const poster = imageUrl(title.posterPath, "w92");
  return (
    <>
      <div className="aspect-[2/3] w-9 shrink-0 overflow-hidden rounded-md bg-surface-3">
        {poster ? <img src={poster} alt="" loading="lazy" className="size-full object-cover" /> : null}
      </div>
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium">{title.name}</p>
        <div className="mt-0.5 flex items-center gap-2 text-xs text-faint">
          <TypeTag type={title.mediaType} />
          {title.year ? <span>{title.year}</span> : null}
        </div>
      </div>
      <AwardBadge badge={title.award} />
    </>
  );
}

export function SearchBox({ className }: { className?: string }) {
  const router = useRouter();
  const [value, setValue] = useState("");
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const q = useDebounced(value.trim(), 300);
  const ready = q.length >= 2;
  const titles = useApi<{ movies: TitleCard[]; series: TitleCard[] }>(ready ? `/titles/search${query({ q })}` : null);
  const people = useApi<UserSummary[]>(ready ? `/users/search${query({ q: q.slice(0, 50) })}` : null);
  const [last, setLast] = useState<{ titles?: typeof titles.data; people?: UserSummary[] }>({});
  if ((titles.data && titles.data !== last.titles) || (people.data && people.data !== last.people)) {
    setLast({ titles: titles.data ?? last.titles, people: people.data ?? last.people });
  }
  const shownTitles = titles.data ?? last.titles;
  const shownPeople = people.data ?? last.people;

  const movies = shownTitles?.movies.slice(0, 5) ?? [];
  const series = shownTitles?.series.slice(0, 5) ?? [];
  const users = shownPeople?.slice(0, 3) ?? [];
  const groups: { label: string; items: Suggestion[] }[] = [
    { label: "Filmes", items: movies.map((t) => ({ key: `m${t.tmdbId}`, href: titleHref(t.mediaType, t.tmdbId), title: t })) },
    { label: "Séries", items: series.map((t) => ({ key: `s${t.tmdbId}`, href: titleHref(t.mediaType, t.tmdbId), title: t })) },
    { label: "Pessoas", items: users.map((u) => ({ key: `u${u.username}`, href: `/u/${u.username}`, user: u })) },
  ].filter((g) => g.items.length > 0);
  const flat = groups.flatMap((g) => g.items);
  const typing = value.trim().length >= 2 && (q !== value.trim() || titles.loading);
  const showPanel = open && value.trim().length >= 2;

  function go(href: string, keepText = false) {
    setOpen(false);
    setActive(-1);
    if (!keepText) setValue("");
    router.push(href);
  }

  function searchAll() {
    const text = value.trim();
    if (text) go(`/busca?q=${encodeURIComponent(text)}`, true);
  }

  function onKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "ArrowDown" && flat.length) {
      e.preventDefault();
      setOpen(true);
      setActive((i) => (i + 1) % flat.length);
    } else if (e.key === "ArrowUp" && flat.length) {
      e.preventDefault();
      setActive((i) => (i <= 0 ? flat.length - 1 : i - 1));
    } else if (e.key === "Escape") {
      setOpen(false);
      setActive(-1);
    }
  }

  return (
    <form
      role="search"
      className={cx("relative", className)}
      onSubmit={(e) => {
        e.preventDefault();
        if (active >= 0 && flat[active]) go(flat[active].href);
        else searchAll();
      }}
    >
      <Search className="pointer-events-none absolute left-3.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
      <input
        value={value}
        onChange={(e) => {
          setValue(e.target.value);
          setOpen(true);
          setActive(-1);
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => setOpen(false)}
        onKeyDown={onKeyDown}
        placeholder="Buscar filmes, séries e pessoas"
        maxLength={100}
        role="combobox"
        aria-expanded={showPanel}
        aria-controls="search-suggestions"
        aria-autocomplete="list"
        className="h-10 w-full rounded-full border border-line bg-surface-2 pl-10 pr-9 text-sm placeholder:text-faint focus:border-accent focus:outline-none"
      />
      {typing ? <LoaderCircle className="absolute right-3.5 top-1/2 size-4 -translate-y-1/2 animate-spin text-faint" /> : null}
      {showPanel ? (
        <div
          id="search-suggestions"
          role="listbox"
          onMouseDown={(e) => e.preventDefault()}
          className="absolute right-0 top-full z-50 mt-2 max-h-[70dvh] w-full overflow-y-auto rounded-2xl border border-line bg-surface p-2 shadow-2xl max-sm:fixed max-sm:inset-x-3 max-sm:top-16 max-sm:w-auto sm:min-w-[22rem]"
        >
          {groups.length === 0 ? (
            <p className="px-3 py-4 text-center text-sm text-muted">{typing ? "Buscando…" : "Nada encontrado."}</p>
          ) : (
            groups.map((group) => (
              <div key={group.label} className="py-1">
                <p className="px-3 pb-1 pt-1.5 text-[11px] font-semibold uppercase tracking-wider text-faint">{group.label}</p>
                {group.items.map((item) => {
                  const index = flat.indexOf(item);
                  return (
                    <button
                      key={item.key}
                      type="button"
                      role="option"
                      aria-selected={index === active}
                      onMouseEnter={() => setActive(index)}
                      onClick={() => go(item.href)}
                      className={cx("flex w-full items-center gap-3 rounded-xl px-3 py-2 text-left", index === active ? "bg-surface-3" : "hover:bg-surface-2")}
                    >
                      {item.title ? (
                        <TitleRow title={item.title} />
                      ) : item.user ? (
                        <>
                          <Avatar url={item.user.avatarUrl} name={item.user.displayName} size={36} />
                          <div className="min-w-0 flex-1">
                            <p className="truncate text-sm font-medium">{item.user.displayName}</p>
                            <p className="truncate text-xs text-faint">@{item.user.username}</p>
                          </div>
                        </>
                      ) : null}
                    </button>
                  );
                })}
              </div>
            ))
          )}
          <button
            type="button"
            onClick={searchAll}
            className="mt-1 flex w-full items-center justify-between rounded-xl border-t border-line px-3 py-2.5 text-sm font-medium text-accent hover:bg-surface-2"
          >
            Ver todos os resultados para “{value.trim()}”
            <ArrowRight className="size-4" />
          </button>
        </div>
      ) : null}
    </form>
  );
}
