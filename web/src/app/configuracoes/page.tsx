"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { ArrowRight, LogOut, Star } from "lucide-react";
import { RatingDisplay } from "@/components/Rating";
import { Avatar, Button, cx, ErrorText, Field, inputClass, LoginPrompt, Modal, Spinner } from "@/components/ui";
import { api, ApiError } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import type { ConversionExample, ConversionPreview, Me, RatingStyle } from "@/lib/types";

function Card({ title, description, children }: { title: string; description?: string; children: React.ReactNode }) {
  return (
    <section className="rounded-2xl border border-line bg-surface p-5">
      <h2 className="text-lg font-semibold">{title}</h2>
      {description ? <p className="mt-0.5 text-sm text-muted">{description}</p> : null}
      <div className="mt-4">{children}</div>
    </section>
  );
}

function message(err: unknown) {
  return err instanceof ApiError ? err.message : "Algo deu errado.";
}

function profileBody(me: Me) {
  return { displayName: me.displayName, bio: me.bio, avatarUrl: me.avatarUrl, coverUrl: me.coverUrl, ratingStyle: me.ratingStyle };
}

function ProfileForm({ me }: { me: Me }) {
  const { setMe } = useAuth();
  const [form, setForm] = useState({ displayName: me.displayName, bio: me.bio ?? "", avatarUrl: me.avatarUrl ?? "", coverUrl: me.coverUrl ?? "" });
  const [state, setState] = useState<{ busy: boolean; error: string; saved: boolean }>({ busy: false, error: "", saved: false });
  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => setForm({ ...form, [key]: e.target.value });

  async function save(e: React.FormEvent) {
    e.preventDefault();
    setState({ busy: true, error: "", saved: false });
    try {
      const updated = await api<Me>("/me/profile", {
        method: "PUT",
        body: {
          ...profileBody(me),
          displayName: form.displayName.trim(),
          bio: form.bio.trim() || null,
          avatarUrl: form.avatarUrl.trim() || null,
          coverUrl: form.coverUrl.trim() || null,
        },
      });
      setMe(updated);
      setState({ busy: false, error: "", saved: true });
    } catch (err) {
      setState({ busy: false, error: message(err), saved: false });
    }
  }

  return (
    <form onSubmit={save} className="space-y-4">
      <div className="overflow-hidden rounded-xl border border-line">
        <div className="h-24 bg-gradient-to-br from-accent-strong/60 via-accent-soft to-bg">
          {form.coverUrl.startsWith("https://") ? <img src={form.coverUrl} alt="" className="size-full object-cover" /> : null}
        </div>
        <div className="-mt-8 flex items-end gap-3 px-4 pb-3">
          <Avatar url={form.avatarUrl.startsWith("https://") ? form.avatarUrl : null} name={form.displayName || me.username} size={64} className="ring-4 ring-surface" />
          <div className="min-w-0 pb-1">
            <p className="truncate font-semibold">{form.displayName || me.username}</p>
            <p className="text-xs text-faint">@{me.username}</p>
          </div>
        </div>
      </div>
      <Field label="Nome">
        <input className={inputClass} value={form.displayName} onChange={set("displayName")} maxLength={60} required />
      </Field>
      <Field label="Bio">
        <textarea className={inputClass} rows={3} value={form.bio} onChange={set("bio")} maxLength={500} />
      </Field>
      <Field label="Link da foto de perfil" hint="Cole um link https de uma imagem. O Maratona não guarda arquivos.">
        <input className={inputClass} value={form.avatarUrl} onChange={set("avatarUrl")} maxLength={500} placeholder="https://…" inputMode="url" />
      </Field>
      <Field label="Link da capa" hint="Imagem larga, como um banner.">
        <input className={inputClass} value={form.coverUrl} onChange={set("coverUrl")} maxLength={500} placeholder="https://…" inputMode="url" />
      </Field>
      <ErrorText>{state.error}</ErrorText>
      <div className="flex items-center gap-3">
        <Button type="submit" variant="primary" busy={state.busy}>
          Salvar perfil
        </Button>
        {state.saved ? <span className="text-sm text-ok">Salvo!</span> : null}
      </div>
    </form>
  );
}

function ExampleRow({ example }: { example: ConversionExample }) {
  return (
    <li className="flex items-center gap-3 py-2 text-sm">
      <span className="min-w-0 flex-1 truncate text-muted">
        {example.title}
        {example.seasonNumber !== null ? ` · T${example.seasonNumber}` : ""}
      </span>
      <RatingDisplay rating={example.from} style={example.fromStyle} size="xs" />
      <ArrowRight className="size-4 shrink-0 text-faint" />
      <RatingDisplay rating={example.to} style={example.toStyle} size="xs" />
    </li>
  );
}

