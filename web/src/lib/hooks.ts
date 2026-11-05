"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { api, ApiError } from "./api";

type State<T> = { key: string | null; data?: T; error?: ApiError };

export function useApi<T>(path: string | null) {
  const [state, setState] = useState<State<T>>({ key: null });
  const [version, setVersion] = useState(0);

  useEffect(() => {
    if (!path) return;
    let active = true;
    api<T>(path).then(
      (data) => active && setState({ key: path, data }),
      (error) => active && setState({ key: path, error: error instanceof ApiError ? error : new ApiError(0, "Algo deu errado.") }),
    );
    return () => {
      active = false;
    };
  }, [path, version]);

  const current = path !== null && state.key === path;
  const reload = useCallback(() => setVersion((v) => v + 1), []);
  const mutate = useCallback((update: (data: T) => T) => setState((s) => (s.data === undefined ? s : { ...s, data: update(s.data) })), []);

  return {
    data: current ? state.data : undefined,
    error: current ? state.error : undefined,
    loading: path !== null && !current,
    reload,
    mutate,
  };
}

export function useDebounced<T>(value: T, delay: number) {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);
  return debounced;
}

export function useAutosave(save: (value: string) => Promise<void>, delay = 800) {
  const [status, setStatus] = useState<"idle" | "pending" | "saving" | "saved" | "error">("idle");
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const latest = useRef<string | null>(null);
  const saveRef = useRef(save);

  useEffect(() => {
    saveRef.current = save;
  }, [save]);

  const flush = useCallback(async () => {
    if (timer.current) clearTimeout(timer.current);
    timer.current = null;
    const value = latest.current;
    if (value === null) return;
    latest.current = null;
    setStatus("saving");
    try {
      await saveRef.current(value);
      setStatus(latest.current === null ? "saved" : "pending");
    } catch {
      latest.current = latest.current ?? value;
      setStatus("error");
    }
  }, []);

  const schedule = useCallback(
    (value: string) => {
      latest.current = value;
      setStatus("pending");
      if (timer.current) clearTimeout(timer.current);
      timer.current = setTimeout(flush, delay);
    },
    [delay, flush],
  );

  useEffect(() => () => void flush(), [flush]);

  return { status, schedule, flush };
}
