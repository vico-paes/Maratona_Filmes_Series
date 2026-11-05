"use client";

import { useState } from "react";
import { Crown, Plus, Search, X } from "lucide-react";
import { api, ApiError, query } from "@/lib/api";
import { useApi, useDebounced } from "@/lib/hooks";
import type { Favorite, MediaType, TitleCard, TitleDetails } from "@/lib/types";
import { Poster } from "../PosterCard";
import { Button, cx, ErrorText, inputClass, Modal, Spinner } from "../ui";

type Pick = { titleId: number; title: TitleCard; crown: boolean };

function Picker({ type, exclude, onPick }: { type: MediaType; exclude: number[]; onPick: (pick: Pick) => void }) {
  const [text, setText] = useState("");
  const q = useDebounced(text.trim(), 400);
  const results = useApi<{ movies: TitleCard[]; series: TitleCard[] }>(q ? `/titles/search${query({ q })}` : null);
  const [busy, setBusy] = useState<number | null>(null);
  const items = (type === "movie" ? results.data?.movies : results.data?.series) ?? [];

  async function pick(card: TitleCard) {
    setBusy(card.tmdbId);
    try {
      const details = await api<TitleDetails>(`/titles/${type}/${card.tmdbId}`);
      onPick({ titleId: details.id, title: card, crown: false });
      setText("");
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="space-y-3">
      <div className="relative">
        <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-faint" />
        <input
          autoFocus
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={type === "movie" ? "Buscar filme" : "Buscar série"}
          maxLength={100}
          className={cx(inputClass, "pl-9")}
        />
      </div>
      {results.loading ? <Spinner className="py-6" /> : null}
      <div className="grid grid-cols-4 gap-2 sm:grid-cols-6">
        {items.slice(0, 12).map((card) => (
          <button
            key={card.tmdbId}
            type="button"
            disabled={busy !== null || exclude.includes(card.tmdbId)}
            onClick={() => pick(card)}
            title={card.name}
            className="relative text-left disabled:opacity-40"
          >
            <Poster path={card.posterPath} name={card.name} type={type} />
            {busy === card.tmdbId ? <span className="absolute inset-0 animate-pulse rounded-xl bg-black/50" /> : null}
            <p className="mt-1 truncate text-[11px] text-muted">{card.name}</p>
          </button>
        ))}
      </div>
    </div>
  );
}

function Group({ type, picks, setPicks }: { type: MediaType; picks: Pick[]; setPicks: (picks: Pick[]) => void }) {
  const [adding, setAdding] = useState(false);
  return (
    <div className="space-y-3">
      <p className="text-sm font-medium text-muted">{type === "movie" ? "Filmes favoritos" : "Séries favoritas"}</p>
      <div className="grid grid-cols-4 gap-2">
        {[0, 1, 2, 3].map((i) => {
          const pick = picks[i];
          if (!pick) {
            return (
              <button
                key={i}
                type="button"
                onClick={() => setAdding(true)}
                className="flex aspect-[2/3] items-center justify-center rounded-xl border border-dashed border-line text-faint hover:border-accent hover:text-accent"
                aria-label="Adicionar favorito"
              >
                <Plus className="size-6" />
              </button>
            );
          }
          return (
            <div key={pick.titleId} className="relative">
              <Poster path={pick.title.posterPath} name={pick.title.name} type={type} className={cx(pick.crown && "ring-2 ring-gold")} />
              <button
                type="button"
                onClick={() => setPicks(picks.filter((p) => p.titleId !== pick.titleId))}
                className="absolute right-1 top-1 rounded-full bg-black/75 p-1 text-fg hover:text-danger"
                aria-label="Remover"
              >
                <X className="size-3.5" />
              </button>
              <button
                type="button"
                onClick={() => setPicks(picks.map((p) => ({ ...p, crown: p.titleId === pick.titleId ? !p.crown : false })))}
                className={cx(
                  "absolute left-1 top-1 rounded-full p-1",
                  pick.crown ? "bg-gold text-black" : "bg-black/75 text-faint hover:text-gold",
                )}
                aria-label={pick.crown ? "Tirar coroa" : "Dar coroa"}
                title="A coroa marca o favorito número 1"
              >
                <Crown className="size-3.5" />
              </button>
            </div>
          );
        })}
      </div>
      {adding && picks.length < 4 ? (
        <Picker
          type={type}
          exclude={picks.map((p) => p.title.tmdbId)}
          onPick={(pick) => {
            const next = [...picks, pick];
            setPicks(next);
            if (next.length >= 4) setAdding(false);
          }}
        />
      ) : null}
    </div>
  );
}

export function FavoritesEditor({
  movies,
  series,
  onClose,
  onSaved,
}: {
  movies: Favorite[];
  series: Favorite[];
  onClose: () => void;
  onSaved: () => void;
}) {
  const toPicks = (list: Favorite[]) => list.map((f) => ({ titleId: f.titleId, title: f.title, crown: f.crown }));
  const [moviePicks, setMoviePicks] = useState<Pick[]>(() => toPicks(movies));
  const [seriesPicks, setSeriesPicks] = useState<Pick[]>(() => toPicks(series));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  async function save() {
    setBusy(true);
    setError("");
    try {
      const body = (picks: Pick[]) => picks.map((p) => ({ titleId: p.titleId, crown: p.crown }));
      await api("/me/favorites", { method: "PUT", body: { movies: body(moviePicks), series: body(seriesPicks) } });
      onSaved();
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Algo deu errado.");
      setBusy(false);
    }
  }

  return (
    <Modal
      open
      wide
      onClose={onClose}
      title="Editar favoritos"
      footer={
        <div className="flex gap-3">
          <Button onClick={onClose} className="flex-1">
            Cancelar
          </Button>
          <Button variant="primary" onClick={save} busy={busy} className="flex-[2]">
            Salvar favoritos
          </Button>
        </div>
      }
    >
      <div className="space-y-6">
        <p className="text-sm text-faint">Escolha até 4 filmes e 4 séries. Toque na coroa para marcar o favorito número 1 de cada grupo.</p>
        <Group type="movie" picks={moviePicks} setPicks={setMoviePicks} />
        <Group type="tv" picks={seriesPicks} setPicks={setSeriesPicks} />
        <ErrorText>{error}</ErrorText>
      </div>
    </Modal>
  );
}