function StyleForm({ me }: { me: Me }) {
  const { setMe } = useAuth();
  const [preview, setPreview] = useState<ConversionPreview | null>(null);
  const [busy, setBusy] = useState<RatingStyle | "convert" | "keep" | null>(null);
  const [error, setError] = useState("");

  async function choose(style: RatingStyle) {
    if (style === me.ratingStyle) return;
    setBusy(style);
    setError("");
    try {
      setPreview(await api<ConversionPreview>(`/me/rating-conversion?to=${style}`));
    } catch (err) {
      setError(message(err));
    } finally {
      setBusy(null);
    }
  }

  async function confirm(convert: boolean) {
    if (!preview) return;
    setBusy(convert ? "convert" : "keep");
    setError("");
    try {
      const updated = await api<Me>("/me/profile", { method: "PUT", body: { ...profileBody(me), ratingStyle: preview.to, convertRatings: convert } });
      setMe(updated);
      setPreview(null);
    } catch (err) {
      setError(message(err));
    } finally {
      setBusy(null);
    }
  }

  const options: { value: RatingStyle; title: string; text: string; sample: React.ReactNode }[] = [
    { value: "stars", title: "Estrelas", text: "De meia a 5 estrelas, em meias estrelas.", sample: <RatingDisplay rating={4.5} style="stars" size="md" /> },
    { value: "numeric", title: "Número", text: "De 0 a 10, de 0,5 em 0,5.", sample: <RatingDisplay rating={8.5} style="numeric" size="md" /> },
  ];

  return (
    <>
      <div className="grid gap-3 sm:grid-cols-2">
        {options.map((option) => (
          <button
            key={option.value}
            type="button"
            onClick={() => choose(option.value)}
            disabled={busy !== null}
            className={cx(
              "flex flex-col items-start gap-2 rounded-xl border p-4 text-left transition-colors",
              me.ratingStyle === option.value ? "border-accent bg-accent-soft/50" : "border-line bg-surface-2 hover:border-accent/50",
            )}
          >
            <span className="flex w-full items-center justify-between font-medium">
              {option.title}
              {me.ratingStyle === option.value ? <span className="text-xs font-normal text-accent">Atual</span> : null}
            </span>
            {option.sample}
            <span className="text-xs text-muted">{option.text}</span>
          </button>
        ))}
      </div>
      <p className="mt-3 text-xs text-faint">
        Cada nota aparece sempre no estilo em que foi dada. Na janela de review você pode trocar o estilo só daquela nota.
      </p>
      <ErrorText>{preview ? "" : error}</ErrorText>
      {preview ? (
        <Modal
          open
          onClose={() => setPreview(null)}
          title={`Mudar para ${preview.to === "stars" ? "estrelas" : "número"}`}
          footer={
            preview.count > 0 ? (
              <div className="flex flex-col gap-2 sm:flex-row">
                <Button onClick={() => setPreview(null)} className="sm:flex-1">
                  Cancelar
                </Button>
                <Button onClick={() => confirm(false)} busy={busy === "keep"} className="sm:flex-1">
                  Não converter
                </Button>
                <Button variant="primary" onClick={() => confirm(true)} busy={busy === "convert"} className="sm:flex-[1.4]">
                  Converter {preview.count} {preview.count === 1 ? "nota" : "notas"}
                </Button>
              </div>
            ) : (
              <div className="flex gap-3">
                <Button onClick={() => setPreview(null)} className="flex-1">
                  Cancelar
                </Button>
                <Button variant="primary" onClick={() => confirm(false)} busy={busy === "keep"} className="flex-1">
                  Mudar estilo
                </Button>
              </div>
            )
          }
        >
          <div className="space-y-4 text-sm">
            {preview.count > 0 ? (
              <>
                <p>
                  Você tem <strong>{preview.count}</strong> {preview.count === 1 ? "nota" : "notas"} em {preview.to === "stars" ? "número" : "estrelas"}. Quer converter para{" "}
                  {preview.to === "stars" ? "estrelas" : "número"}?
                </p>
                <div className="rounded-xl bg-surface-2 p-3">
                  <p className="font-medium">Como funciona</p>
                  <p className="mt-1 text-muted">{preview.rule}</p>
                  <ul className="mt-2 divide-y divide-line">
                    {preview.samples.map((s, i) => (
                      <ExampleRow key={i} example={s} />
                    ))}
                  </ul>
                </div>
                {preview.examples.length > 0 ? (
                  <div>
                    <p className="font-medium">Com as suas notas</p>
                    <ul className="divide-y divide-line">
                      {preview.examples.map((s, i) => (
                        <ExampleRow key={i} example={s} />
                      ))}
                    </ul>
                  </div>
                ) : null}
                <p className="text-xs text-faint">Se não converter, as notas antigas continuam no estilo em que foram dadas e só as novas usam o novo estilo.</p>
              </>
            ) : (
              <p className="flex items-center gap-2">
                <Star className="size-4 text-accent" />
                Você não tem notas para converter. As próximas notas vão usar o novo estilo.
              </p>
            )}
            <ErrorText>{error}</ErrorText>
          </div>
        </Modal>
      ) : null}
    </>
  );
}

