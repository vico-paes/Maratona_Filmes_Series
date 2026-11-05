export type MediaType = "movie" | "tv";
export type RatingStyle = "stars" | "numeric";
export type AwardKind = "oscar" | "emmy";

export type Page<T> = { items: T[]; page: number; totalPages: number; total: number };

export type Ratings = {
  imdb: number | null;
  imdbVotes: number | null;
  rottenTomatoes: number | null;
  metacritic: number | null;
  atualizadoEm: string;
};

export type AwardBadge = { kind: AwardKind; status: "nominated" | "won" | "major"; wins: number; nominations: number };

export type Award = { kind: AwardKind; year: number | null; category: string; person: string | null; won: boolean; isMajor: boolean };

export type TitleCard = {
  tmdbId: number;
  mediaType: MediaType;
  name: string;
  year: number | null;
  posterPath: string | null;
  ratings: Ratings | null;
  award: AwardBadge | null;
};

export type Cast = { name: string; character: string | null; profilePath: string | null };

export type SeasonSummary = {
  id: number;
  seasonNumber: number;
  name: string;
  posterPath: string | null;
  airDate: string | null;
  episodeCount: number;
};

export type TitleDetails = {
  id: number;
  tmdbId: number;
  mediaType: MediaType;
  imdbId: string | null;
  name: string;
  originalName: string | null;
  overview: string | null;
  posterPath: string | null;
  backdropPath: string | null;
  releaseDate: string | null;
  lastAirDate: string | null;
  year: number | null;
  runtime: number | null;
  status: string | null;
  genres: string[];
  credits: { directors: string[]; creators: string[]; writers: string[]; cast: Cast[] };
  seasons: SeasonSummary[];
  ratings: Ratings | null;
  award: AwardBadge | null;
  awards: Award[];
};

export type Episode = {
  id: number;
  episodeNumber: number;
  name: string;
  overview: string | null;
  stillPath: string | null;
  airDate: string | null;
  runtime: number | null;
};

export type SeasonDetails = {
  id: number;
  seasonNumber: number;
  name: string;
  overview: string | null;
  posterPath: string | null;
  airDate: string | null;
  episodes: Episode[];
};

export type Home = { nowPlaying: TitleCard[]; popularMovies: TitleCard[]; onTheAir: TitleCard[]; popularSeries: TitleCard[] };

export type UserSummary = { username: string; displayName: string; avatarUrl: string | null };

export type Me = {
  id: number;
  username: string;
  email: string;
  displayName: string;
  bio: string | null;
  avatarUrl: string | null;
  coverUrl: string | null;
  ratingStyle: RatingStyle;
  createdAt: string;
};

export type Entry = {
  titleId: number;
  inWatchlist: boolean;
  watched: boolean;
  watchedAt: string | null;
  rating: number | null;
  ratingStyle: RatingStyle | null;
};

export type SeasonEntry = {
  seasonId: number;
  seasonNumber: number;
  watched: boolean;
  watchedAt: string | null;
  rating: number | null;
  ratingStyle: RatingStyle | null;
};

export type Review = {
  id: number;
  user: UserSummary;
  titleId: number;
  seasonId: number | null;
  seasonNumber: number | null;
  rating: number | null;
  ratingStyle: RatingStyle | null;
  body: string;
  spoiler: boolean;
  likes: number;
  likedByMe: boolean;
  createdAt: string;
  updatedAt: string;
  title: TitleCard | null;
};

export type Note = { id: number; seasonId: number | null; episodeId: number | null; body: string; updatedAt: string };

export type MyTitleState = { entry: Entry | null; seasons: SeasonEntry[]; reviews: Review[]; notes: Note[] };

export type LibraryItem = {
  titleId: number;
  title: TitleCard;
  inWatchlist: boolean;
  watched: boolean;
  watching: boolean;
  watchedAt: string | null;
  rating: number | null;
  ratingStyle: RatingStyle | null;
  addedAt: string;
  newSeasons: number;
};

export type NewSeason = {
  titleId: number;
  title: TitleCard;
  seasonId: number;
  seasonNumber: number;
  name: string;
  airDate: string | null;
  released: boolean;
};

export type Favorite = { slot: number; crown: boolean; titleId: number; title: TitleCard };

export type WatchedItem = { titleId: number; title: TitleCard; watchedAt: string | null; rating: number | null; ratingStyle: RatingStyle | null };

export type Stats = {
  moviesWatched: number;
  seriesCompleted: number;
  seasonsWatched: number;
  hoursWatched: number;
  reviews: number;
  lists: number;
};

export type Profile = {
  username: string;
  displayName: string;
  bio: string | null;
  avatarUrl: string | null;
  coverUrl: string | null;
  ratingStyle: RatingStyle;
  createdAt: string;
  favoriteMovies: Favorite[];
  favoriteSeries: Favorite[];
  recentlyWatched: WatchedItem[];
  recentReviews: Review[];
  stats: Stats;
  followers: number;
  following: number;
  isMe: boolean;
  isFollowing: boolean;
};

export type ListSummary = {
  id: number;
  name: string;
  description: string | null;
  isPublic: boolean;
  itemCount: number;
  posters: string[];
  updatedAt: string;
};

export type ListDetails = {
  id: number;
  name: string;
  description: string | null;
  isPublic: boolean;
  owner: UserSummary;
  isMine: boolean;
  items: { titleId: number; position: number; title: TitleCard; addedAt: string }[];
  createdAt: string;
  updatedAt: string;
};

export type ConversionExample = {
  title: string;
  seasonNumber: number | null;
  from: number;
  fromStyle: RatingStyle;
  to: number;
  toStyle: RatingStyle;
};

export type ConversionPreview = {
  to: RatingStyle;
  rule: string;
  count: number;
  examples: ConversionExample[];
  samples: ConversionExample[];
};
