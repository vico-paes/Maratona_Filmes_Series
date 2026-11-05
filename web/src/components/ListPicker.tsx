"use client";

import { useState } from "react";
import { Check, Lock, Plus } from "lucide-react";
import { api, ApiError } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useApi } from "@/lib/hooks";
import type { ListSummary } from "@/lib/types";
import { Button, ErrorText, inputClass, Modal, Spinner } from "./ui";

export function ListPicker({ titleId, titleName, onClose }: { titleId: number; titleName: string; onClose: () => void }) {
  const { me } = useAuth();
  const lists = useApi<ListSummary[]>(me ? `/users/${me.username}/lists` : null);
  const [added, setAdded] = useState<number[]>([]);
  const [name, setName] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState<number | "new" | null>(null);

  async function add(listId: number) {
    setBusy(listId);
    setError("");
    try {
      await api(`/me/lists/${listId}/items`, { method: "POST", body: { titleId } });
      setAdded((a) => [...a, listId]);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Algo deu errado.");
    } finally {
      setBusy(null);
    }
  }

  async function create(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    setBusy("new");
    setError("");
    try {
      const list = await api<{ id: number }>("/me/lists", { method: "POST", body: { name: name.trim(), isPublic: true } });
      await api(`/me/lists/${list.id}/items`, { method: "POST", body: { titleId } });
      setAdded((a) => [...a, list.id]);
      setName("");
      lists.reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Algo deu errado.");
    } finally {
      setBusy(null);
    }
  }

  return (
    <Modal open onClose={onClose} title={<span>Adicionar “{titleName}” a uma lista</span>}>
      <div className="space-y-4">
        <form onSubmit={create} className="flex gap-2">
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nova lista" maxLength={100} className={inputClass} />
          <Button type="submit" variant="primary" busy={busy === "new"} disabled={!name.trim()}>
            <Plus className="size-4" />
            Criar
          </Button>
        </form>
        <ErrorText>{error}</ErrorText>
        {!lists.data ? (
          <Spinner className="py-6" />
        ) : lists.data.length === 0 ? (
          <p className="py-4 text-center text-sm text-muted">Você ainda não tem listas.</p>
        ) : (
          <ul className="divide-y divide-line">
            {lists.data.map((list) => (
              <li key={list.id} className="flex items-center gap-3 py-2.5">
                <div className="min-w-0 flex-1">
                  <p className="flex items-center gap-1.5 truncate font-medium">
                    {list.name}
                    {!list.isPublic ? <Lock className="size-3.5 text-faint" /> : null}
                  </p>
                  <p className="text-xs text-faint">{list.itemCount} títulos</p>
                </div>
                {added.includes(list.id) ? (
                  <span className="flex items-center gap-1 text-sm text-ok">
                    <Check className="size-4" /> Adicionado
                  </span>
                ) : (
                  <Button size="sm" onClick={() => add(list.id)} busy={busy === list.id}>
                    Adicionar
                  </Button>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </Modal>
  );
}