function PasswordForm() {
  const [form, setForm] = useState({ currentPassword: "", newPassword: "" });
  const [state, setState] = useState({ busy: false, error: "", saved: false });

  async function save(e: React.FormEvent) {
    e.preventDefault();
    setState({ busy: true, error: "", saved: false });
    try {
      await api("/me/password", { method: "PUT", body: form, keepSession: true });
      setForm({ currentPassword: "", newPassword: "" });
      setState({ busy: false, error: "", saved: true });
    } catch (err) {
      setState({ busy: false, error: message(err), saved: false });
    }
  }

  return (
    <form onSubmit={save} className="space-y-4">
      <Field label="Senha atual">
        <input
          className={inputClass}
          type="password"
          autoComplete="current-password"
          value={form.currentPassword}
          onChange={(e) => setForm({ ...form, currentPassword: e.target.value })}
          required
          maxLength={100}
        />
      </Field>
      <Field label="Nova senha" hint="Pelo menos 8 caracteres.">
        <input
          className={inputClass}
          type="password"
          autoComplete="new-password"
          value={form.newPassword}
          onChange={(e) => setForm({ ...form, newPassword: e.target.value })}
          required
          minLength={8}
          maxLength={100}
        />
      </Field>
      <ErrorText>{state.error}</ErrorText>
      <div className="flex items-center gap-3">
        <Button type="submit" busy={state.busy}>
          Trocar senha
        </Button>
        {state.saved ? <span className="text-sm text-ok">Senha trocada!</span> : null}
      </div>
    </form>
  );
}

function AccountForm() {
  const router = useRouter();
  const { signOut } = useAuth();
  const [open, setOpen] = useState(false);
  const [password, setPassword] = useState("");
  const [state, setState] = useState({ busy: false, error: "" });

  async function remove() {
    setState({ busy: true, error: "" });
    try {
      await api("/me/delete", { method: "POST", body: { password }, keepSession: true });
      signOut();
      router.replace("/");
    } catch (err) {
      setState({ busy: false, error: message(err) });
    }
  }

  return (
    <div className="flex flex-wrap gap-3">
      <Button
        onClick={() => {
          signOut();
          router.replace("/");
        }}
      >
        <LogOut className="size-4" />
        Sair
      </Button>
      <Button variant="danger" onClick={() => setOpen(true)}>
        Excluir conta
      </Button>
      {open ? (
        <Modal
          open
          onClose={() => setOpen(false)}
          title="Excluir conta"
          footer={
            <div className="flex gap-3">
              <Button onClick={() => setOpen(false)} className="flex-1">
                Cancelar
              </Button>
              <Button variant="danger" onClick={remove} busy={state.busy} disabled={!password} className="flex-1">
                Excluir para sempre
              </Button>
            </div>
          }
        >
          <div className="space-y-4">
            <p className="text-sm text-muted">Tudo será apagado: biblioteca, notas, reviews, listas e seguidores. Não dá para desfazer.</p>
            <Field label="Confirme sua senha">
              <input className={inputClass} type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} maxLength={100} />
            </Field>
            <ErrorText>{state.error}</ErrorText>
          </div>
        </Modal>
      ) : null}
    </div>
  );
}

export default function SettingsPage() {
  const { me, ready } = useAuth();
  if (!ready) return <Spinner />;
  if (!me) return <LoginPrompt text="Entre para editar seu perfil e suas preferências." />;
  return (
    <div className="mx-auto max-w-2xl space-y-5">
      <title>Configurações · Maratona</title>
      <h1 className="text-2xl font-bold">Configurações</h1>
      <Card title="Perfil" description="Como as outras pessoas veem você.">
        <ProfileForm me={me} />
      </Card>
      <Card title="Estilo de nota" description="Como você prefere dar notas daqui pra frente.">
        <StyleForm me={me} />
      </Card>
      <Card title="Senha">
        <PasswordForm />
      </Card>
      <Card title="Conta" description={me.email}>
        <AccountForm />
      </Card>
    </div>
  );
}
