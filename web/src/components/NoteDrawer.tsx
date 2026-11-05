"use client";

import { useCallback, useState } from "react";
import { Check, CloudOff, LoaderCircle, Lock, X } from "lucide-react";
import { api } from "@/lib/api";
import { useAutosave } from "@/lib/hooks";
import type { Note } from "@/lib/types";

export type NoteTarget = { titleId: number; seasonId: number | null; episodeId: number | null; label: string; subtitle?: string };

export function NoteDrawer({
  target,
  initial,
  onSaved,
  onClose,
}: {
  target: NoteTarget;
  initial: string;
  onSaved: (note: Note | null) => void;
  onClose: () => void;
}) {
  const [body, setBody] = useState(initial);

  const save = useCallback(
    async (value: string) => {
      const note = await api<Note | undefined>(`/me/titles/${target.titleId}/notes`, {
        method: "PUT",
        body: { seasonId: target.seasonId, episodeId: target.episodeId, body: value },
      });
      onSaved(note ?? null);
    },
    [target, onSaved],
  );

  const { status, schedule, flush } = useAutosave(save);

  async function close() {
    await flush();
    onClose();
  }

  return (
    <aside
      role="dialog"
      aria-label="Nota privada"
      className="fixed inset-x-0 bottom-0 z-40 flex max-h-[80dvh] flex-col rounded-t-2xl border border-line bg-surface shadow-2xl md:inset-y-0 md:left-auto md:right-0 md:max-h-none md:w-[420px] md:rounded-none md:border-y-0 md:border-r-0"
    >
      <div className="flex items-start gap-3 border-b border-line px-5 py-4">
        <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-xl bg-accent-soft text-accent">
          <Lock className="size-4" />
        </span>
        <div className="min-w-0 flex-1">
          <p className="font-semibold">Nota · {target.label}</p>
          {target.subtitle ? <p className="truncate text-sm text-muted">{target.subtitle}</p> : null}
        </div>
        <button onClick={close} className="rounded-lg p-1 text-muted hover:bg-surface-2 hover:text-fg" aria-label="Fechar nota">
          <X className="size-5" />
        </button>
      </div>
      <div className="flex flex-1 flex-col gap-3 overflow-hidden px-5 py-4">
        <p className="text-xs text-faint">Só você vê isto. A nota nunca é publicada e salva sozinha enquanto você escreve.</p>
        <textarea
          autoFocus
          value={body}
          onChange={(e) => {
            setBody(e.target.value);
            schedule(e.target.value);
          }}
          onBlur={() => void flush()}
          maxLength={20000}
          placeholder="Anote o que está achando enquanto assiste…"
          className="min-h-48 flex-1 resize-none rounded-xl border border-line bg-surface-2 p-3.5 text-sm leading-relaxed placeholder:text-faint focus:border-accent focus:outline-none"
        />
        <div className="flex h-5 items-center gap-1.5 text-xs text-faint">
          {status === "pending" || status === "saving" ? (
            <>
              <LoaderCircle className="size-3.5 animate-spin" /> Salvando…
            </>
          ) : status === "saved" ? (
            <>
              <Check className="size-3.5 text-ok" /> Salvo
            </>
          ) : status === "error" ? (
            <>
              <CloudOff className="size-3.5 text-danger" /> Não foi possível salvar. Continue escrevendo para tentar de novo.
            </>
          ) : null}
        </div>
      </div>
    </aside>
  );
}
