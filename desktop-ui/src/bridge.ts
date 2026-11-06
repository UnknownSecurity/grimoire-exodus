export const protocolVersion = "1.0";

export interface BridgeError {
  code: string;
  message: string;
  retryable: boolean;
}

export interface BridgeEvent<T = unknown> {
  protocol: string;
  event: string;
  sequence: number;
  payload: T;
}

interface BridgeResponse<T> {
  protocol: string;
  id: string;
  ok: boolean;
  result?: T;
  error?: BridgeError;
}

interface WebViewChannel {
  postMessage(message: unknown): void;
  addEventListener(type: "message", listener: (event: MessageEvent) => void): void;
}

declare global {
  interface Window {
    chrome?: { webview?: WebViewChannel };
  }
}

type PendingRequest = {
  resolve: (value: unknown) => void;
  reject: (reason: Error) => void;
  timer: number;
};

class DesktopBridge {
  private counter = 0;
  private readonly pending = new Map<string, PendingRequest>();
  private readonly eventListeners = new Set<(event: BridgeEvent) => void>();

  constructor() {
    window.chrome?.webview?.addEventListener("message", (event) => this.receive(event.data));
  }

  get available(): boolean {
    return Boolean(window.chrome?.webview);
  }

  onEvent(listener: (event: BridgeEvent) => void): () => void {
    this.eventListeners.add(listener);
    return () => this.eventListeners.delete(listener);
  }

  request<T>(method: string, params: Record<string, unknown> = {}): Promise<T> {
    if (!this.available) {
      return Promise.reject(new Error("The desktop bridge is not available."));
    }

    const id = `ui-${Date.now()}-${++this.counter}`;
    return new Promise<T>((resolve, reject) => {
      const timer = window.setTimeout(() => {
        this.pending.delete(id);
        reject(new Error("Grimoire did not answer the UI request in time."));
      }, 10000);
      this.pending.set(id, {
        resolve: resolve as (value: unknown) => void,
        reject,
        timer
      });
      window.chrome!.webview!.postMessage({
        protocol: protocolVersion,
        id,
        method,
        params
      });
    });
  }

  private receive(message: unknown): void {
    if (!message || typeof message !== "object") return;
    const value = message as Partial<BridgeResponse<unknown> & BridgeEvent>;
    if (typeof value.event === "string") {
      this.eventListeners.forEach((listener) => listener(value as BridgeEvent));
      return;
    }
    if (typeof value.id !== "string") return;
    const pending = this.pending.get(value.id);
    if (!pending) return;
    window.clearTimeout(pending.timer);
    this.pending.delete(value.id);
    if (value.ok) {
      pending.resolve(value.result);
      return;
    }
    pending.reject(new Error(value.error?.message ?? "The Grimoire action failed."));
  }
}

export const bridge = new DesktopBridge();
