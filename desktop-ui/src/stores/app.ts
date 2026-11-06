import { defineStore } from "pinia";

export interface ClientSnapshot {
  id: string;
  title: string;
  state: string;
  engine: string;
  isActive: boolean;
  isLoggedIn: boolean;
  loadProgress: number;
  ping: number | null;
  botRunning: boolean;
  botName: string;
  botIndex: number;
  commandCount: number;
}

export interface BotSnapshot {
  running: boolean;
  name: string;
  index: number;
  commandCount: number;
  activeAction: string | null;
  runtime: string;
}

export interface ClientOptionsSnapshot {
  infiniteRange: boolean;
  provokeCell: boolean;
  provokeMap: boolean;
  targetMagnet: boolean;
  lagKiller: boolean;
  hidePlayers: boolean;
  skipCutscenes: boolean;
  disableAnimations: boolean;
  untargetSelf: boolean;
  autoSaveState: boolean;
  fastWalk: boolean;
}

export interface ClientLocationSnapshot {
  available: boolean;
  cell: string;
  pad: string;
  cells: string[];
  pads: string[];
}

export interface AppSnapshot {
  version: string;
  homeVisible: boolean;
  hideDeviceStats: boolean;
  windowState: "Normal" | "Minimized" | "Maximized";
  activeSessionId: string | null;
  multiClientEnabled: boolean;
  canCreateClient: boolean;
  sessions: ClientSnapshot[];
  activeBot: BotSnapshot;
  location: ClientLocationSnapshot;
  options: ClientOptionsSnapshot;
}

const emptyState: AppSnapshot = {
  version: "Development",
  homeVisible: true,
  hideDeviceStats: false,
  windowState: "Normal",
  activeSessionId: null,
  multiClientEnabled: false,
  canCreateClient: false,
  sessions: [],
  activeBot: {
    running: false,
    name: "Untitled Bot",
    index: 0,
    commandCount: 0,
    activeAction: null,
    runtime: "00:00:00"
  },
  location: {
    available: false,
    cell: "",
    pad: "",
    cells: [],
    pads: []
  },
  options: {
    infiniteRange: false,
    provokeCell: false,
    provokeMap: false,
    targetMagnet: false,
    lagKiller: false,
    hidePlayers: false,
    skipCutscenes: false,
    disableAnimations: false,
    untargetSelf: false,
    autoSaveState: false,
    fastWalk: false
  }
};

export const useAppStore = defineStore("app", {
  state: () => ({
    snapshot: emptyState,
    ready: false,
    message: "Connecting to Grimoire…",
    error: ""
  }),
  getters: {
    activeSession(state): ClientSnapshot | undefined {
      return state.snapshot.sessions.find((session) => session.id === state.snapshot.activeSessionId);
    }
  },
  actions: {
    apply(snapshot: AppSnapshot) {
      this.snapshot = snapshot;
      this.ready = true;
      this.message = "";
      this.error = "";
    },
    fail(message: string) {
      this.error = message;
    }
  }
});
