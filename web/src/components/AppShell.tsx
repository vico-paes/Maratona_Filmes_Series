"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Clapperboard, House, Library, MessagesSquare, Settings, UserRound } from "lucide-react";
import { useAuth } from "@/lib/auth";
import { SearchBox } from "./SearchBox";
import { Avatar, cx } from "./ui";

function useTabs() {
  const { me } = useAuth();
  return [
    { href: "/", label: "Início", icon: House, match: (p: string) => p === "/" || p.startsWith("/busca") },
    { href: "/biblioteca", label: "Biblioteca", icon: Library, match: (p: string) => p.startsWith("/biblioteca") },
    { href: "/feed", label: "Feed", icon: MessagesSquare, match: (p: string) => p.startsWith("/feed") },
    {
      href: me ? `/u/${me.username}` : "/entrar",
      label: "Perfil",
      icon: UserRound,
      match: (p: string) => (me ? p.startsWith(`/u/${me.username}`) : p.startsWith("/entrar")),
    },
    { href: "/configuracoes", label: "Configurações", icon: Settings, match: (p: string) => p.startsWith("/configuracoes") },
  ];
}

function Logo() {
  return (
    <Link href="/" className="flex shrink-0 items-center gap-2 font-bold">
      <span className="flex size-9 items-center justify-center rounded-xl bg-gradient-to-br from-accent to-accent-strong text-white shadow-lg shadow-accent/25">
        <Clapperboard className="size-5" />
      </span>
      <span className="hidden text-lg tracking-tight sm:inline">Maratona</span>
    </Link>
  );
}

function Footer() {
  return (
    <footer className="mt-16 border-t border-line px-4 pb-28 pt-8 text-xs text-faint md:pb-10">
      <div className="mx-auto flex max-w-6xl flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <a href="https://www.themoviedb.org/" target="_blank" rel="noreferrer" className="flex items-center gap-3">
          <img
            src="https://www.themoviedb.org/assets/2/v4/logos/v2/blue_short-8e7b30f73a4020692ccca9c88bafe5dcb6f8a62a4c6bc55cd9ba82bb2cd95f6c.svg"
            alt="TMDB"
            className="h-4 w-auto"
          />
          <span>This product uses the TMDB API but is not endorsed or certified by TMDB.</span>
        </a>
        <p>
          Notas externas via{" "}
          <a href="https://www.omdbapi.com/" target="_blank" rel="noreferrer" className="underline hover:text-muted">
            OMDb
          </a>
          . Prêmios via{" "}
          <a href="https://github.com/DLu/oscar_data" target="_blank" rel="noreferrer" className="underline hover:text-muted">
            oscar_data
          </a>{" "}
          e{" "}
          <a href="https://www.wikidata.org/" target="_blank" rel="noreferrer" className="underline hover:text-muted">
            Wikidata
          </a>
          .
        </p>
      </div>
    </footer>
  );
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const tabs = useTabs();
  const { me, ready } = useAuth();

  return (
    <div className="flex min-h-dvh flex-col">
      <header className="sticky top-0 z-30 border-b border-line bg-bg/85 backdrop-blur-lg">
        <div className="mx-auto flex h-16 max-w-6xl items-center gap-3 px-4">
          <Logo />
          <nav className="ml-4 hidden items-center gap-1 md:flex">
            {tabs.slice(0, 3).map((tab) => (
              <Link
                key={tab.label}
                href={tab.href}
                className={cx(
                  "rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                  tab.match(pathname) ? "bg-surface-2 text-fg" : "text-muted hover:text-fg",
                )}
              >
                {tab.label}
              </Link>
            ))}
          </nav>
          <SearchBox className="ml-auto w-full max-w-sm" />
          <div className="hidden items-center gap-1 md:flex">
            <Link
              href="/configuracoes"
              aria-label="Configurações"
              className={cx("rounded-lg p-2", pathname.startsWith("/configuracoes") ? "bg-surface-2 text-fg" : "text-muted hover:text-fg")}
            >
              <Settings className="size-5" />
            </Link>
            {!ready ? (
              <div className="size-9" />
            ) : me ? (
              <Link href={`/u/${me.username}`} aria-label="Meu perfil" className="rounded-full ring-2 ring-transparent hover:ring-accent">
                <Avatar url={me.avatarUrl} name={me.displayName} size={36} />
              </Link>
            ) : (
              <Link href="/entrar" className="rounded-xl bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent-strong">
                Entrar
              </Link>
            )}
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">{children}</main>

      <Footer />

      <nav className="fixed inset-x-3 bottom-3 z-30 flex justify-around rounded-2xl border border-line bg-surface/95 px-1 py-1.5 shadow-2xl backdrop-blur-lg md:hidden">
        {tabs.map((tab) => {
          const active = tab.match(pathname);
          const Icon = tab.icon;
          return (
            <Link
              key={tab.label}
              href={tab.href}
              className={cx(
                "flex min-w-0 flex-1 flex-col items-center gap-0.5 rounded-xl px-1 py-1.5 text-[10px] font-medium",
                active ? "bg-accent-soft text-accent" : "text-muted",
              )}
            >
              <Icon className="size-5" />
              <span className="truncate">{tab.label}</span>
            </Link>
          );
        })}
      </nav>
    </div>
  );
}
