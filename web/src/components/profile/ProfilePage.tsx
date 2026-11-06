"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { Clock, Crown, Film, Layers, ListVideo, MessageSquareText, Pencil, Tv } from "lucide-react";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { formatDate, imageUrl } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { Favorite, ListSummary, MediaType, Page, Profile, UserSummary } from "@/lib/types";
import { Poster, PosterCard } from "../PosterCard";
import { PosterRow } from "../PosterRow";
import { RatingDisplay } from "../Rating";
import { ReviewFeed } from "../ReviewFeed";
import { Avatar, Button, cx, Message, Modal, SectionTitle, Segmented, Spinner } from "../ui";
import { FavoritesEditor } from "./FavoritesEditor";

function People({ username, kind, onClose }: { username: string; kind: "followers" | "following"; onClose: () => void }) {
  const { data } = useApi<Page<UserSummary>>(`/users/${username}/${kind}`);
  return (
    <Modal open onClose={onClose} title={kind === "followers" ? "Seguidores" : "Seguindo"}>
      {!data ? (
        <Spinner className="py-8" />
      ) : data.items.length === 0 ? (
        <p className="py-6 text-center text-sm text-muted">Ninguém por aqui ainda.</p>
      ) : (
        <ul className="space-y-1">
          {data.items.map((user) => (
            <li key={user.username}>
              <Link href={`/u/${user.username}`} onClick={onClose} className="flex items-center gap-3 rounded-xl p-2 hover:bg-surface-2">
                <Avatar url={user.avatarUrl} name={user.displayName} size={40} />
                <div className="min-w-0">
                  <p className="truncate font-medium">{user.displayName}</p>
                  <p className="truncate text-sm text-faint">@{user.username}</p>
                </div>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </Modal>
  );
}

function Favorites({ title, type, items, onEdit }: { title: string; type: MediaType; items: Favorite[]; onEdit?: () => void }) {
  if (items.length === 0 && !onEdit) return null;
  return (
    <section>
      <SectionTitle
        action={
          onEdit ? (
            <button type="button" onClick={onEdit} className="flex items-center gap-1.5 rounded-lg px-2 py-1 text-sm text-muted hover:bg-surface-2 hover:text-fg">
              <Pencil className="size-3.5" /> Editar
            </button>
          ) : null
        }
      >
        {title}
      </SectionTitle>
      {items.length === 0 ? (
        <button type="button" onClick={onEdit} className="w-full rounded-2xl border border-dashed border-line py-8 text-sm text-muted hover:border-accent hover:text-fg">
          Escolha seus 4 {type === "movie" ? "filmes" : "séries"} favoritos
        </button>
      ) : (
        <div className="grid grid-cols-4 gap-2 sm:gap-3">
          {items.map((fav) => (
            <div key={fav.titleId} className="relative pt-3">
              {fav.crown ? (
                <Crown className="absolute left-1/2 top-0 z-10 size-6 -translate-x-1/2 fill-gold text-gold-deep drop-shadow-[0_2px_6px_rgba(242,193,78,0.6)]" />
              ) : null}
              <PosterCard title={fav.title} showName={false} />
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function Stat({ icon: Icon, value, label }: { icon: typeof Film; value: number; label: string }) {
  return (
    <div className="flex min-w-0 flex-col items-center gap-0.5 px-2">
      <span className="flex items-center gap-1.5 text-lg font-bold sm:text-xl">
        <Icon className="size-4 text-accent" />
        {value >= 10000 ? `${(value / 1000).toLocaleString("pt-BR", { maximumFractionDigits: 1 })}k` : value.toLocaleString("pt-BR")}
      </span>
      <span className="truncate text-xs text-faint">{label}</span>
    </div>
  );
}

function Lists({ username }: { username: string }) {
  const { data } = useApi<ListSummary[]>(`/users/${username}/lists`);
  if (!data) return <Spinner />;
  if (data.length === 0) return <Message title="Nenhuma lista ainda" />;
  return (
    <div className="grid gap-3 sm:grid-cols-2">
      {data.map((list) => (
        <Link key={list.id} href={`/listas/${list.id}`} className="flex gap-3 rounded-2xl border border-line bg-surface p-3 hover:border-accent/60">
          <div className="flex w-28 shrink-0 -space-x-6">
            {[0, 1, 2].map((i) => {
              const url = imageUrl(list.posters[i] ?? null, "w185");
              return (
                <div key={i} className="aspect-[2/3] w-12 overflow-hidden rounded-md bg-surface-3 ring-2 ring-surface" style={{ zIndex: 3 - i }}>
                  {url ? <img src={url} alt="" loading="lazy" className="size-full object-cover" /> : null}
                </div>
              );
            })}
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate font-semibold">{list.name}</p>
            <p className="text-xs text-faint">
              {list.itemCount} títulos{list.isPublic ? "" : " · privada"}
            </p>
            {list.description ? <p className="mt-1 line-clamp-2 text-xs text-muted">{list.description}</p> : null}
          </div>
        </Link>
      ))}
    </div>
  );
}

function Overview({ profile, onEditFavorites, onShowReviews }: { profile: Profile; onEditFavorites?: () => void; onShowReviews: () => void }) {
  return (
    <div className="space-y-8">
      <Favorites title="Filmes favoritos" type="movie" items={profile.favoriteMovies} onEdit={onEditFavorites} />
      <Favorites title="Séries favoritas" type="tv" items={profile.favoriteSeries} onEdit={onEditFavorites} />
      {profile.recentlyWatched.length > 0 ? (
        <section>
          <SectionTitle>Assistidos recentemente</SectionTitle>
          <PosterRow>
            {profile.recentlyWatched.map((item) => (
              <PosterCard
                key={item.titleId}
                title={item.title}
                bottom={
                  <div className="flex h-5 items-center justify-between gap-1 text-[11px] text-faint">
                    <RatingDisplay rating={item.rating} style={item.ratingStyle} size="xs" />
                    <span className="truncate">{formatDate(item.watchedAt, false)}</span>
                  </div>
                }
              />
            ))}
          </PosterRow>
        </section>
      ) : null}
      {profile.recentReviews.length > 0 ? (
        <section>
          <SectionTitle
            action={
              <button type="button" onClick={onShowReviews} className="text-sm font-medium text-accent hover:underline">
                Ver todas
              </button>
            }
          >
            Reviews <span className="text-faint">({profile.stats.reviews})</span>
          </SectionTitle>
          <div className="scrollbar-none -mx-4 flex snap-x gap-3 overflow-x-auto px-4 pb-2">
            {profile.recentReviews.map((review) =>
              review.title ? (
                <Link
                  key={review.id}
                  href={`/${review.title.mediaType === "movie" ? "filme" : "serie"}/${review.title.tmdbId}`}
                  className="flex w-80 shrink-0 snap-start gap-3 rounded-2xl border border-line bg-surface p-3 hover:border-accent/60"
                >
                  <div className="w-16 shrink-0">
                    <Poster path={review.title.posterPath} name={review.title.name} type={review.title.mediaType} />
                  </div>
                  <div className="min-w-0 flex-1 space-y-1">
                    <p className="truncate text-sm font-semibold">
                      {review.title.name}
                      {review.seasonNumber !== null ? <span className="font-normal text-muted"> · T{review.seasonNumber}</span> : null}
                    </p>
                    <RatingDisplay rating={review.rating} style={review.ratingStyle} size="xs" />
                    <p className={cx("line-clamp-3 text-xs", review.spoiler ? "italic text-faint" : "text-muted")}>
                      {review.spoiler ? "Contém spoilers" : review.body}
                    </p>
                  </div>
                </Link>
              ) : null,
            )}
          </div>
        </section>
      ) : null}
      {profile.favoriteMovies.length === 0 && profile.favoriteSeries.length === 0 && profile.recentlyWatched.length === 0 && !onEditFavorites ? (
        <Message title="Nada por aqui ainda">Esta pessoa ainda não marcou nada como assistido.</Message>
      ) : null}
    </div>
  );
}

export function ProfilePage() {
  const { username } = useParams<{ username: string }>();
  const { me } = useAuth();
  const { data: profile, error, reload, mutate } = useApi<Profile>(`/users/${encodeURIComponent(username)}`);
  const [tab, setTab] = useState<"overview" | "reviews" | "lists">("overview");
  const [people, setPeople] = useState<"followers" | "following" | null>(null);
  const [editingFavorites, setEditingFavorites] = useState(false);
  const [following, setFollowing] = useState(false);

  if (error) return <Message title={error.status === 404 ? "Perfil não encontrado" : "Não foi possível carregar"}>{error.message}</Message>;
  if (!profile) return <Spinner />;

  async function toggleFollow() {
    if (!profile) return;
    setFollowing(true);
    try {
      await api(`/users/${profile.username}/follow`, { method: profile.isFollowing ? "DELETE" : "POST" });
      mutate((p) => ({ ...p, isFollowing: !p.isFollowing, followers: p.followers + (p.isFollowing ? -1 : 1) }));
    } finally {
      setFollowing(false);
    }
  }

  const cover = profile.coverUrl;

  return (
    <div className="space-y-6">
      <title>{`${profile.displayName} · Maratona`}</title>
      <div className="relative -mx-4 -mt-6">
        <div className="relative h-44 overflow-hidden sm:h-64">
          {cover ? <img src={cover} alt="" className="size-full object-cover" /> : <div className="size-full bg-gradient-to-br from-accent-strong/60 via-accent-soft to-bg" />}
          <div className="absolute inset-0 bg-gradient-to-t from-bg via-transparent to-transparent" />
        </div>
        <div className="relative -mt-16 flex flex-col items-center px-4 text-center">
          <Avatar url={profile.avatarUrl} name={profile.displayName} size={112} className="ring-4 ring-bg" />
          <h1 className="mt-3 text-2xl font-bold">{profile.displayName}</h1>
          <p className="text-sm text-faint">@{profile.username}</p>
          {profile.bio ? <p className="mt-2 max-w-lg whitespace-pre-line text-sm text-muted">{profile.bio}</p> : null}
          <div className="mt-3 flex items-center gap-5 text-sm">
            <button type="button" onClick={() => setPeople("following")} className="hover:text-accent">
              <span className="font-bold">{profile.following}</span> <span className="text-muted">Seguindo</span>
            </button>
            <button type="button" onClick={() => setPeople("followers")} className="hover:text-accent">
              <span className="font-bold">{profile.followers}</span> <span className="text-muted">{profile.followers === 1 ? "Seguidor" : "Seguidores"}</span>
            </button>
          </div>
          <div className="mt-4">
            {profile.isMe ? (
              <Link href="/configuracoes" className="inline-flex h-10 items-center gap-2 rounded-xl bg-surface-3 px-4 text-sm font-medium hover:bg-line">
                <Pencil className="size-4" />
                Editar perfil
              </Link>
            ) : me ? (
              <Button variant={profile.isFollowing ? "secondary" : "primary"} onClick={toggleFollow} busy={following}>
                {profile.isFollowing ? "Seguindo" : "Seguir"}
              </Button>
            ) : (
              <Link href="/entrar" className="inline-flex h-10 items-center rounded-xl bg-accent px-5 text-sm font-medium text-white hover:bg-accent-strong">
                Seguir
              </Link>
            )}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-y-4 rounded-2xl border border-line bg-surface py-4 sm:grid-cols-5">
        <Stat icon={Film} value={profile.stats.moviesWatched} label="Filmes" />
        <Stat icon={Tv} value={profile.stats.seriesCompleted} label="Séries" />
        <Stat icon={Layers} value={profile.stats.seasonsWatched} label="Temporadas" />
        <Stat icon={Clock} value={profile.stats.hoursWatched} label="Horas" />
        <Stat icon={MessageSquareText} value={profile.stats.reviews} label="Reviews" />
      </div>

      <Segmented
        value={tab}
        onChange={setTab}
        options={[
          { value: "overview", label: "Visão geral" },
          { value: "reviews", label: "Reviews" },
          { value: "lists", label: <span className="flex items-center gap-1.5"><ListVideo className="size-4" />Listas</span> },
        ]}
      />

      {tab === "overview" ? (
        <Overview profile={profile} onEditFavorites={profile.isMe ? () => setEditingFavorites(true) : undefined} onShowReviews={() => setTab("reviews")} />
      ) : tab === "reviews" ? (
        <ReviewFeed path={`/users/${profile.username}/reviews`} empty="Nenhuma review ainda." showUser={false} />
      ) : (
        <Lists username={profile.username} />
      )}

      {people ? <People key={people} username={profile.username} kind={people} onClose={() => setPeople(null)} /> : null}
      {editingFavorites ? (
        <FavoritesEditor movies={profile.favoriteMovies} series={profile.favoriteSeries} onClose={() => setEditingFavorites(false)} onSaved={reload} />
      ) : null}
    </div>
  );
}
