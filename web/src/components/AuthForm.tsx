"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { api, ApiError } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import type { Me } from "@/lib/types";
import { Button, ErrorText, Field, inputClass } from "./ui";

type Mode = "login" | "register";

function nextPath() {
  const next = new URLSearchParams(window.location.search).get("next");
  return next && next.startsWith("/") && !next.startsWith("//") ? next : "/";
}

export function AuthForm({ mode }: { mode: Mode }) {
  const router = useRouter();
  const { signIn } = useAuth();
  const [form, setForm] = useState({ login: "", username: "", email: "", displayName: "", password: "" });
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) => setForm({ ...form, [key]: e.target.value });

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      const body =
        mode === "login"
          ? { login: form.login.trim(), password: form.password }
          : { username: form.username.trim(), email: form.email.trim(), displayName: form.displayName.trim() || null, password: form.password };
      const result = await api<{ token: string; user: Me }>(`/auth/${mode}`, { method: "POST", body });
      signIn(result.token, result.user);
      router.replace(nextPath());
    } catch (err) {
      setError(err instanceof ApiError ? (err.status === 401 ? "Usuário ou senha incorretos." : err.message) : "Algo deu errado.");
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-sm py-8">
      <h1 className="text-2xl font-bold">{mode === "login" ? "Entrar" : "Criar conta"}</h1>
      <p className="mt-1 text-sm text-muted">
        {mode === "login" ? "Que bom te ver de novo." : "Comece a catalogar o que você assiste."}
      </p>
      <form onSubmit={submit} className="mt-6 space-y-4">
        {mode === "login" ? (
          <Field label="Usuário ou e-mail">
            <input className={inputClass} value={form.login} onChange={set("login")} autoComplete="username" required maxLength={254} />
          </Field>
        ) : (
          <>
            <Field label="Usuário" hint="De 3 a 30 letras, números, ponto ou _.">
              <input
                className={inputClass}
                value={form.username}
                onChange={set("username")}
                autoComplete="username"
                required
                pattern="[a-zA-Z0-9_.]{3,30}"
                maxLength={30}
              />
            </Field>
            <Field label="Nome de exibição">
              <input className={inputClass} value={form.displayName} onChange={set("displayName")} maxLength={60} />
            </Field>
            <Field label="E-mail">
              <input className={inputClass} type="email" value={form.email} onChange={set("email")} autoComplete="email" required maxLength={254} />
            </Field>
          </>
        )}
        <Field label="Senha" hint={mode === "register" ? "Pelo menos 8 caracteres." : undefined}>
          <input
            className={inputClass}
            type="password"
            value={form.password}
            onChange={set("password")}
            autoComplete={mode === "login" ? "current-password" : "new-password"}
            required
            minLength={mode === "register" ? 8 : undefined}
            maxLength={100}
          />
        </Field>
        <ErrorText>{error}</ErrorText>
        <Button type="submit" variant="primary" busy={busy} className="w-full">
          {mode === "login" ? "Entrar" : "Criar conta"}
        </Button>
      </form>
      <p className="mt-6 text-center text-sm text-muted">
        {mode === "login" ? "Ainda não tem conta? " : "Já tem conta? "}
        <Link href={mode === "login" ? "/cadastro" : "/entrar"} className="font-medium text-accent hover:underline">
          {mode === "login" ? "Criar conta" : "Entrar"}
        </Link>
      </p>
    </div>
  );
}
