const baseUrl = (process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000").replace(/\/$/, "");
const tokenKey = "maratona.token";

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}

export function getToken() {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(tokenKey);
}

export function setToken(token: string | null) {
  if (token) window.localStorage.setItem(tokenKey, token);
  else window.localStorage.removeItem(tokenKey);
  window.dispatchEvent(new Event(tokenEvent));
}

export const tokenEvent = "maratona:token";

const statusMessages: Record<number, string> = {
  401: "Você precisa entrar na sua conta.",
  403: "Você não tem permissão para isso.",
  404: "Não encontrado.",
  429: "Muitas requisições. Espere um pouco e tente de novo.",
  502: "Um serviço externo não respondeu. Tente de novo em instantes.",
};

async function errorMessage(response: Response) {
  try {
    const body = await response.json();
    const errors = body?.errors as Record<string, string[]> | undefined;
    const first = errors ? Object.values(errors).flat()[0] : undefined;
    return first ?? body?.detail ?? body?.title ?? statusMessages[response.status] ?? "Algo deu errado.";
  } catch {
    return statusMessages[response.status] ?? "Algo deu errado.";
  }
}

export async function api<T>(path: string, init: { method?: string; body?: unknown; keepSession?: boolean } = {}): Promise<T> {
  const headers: Record<string, string> = {};
  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (init.body !== undefined) headers["Content-Type"] = "application/json";
  let response: Response;
  try {
    response = await fetch(`${baseUrl}${path}`, {
      method: init.method ?? "GET",
      headers,
      body: init.body === undefined ? undefined : JSON.stringify(init.body),
    });
  } catch {
    throw new ApiError(0, "Não foi possível falar com o servidor. Ele pode estar acordando, tente de novo em alguns segundos.");
  }
  if (response.status === 401 && token && !init.keepSession) setToken(null);
  if (!response.ok) throw new ApiError(response.status, await errorMessage(response));
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export function query(params: Record<string, string | number | null | undefined>) {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== null && value !== undefined && value !== "") search.set(key, String(value));
  }
  const text = search.toString();
  return text ? `?${text}` : "";
}
