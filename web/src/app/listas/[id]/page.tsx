"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import { Lock, Pencil, Trash2, X } from "lucide-react";
import { PosterCard, PosterGrid } from "@/components/PosterCard";
import { Avatar, Button, ErrorText, Field, inputClass, Message, Modal, Spinner } from "@/components/ui";
import { api, ApiError } from "@/lib/api";
import { useApi } from "@/lib/hooks";
import type { ListDetails } from "@/lib/types";

function EditList({ list, onClose, onSaved }: { list: ListDetails; onClose: () => void; onSaved: () => void }) {
  const router = useRouter();
  const [form, setForm] = useState({ name: list.name, description: list.description ?? "", isPublic: list.isPublic });
  const [busy, setBusy] = useState<"save" | "delete" | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [error, setError] = useState("");

  async function save() {
    setBusy("save");
    setError("");
    try {
      await api(`/me/lists/${list.id}`, { method: "PUT", body: { name: form.name.trim(), description: form.description.trim() || null, isPublic: form.isPublic } });
      onSaved();
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Algo deu errado.");
      setBusy(null);
    }
  }

  async function remove() {
    setBusy("delete");
    try {
      await api(`/me/lists/${list.id}`, { method: "DELETE" });
      router.replace(`/u/${list.owner.username}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Algo deu errado.");
      setBusy(null);
    }
  }

  return (
    <Modal
      open
      onClose={onClose}
      title="Editar lista"
      footer={
        <div className="flex gap-3">
          <Button onClick={onClose} className="flex-1">
            Cancelar
          </Button>
          <Button variant="primary" onClick={save} busy={busy === "save"} disabled={!form.name.trim()} className="flex-[2]">
            Salvar
          </Button>
        </div>
      }
    >
      <div className="space-y-4">
        <Field label="Nome">
          <input className={inputClass} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} maxLength={100} />
        </Field>
        <Field label="Descrição">
          <textarea className={inputClass} rows={3} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} maxLength={1000} />
        </Field>
        <label className="flex cursor-pointer items-center gap-2 text-sm">
          <input type="checkbox" checked={form.isPublic} onChange={(e) => setForm({ ...form, isPublic: e.target.checked })} className="size-4 accent-[var(--color-accent)]" />
          Lista pública
        </label>
        <ErrorText>{error}</ErrorText>
        <div className="border-t border-line pt-4">
          {confirmDelete ? (
            <div className="flex items-center gap-2">
              <span className="flex-1 text-sm text-muted">Apagar a lista? Isso não pode ser desfeito.</span>
              <Button size="sm" onClick={() => setConfirmDelete(false)}>
                Não
              </Button>
              <Button size="sm" variant="danger" onClick={remove} busy={busy === "delete"}>
                Apagar
              </Button>
            </div>
          ) : (
            <Button size="sm" variant="danger" onClick={() => setConfirmDelete(true)}>
              <Trash2 className="size-4" />
              Apagar lista
            </Button>
          )}
        </div>
      </div>
    </Modal>
  );
}

export default function ListPage() {
  const { id } = useParams<{ id: string }>();
  const { data: list, error, reload, mutate } = useApi<ListDetails>(/^\d+$/.test(id) ? `/lists/${id}` : null);
  const [editing, setEditing] = useState(false);

  if (error || !/^\d+$/.test(id)) return <Message title="Lista não encontrada">{error?.status === 404 || !error ? "Ela pode ser privada ou ter sido apagada." : error.message}</Message>;
  if (!list) return <Spinner />;

  async function removeItem(titleId: number) {
    await api(`/me/lists/${list!.id}/items/${titleId}`, { method: "DELETE" });
    mutate((l) => ({ ...l, items: l.items.filter((i) => i.titleId !== titleId) }));
  }

  return (
    <div className="space-y-6">
      <title>{`${list.name} · Maratona`}</title>
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0 space-y-2">
          <h1 className="flex items-center gap-2 text-2xl font-bold">
            {list.name}
            {!list.isPublic ? <Lock className="size-5 text-faint" /> : null}
          </h1>
          <Link href={`/u/${list.owner.username}`} className="flex w-fit items-center gap-2 text-sm text-muted hover:text-accent">
            <Avatar url={list.owner.avatarUrl} name={list.owner.displayName} size={24} />
            {list.owner.displayName}
          </Link>
          {list.description ? <p className="max-w-2xl whitespace-pre-line text-sm text-muted">{list.description}</p> : null}
          <p className="text-xs text-faint">{list.items.length} títulos</p>
        </div>
        {list.isMine ? (
          <Button onClick={() => setEditing(true)}>
            <Pencil className="size-4" />
            Editar
          </Button>
        ) : null}
      </div>
      {list.items.length === 0 ? (
        <Message title="Lista vazia">{list.isMine ? "Adicione títulos pelo botão “Lista” na página de cada filme ou série." : null}</Message>
      ) : (
        <PosterGrid>
          {list.items.map((item) => (
            <div key={item.titleId} className="relative">
              <PosterCard title={item.title} />
              {list.isMine ? (
                <button
                  type="button"
                  onClick={() => removeItem(item.titleId)}
                  className="absolute bottom-12 right-1.5 rounded-full bg-black/75 p-1.5 text-fg hover:text-danger"
                  aria-label={`Tirar ${item.title.name} da lista`}
                >
                  <X className="size-3.5" />
                </button>
              ) : null}
            </div>
          ))}
        </PosterGrid>
      )}
      {editing ? <EditList list={list} onClose={() => setEditing(false)} onSaved={reload} /> : null}
    </div>
  );
}
