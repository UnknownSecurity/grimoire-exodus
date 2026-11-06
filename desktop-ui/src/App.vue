<script setup lang="ts">
import {
  Activity,
  ArrowRight,
  Banknote,
  BookOpen,
  Bot,
  ChevronDown,
  ChevronUp,
  CirclePlus,
  Crosshair,
  EyeOff,
  FastForward,
  FileText,
  FolderOpen,
  Footprints,
  Gauge,
  Home,
  Keyboard,
  Layers3,
  ListTree,
  LockKeyhole,
  MapPin,
  Maximize2,
  Minus,
  Monitor,
  MousePointer2,
  PackageOpen,
  Palette,
  Pipette,
  Play,
  Plug,
  Radio,
  RefreshCw,
  Save,
  Settings,
  ShieldCheck,
  SkipForward,
  SlidersHorizontal,
  Sparkles,
  Square,
  SquareTerminal,
  Stethoscope,
  Trash2,
  MessageSquare,
  Wrench,
  Copy,
  X
} from "@lucide/vue";
import { animate } from "motion/mini";
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch, type Component } from "vue";
import { bridge, type BridgeEvent } from "./bridge";
import { type AppSnapshot, useAppStore } from "./stores/app";

type ShellActionKind = "tool" | "bot-start" | "bot-stop" | "option";

interface ShellAction {
  id: string;
  label: string;
  description: string;
  icon: Component;
  kind?: ShellActionKind;
  checked?: boolean;
  requiresSession?: boolean;
  requiresLogin?: boolean;
}

interface ShellSection {
  label: string;
  description: string;
  icon: Component;
}

interface AccountSnapshot {
  id: string;
  label: string;
  username: string;
  characterName?: string;
  characterClass?: string;
  characterLevel?: number;
  portraitDataUri?: string;
  active: boolean;
}

interface HomeSnapshot {
  success: boolean;
  message?: string;
  accounts: AccountSnapshot[];
  quickBots: string[];
  quickBotsLoading: boolean;
}

interface DoctorCheck {
  id?: string;
  title: string;
  status: "pending" | "pass" | "warning" | "fail";
  message: string;
}

interface DoctorResponse {
  success: boolean;
  completed: boolean;
  requestId?: string;
  message?: string;
  summary?: { passed: number; warnings: number; failed: number };
  checks?: DoctorCheck[];
}

interface ReportResponse {
  success: boolean;
  completed: boolean;
  external: boolean;
  requestId?: string;
  issueUrl?: string;
  message?: string;
}

const store = useAppStore();
const emblemUrl = "/Brand/grimoire-emblem.png";
const homeArtUrl = "/Home/images/ashfall-dragons.png";
const route = ref("home");
const gameSlot = ref<HTMLElement | null>(null);
const actionPending = ref(false);
const statsVisible = ref(false);
const selectedCell = ref("");
const selectedPad = ref("");
const openAccordion = ref<string | null>(null);
const accountFormOpen = ref(false);
const accountLabel = ref("");
const accountUsername = ref("");
const accountPassword = ref("");
const accountRemovalId = ref("");
const homeNotice = ref("");
const homeState = ref<HomeSnapshot>({
  success: true,
  accounts: [],
  quickBots: [],
  quickBotsLoading: true
});
const doctorBusy = ref(false);
const doctorRequestId = ref("");
const doctorMessage = ref("Ready to check this device.");
const doctorChecks = ref<DoctorCheck[]>([]);
const reportCategory = ref("Bug");
const reportSubject = ref("");
const reportDetails = ref("");
const reportSteps = ref("");
const reportBusy = ref(false);
const reportMessage = ref("");
const reportIssueUrl = ref("");
const reportRequestId = ref("");
let resizeObserver: ResizeObserver | undefined;
let removeBridgeListener: (() => void) | undefined;
let viewportFrame = 0;
let lastViewportKey = "";
let lastLocationKey = "";
let autoLocationSessionId = "";
let autoLocationAttempts = 0;
let autoLocationTimer = 0;
let doctorPollTimer = 0;
let reportPollTimer = 0;
let doctorPollCount = 0;
let reportPollCount = 0;
let routeMotion: { stop: () => void } | undefined;

const activeSession = computed(() => store.activeSession);
const showGame = computed(() => route.value !== "home" && !store.snapshot.homeVisible && Boolean(activeSession.value));
const showStatusCards = computed(() => !store.snapshot.hideDeviceStats && statsVisible.value);
const isMaximized = computed(() => store.snapshot.windowState === "Maximized");
const cellOptions = computed(() => uniqueChoices(store.snapshot.location.cells, store.snapshot.location.cell));
const padOptions = computed(() => uniqueChoices(store.snapshot.location.pads, store.snapshot.location.pad));
const accountFormValid = computed(() => accountUsername.value.trim().length > 0 && accountPassword.value.length > 0);
const reportFormValid = computed(() => reportSubject.value.trim().length >= 5 && reportDetails.value.trim().length >= 20);
const botProgress = computed(() => {
  const total = Math.max(0, store.snapshot.activeBot.commandCount);
  if (!total) return "No actions loaded";
  return `Action ${Math.min(total, store.snapshot.activeBot.index + 1)} of ${total}`;
});

const topRoutes = [
  { id: "home", label: "Home", icon: Home },
  { id: "bots", label: "Bots", icon: Bot },
  { id: "tools", label: "Tools", icon: Wrench },
  { id: "packets", label: "Packets", icon: Radio },
  { id: "options", label: "Options", icon: SlidersHorizontal },
  { id: "plugins", label: "Plugins", icon: Plug }
];

const sections: Record<string, ShellSection> = {
  home: {
    label: "Home",
    description: "Open a client or jump into a common Grimoire workflow.",
    icon: Home
  },
  bots: {
    label: "Bot controls",
    description: "Load, edit, start, and stop the active client's GBot through the existing native engine.",
    icon: Bot
  },
  tools: {
    label: "Tools",
    description: "Every supported legacy utility remains available as a session-aware native window.",
    icon: Wrench
  },
  packets: {
    label: "Packet tools",
    description: "Open the logger, spammer, or tamperer for the currently selected client.",
    icon: Radio
  },
  options: {
    label: "Client options",
    description: "Toggle the same per-client options used by the legacy toolbar and Bot Manager.",
    icon: SlidersHorizontal
  },
  plugins: {
    label: "Plugins",
    description: "Manage the existing in-process plugin collection without broadening plugin discovery.",
    icon: Plug
  },
  client: {
    label: "AQW client",
    description: "Monitor the active game and bot session.",
    icon: Monitor
  }
};

const homeActions: ShellAction[] = [
  { id: "bot-manager", label: "Bot Manager", description: "Edit and inspect the active GBot.", icon: Bot },
  { id: "settings", label: "Settings", description: "Open global Grimoire settings.", icon: Settings },
  { id: "bank", label: "Bank", description: "Open the active character's in-game bank.", icon: Banknote, requiresLogin: true },
  { id: "ultra-kit", label: "Ultra Kit", description: "Monitor the current encounter.", icon: ShieldCheck, requiresLogin: true }
];

const toolActions: ShellAction[] = [
  { id: "travel", label: "Fast Travels", description: "Open map and travel shortcuts.", icon: MapPin, requiresSession: true },
  { id: "loaders", label: "Loaders / Grabbers", description: "Open shop, quest, and item loaders.", icon: PackageOpen, requiresSession: true },
  { id: "ultra-kit", label: "Ultra Kit", description: "Inspect monsters, players, classes, and encounter status.", icon: ShieldCheck, requiresLogin: true },
  { id: "hotkeys", label: "Hotkeys", description: "Configure legacy client and bot shortcuts.", icon: Keyboard },
  { id: "cosmetics", label: "Cosmetics", description: "Open the character cosmetics utility.", icon: Palette, requiresLogin: true },
  { id: "bank", label: "Open Bank", description: "Open the active character's in-game bank.", icon: Banknote, requiresLogin: true },
  { id: "bank-items", label: "Bank Items", description: "Open the searchable native bank inventory window.", icon: ListTree, requiresLogin: true },
  { id: "bank-reload", label: "Reload Bank", description: "Refresh the active character's bank cache.", icon: FastForward, requiresLogin: true },
  { id: "eye-dropper", label: "Eye Dropper", description: "Inspect colors from the game surface.", icon: Pipette, requiresSession: true },
  { id: "logs", label: "Logs", description: "Open the bounded application log viewer.", icon: FileText },
  { id: "notepad", label: "Notepad", description: "Open the built-in notes window.", icon: FileText },
  { id: "ping-monitor", label: "Ping Monitor", description: "Show or hide the live in-client latency monitor.", icon: Activity, requiresSession: true },
  { id: "immediate-login", label: "Immediate Login", description: "Run the existing login flow for the active client.", icon: Play, requiresSession: true },
  { id: "settings", label: "Global Settings", description: "General, performance, keys, version, and logs.", icon: Settings }
];

const packetActions: ShellAction[] = [
  { id: "packet-logger", label: "Packet Logger", description: "Inspect the active client's bounded packet stream.", icon: SquareTerminal, requiresSession: true },
  { id: "packet-spammer", label: "Packet Spammer", description: "Open the existing packet sender utility.", icon: Radio, requiresSession: true },
  { id: "packet-tamperer", label: "Packet Tamperer", description: "Open packet interception and modification tools.", icon: Wrench, requiresSession: true }
];

const pluginActions: ShellAction[] = [
  { id: "plugin-manager", label: "Plugin Manager", description: "Load and manage plugins from the existing trusted location.", icon: Plug }
];

const botActions = computed<ShellAction[]>(() => [
  { id: "bot-manager", label: "Bot Manager", description: "Edit commands, skills, quests, drops, and bot options.", icon: ListTree },
  { id: "load-bot", label: "Load Bot", description: "Choose a GBot through the native file picker.", icon: FolderOpen },
  { id: "start-bot", label: "Start Bot", description: "Start the loaded GBot for the active client.", icon: Play, kind: "bot-start", requiresSession: true },
  { id: "stop-bot", label: "Stop Bot", description: "Safely stop the active client's current run.", icon: Square, kind: "bot-stop", requiresSession: true }
]);

const optionActions = computed<ShellAction[]>(() => [
  { id: "infinite-range", label: "Infinite Range", description: "Extend combat interaction range.", icon: Crosshair, kind: "option", checked: store.snapshot.options.infiniteRange, requiresSession: true },
  { id: "provoke-cell", label: "Provoke Cell", description: "Provoke monsters in the current cell.", icon: Activity, kind: "option", checked: store.snapshot.options.provokeCell, requiresSession: true },
  { id: "provoke-map", label: "Provoke Map", description: "Provoke monsters throughout the loaded map.", icon: Layers3, kind: "option", checked: store.snapshot.options.provokeMap, requiresSession: true },
  { id: "target-magnet", label: "Target Magnet", description: "Keep the active target positioned for combat.", icon: MousePointer2, kind: "option", checked: store.snapshot.options.targetMagnet, requiresSession: true },
  { id: "lag-killer", label: "Lag Killer", description: "Reduce selected visual workload.", icon: Gauge, kind: "option", checked: store.snapshot.options.lagKiller, requiresSession: true },
  { id: "hide-players", label: "Hide Players", description: "Hide other player characters on the active client.", icon: EyeOff, kind: "option", checked: store.snapshot.options.hidePlayers, requiresSession: true },
  { id: "skip-cutscenes", label: "Skip Cutscenes", description: "Skip supported client cutscenes.", icon: SkipForward, kind: "option", checked: store.snapshot.options.skipCutscenes, requiresSession: true },
  { id: "disable-animations", label: "Disable Skill Animations", description: "Suppress supported skill animations.", icon: Sparkles, kind: "option", checked: store.snapshot.options.disableAnimations, requiresSession: true },
  { id: "untarget-self", label: "Untarget Self", description: "Automatically clear self targeting.", icon: MousePointer2, kind: "option", checked: store.snapshot.options.untargetSelf, requiresSession: true },
  { id: "auto-save-state", label: "Auto-save State", description: "Persist supported bot state automatically.", icon: Save, kind: "option", checked: store.snapshot.options.autoSaveState, requiresSession: true },
  { id: "fast-walk", label: "Fast Walk Speed", description: "Use the legacy accelerated movement speed.", icon: Footprints, kind: "option", checked: store.snapshot.options.fastWalk, requiresSession: true }
]);

const currentSection = computed(() => sections[route.value] ?? sections.home);
const routeActions = computed<ShellAction[]>(() => {
  switch (route.value) {
    case "bots": return botActions.value;
    case "tools": return toolActions;
    case "packets": return packetActions;
    case "options": return optionActions.value;
    case "plugins": return pluginActions;
    case "home": return homeActions;
    default: return [];
  }
});
async function invoke(method: string, params: Record<string, unknown> = {}) {
  actionPending.value = true;
  store.error = "";
  try {
    await bridge.request(method, params);
  } catch (error) {
    store.fail(error instanceof Error ? error.message : "The action failed.");
  } finally {
    actionPending.value = false;
  }
}

async function selectRoute(id: string) {
  route.value = id;
  await invoke(id === "home" ? "navigation.home" : "navigation.client");
  if (id === "home") await refreshHomeState();
}

function playRouteMotion() {
  if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
  void nextTick(() => {
    const selector = route.value === "home"
      ? ".home-hero, .home-cards > article, .home-dashboard > *"
      : ".side-heading, .client-summary";
    const targets = document.querySelectorAll<HTMLElement>(selector);
    if (!targets.length) return;
    routeMotion?.stop();
    const animation = animate(
      targets,
      { opacity: [0, 1], transform: ["translateY(9px) scale(.992)", "translateY(0) scale(1)"] },
      {
        duration: 0.28,
        delay: (index) => Math.min(index * 0.035, 0.2),
        ease: "easeOut"
      }
    );
    routeMotion = animation;
    void animation.finished.then(() => {
      if (routeMotion !== animation) return;
      animation.stop();
      routeMotion = undefined;
    }).catch(() => undefined);
  });
}

async function activateSession(sessionId: string) {
  if (route.value === "home" || route.value === "client") route.value = "bots";
  await invoke("sessions.activate", { sessionId });
  await nextTick();
  reportViewport();
}

async function closeSession(sessionId: string, event: MouseEvent) {
  event.stopPropagation();
  await invoke("sessions.close", { sessionId });
}

async function openTool(tool: string) {
  await invoke("tools.open", { tool });
}

function isActionDisabled(action: ShellAction) {
  if (actionPending.value) return true;
  if (action.requiresSession && !activeSession.value) return true;
  if (action.requiresLogin && !activeSession.value?.isLoggedIn) return true;
  if (action.kind === "bot-start" && store.snapshot.activeBot.running) return true;
  if (action.kind === "bot-stop" && !store.snapshot.activeBot.running) return true;
  return false;
}

async function runShellAction(action: ShellAction) {
  if (isActionDisabled(action)) return;
  const session = activeSession.value;
  if (action.kind === "bot-start" && session) {
    await invoke("bots.start", { sessionId: session.id });
    return;
  }
  if (action.kind === "bot-stop" && session) {
    await invoke("bots.stop", { sessionId: session.id });
    return;
  }
  if (action.kind === "option" && session) {
    await invoke("options.toggle", { sessionId: session.id, option: action.id });
    return;
  }
  await openTool(action.id);
}

async function toggleBot() {
  const session = activeSession.value;
  if (!session) return;
  await invoke(store.snapshot.activeBot.running ? "bots.stop" : "bots.start", {
    sessionId: session.id
  });
}

function uniqueChoices(values: string[], current: string) {
  const seen = new Set<string>();
  return [current, ...values].filter((value) => {
    const normalized = String(value || "").trim();
    if (!normalized || seen.has(normalized.toLowerCase())) return false;
    seen.add(normalized.toLowerCase());
    return true;
  });
}

function syncLocation(snapshot: AppSnapshot) {
  const location = snapshot.location;
  const key = JSON.stringify([location.cell, location.pad, location.cells, location.pads]);
  if (key !== lastLocationKey) {
    lastLocationKey = key;
    selectedCell.value = location.cell || "";
    selectedPad.value = location.pad || "";
  }

  const session = snapshot.sessions.find((candidate) => candidate.id === snapshot.activeSessionId);
  if (!location.available || !session) {
    window.clearTimeout(autoLocationTimer);
    autoLocationTimer = 0;
    autoLocationSessionId = "";
    autoLocationAttempts = 0;
    return;
  }
  if (autoLocationSessionId !== session.id) {
    window.clearTimeout(autoLocationTimer);
    autoLocationTimer = 0;
    autoLocationSessionId = session.id;
    autoLocationAttempts = 0;
  }
  if (location.cell && location.pad && location.cells.length && location.pads.length) {
    window.clearTimeout(autoLocationTimer);
    autoLocationTimer = 0;
    return;
  }
  scheduleAutoLocationRefresh(session.id);
}

function scheduleAutoLocationRefresh(sessionId: string) {
  if (autoLocationTimer || autoLocationAttempts >= 8) return;
  const delay = autoLocationAttempts === 0 ? 0 : 750;
  autoLocationTimer = window.setTimeout(async () => {
    autoLocationTimer = 0;
    if (autoLocationSessionId !== sessionId) return;
    autoLocationAttempts++;
    await refreshLocation();
    const location = store.snapshot.location;
    if (autoLocationSessionId === sessionId
        && location.available
        && (!location.cell || !location.pad || !location.cells.length || !location.pads.length)) {
      scheduleAutoLocationRefresh(sessionId);
    }
  }, delay);
}

async function refreshLocation() {
  const session = activeSession.value;
  if (!session) return;
  await invoke("location.refresh", { sessionId: session.id });
}

async function moveLocation() {
  const session = activeSession.value;
  if (!session || !selectedCell.value || !selectedPad.value) return;
  await invoke("location.move", {
    sessionId: session.id,
    cell: selectedCell.value,
    pad: selectedPad.value
  });
}

async function requestHome<T>(method: string, params: Record<string, unknown> = {}) {
  store.error = "";
  try {
    return await bridge.request<T>(method, params);
  } catch (error) {
    store.fail(error instanceof Error ? error.message : "Grimoire Home could not complete that action.");
    return undefined;
  }
}

function applyHomeState(state: HomeSnapshot | undefined) {
  if (!state) return;
  homeState.value = {
    success: state.success,
    message: state.message,
    accounts: Array.isArray(state.accounts) ? state.accounts : [],
    quickBots: Array.isArray(state.quickBots) ? state.quickBots : [],
    quickBotsLoading: Boolean(state.quickBotsLoading)
  };
  if (state.message) homeNotice.value = state.message;
}

async function refreshHomeState() {
  applyHomeState(await requestHome<HomeSnapshot>("home.getState"));
}

async function saveAccount() {
  if (!accountFormValid.value) return;
  const state = await requestHome<HomeSnapshot>("home.accounts.save", {
    label: accountLabel.value,
    username: accountUsername.value,
    password: accountPassword.value
  });
  applyHomeState(state);
  if (state?.success) {
    accountLabel.value = "";
    accountUsername.value = "";
    accountPassword.value = "";
    accountFormOpen.value = false;
  }
}

async function openAccount(account: AccountSnapshot) {
  if (account.active) return;
  applyHomeState(await requestHome<HomeSnapshot>("home.accounts.open", { id: account.id }));
}

async function removeAccount(account: AccountSnapshot) {
  if (accountRemovalId.value !== account.id) {
    accountRemovalId.value = account.id;
    homeNotice.value = `Select remove again to confirm deleting ${account.label || account.username} from the locker.`;
    return;
  }
  accountRemovalId.value = "";
  applyHomeState(await requestHome<HomeSnapshot>("home.accounts.remove", { id: account.id }));
}

async function openBotFolder() {
  applyHomeState(await requestHome<HomeSnapshot>("home.botFolder"));
}

function toggleAccordion(id: string) {
  openAccordion.value = openAccordion.value === id ? null : id;
}

async function runDoctor() {
  openAccordion.value = "doctor";
  doctorBusy.value = true;
  doctorPollCount = 0;
  doctorMessage.value = "Running automated compatibility checks…";
  doctorChecks.value = [];
  handleDoctorResponse(await requestHome<DoctorResponse>("home.doctor.begin"));
}

function handleDoctorResponse(state: DoctorResponse | undefined) {
  if (!state) {
    doctorBusy.value = false;
    return;
  }
  doctorChecks.value = state.checks || [];
  doctorMessage.value = state.message || doctorMessage.value;
  doctorRequestId.value = state.requestId || doctorRequestId.value;
  if (!state.success || state.completed) {
    doctorBusy.value = false;
    if (state.summary) {
      doctorMessage.value = `${state.summary.passed} passed · ${state.summary.warnings} warnings · ${state.summary.failed} issues. ${state.message || "Checkup complete."}`;
    }
    return;
  }
  window.clearTimeout(doctorPollTimer);
  doctorPollTimer = window.setTimeout(pollDoctor, 500);
}

async function pollDoctor() {
  if (!doctorRequestId.value) return;
  doctorPollCount++;
  if (doctorPollCount > 60) {
    doctorBusy.value = false;
    doctorMessage.value = "The compatibility check timed out. Try again.";
    return;
  }
  handleDoctorResponse(await requestHome<DoctorResponse>("home.doctor.status", {
    requestId: doctorRequestId.value
  }));
}

async function copyDoctorResults() {
  if (!doctorRequestId.value) return;
  const result = await requestHome<{ success: boolean; message?: string }>("home.doctor.copy", {
    requestId: doctorRequestId.value
  });
  if (result?.message) homeNotice.value = result.message;
}

async function submitReport() {
  if (!reportFormValid.value || reportBusy.value) return;
  reportBusy.value = true;
  reportPollCount = 0;
  reportRequestId.value = "";
  reportMessage.value = "Sending the report securely…";
  reportIssueUrl.value = "";
  handleReportResponse(await requestHome<ReportResponse>("home.report.begin", {
    category: reportCategory.value,
    subject: reportSubject.value.trim(),
    version: store.snapshot.version,
    details: reportDetails.value.trim(),
    steps: reportSteps.value.trim(),
    startedAt: Date.now()
  }));
}

function handleReportResponse(state: ReportResponse | undefined) {
  if (!state) {
    reportBusy.value = false;
    return;
  }
  reportMessage.value = state.message || reportMessage.value;
  reportRequestId.value = state.requestId || reportRequestId.value;
  if (!state.success || state.completed || state.external) {
    reportBusy.value = false;
    reportIssueUrl.value = state.issueUrl || "";
    if (state.success && state.completed) {
      reportSubject.value = "";
      reportDetails.value = "";
      reportSteps.value = "";
    }
    return;
  }
  window.clearTimeout(reportPollTimer);
  reportPollTimer = window.setTimeout(async () => {
    reportPollCount++;
    if (reportPollCount > 60 || !reportRequestId.value) {
      reportBusy.value = false;
      reportMessage.value = "The report status timed out. Try again.";
      return;
    }
    handleReportResponse(await requestHome<ReportResponse>("home.report.status", {
      requestId: reportRequestId.value
    }));
  }, 500);
}

async function openHomeLink(address: string) {
  const result = await requestHome<{ success: boolean; message?: string }>("home.link.open", { address });
  if (result?.message) homeNotice.value = result.message;
}

function handleBridgeEvent(event: BridgeEvent) {
  if (event.protocol !== "1.0" || event.event !== "app.stateChanged") return;
  const snapshot = event.payload as AppSnapshot;
  store.apply(snapshot);
  syncLocation(snapshot);
  if (store.snapshot.homeVisible) route.value = "home";
  else if (route.value === "home" || route.value === "client") route.value = "bots";
}

function beginWindowDrag(event: MouseEvent) {
  if (event.button !== 0) return;
  const target = event.target as HTMLElement | null;
  if (target?.closest("button, a, input, select, textarea")) return;
  void bridge.request("window.beginDrag").catch(() => undefined);
}

function runWindowAction(method: "window.minimize" | "window.toggleMaximize" | "window.close") {
  void bridge.request(method).catch((error) => {
    store.fail(error instanceof Error ? error.message : "The window action failed.");
  });
}

function reportViewport() {
  if (!gameSlot.value || !bridge.available) return;
  window.cancelAnimationFrame(viewportFrame);
  viewportFrame = window.requestAnimationFrame(() => {
    const rect = gameSlot.value!.getBoundingClientRect();
    const values = [rect.left, rect.top, rect.width, rect.height, window.innerWidth, window.innerHeight]
      .map((value) => Math.round(value));
    const viewportKey = values.join(":");
    if (viewportKey === lastViewportKey) return;
    lastViewportKey = viewportKey;
    void bridge.request("ui.setGameViewport", {
      x: rect.left,
      y: rect.top,
      width: rect.width,
      height: rect.height,
      viewportWidth: window.innerWidth,
      viewportHeight: window.innerHeight
    }).catch(() => { lastViewportKey = ""; });
  });
}

watch(showGame, () => {
  lastViewportKey = "";
  nextTick(reportViewport);
});
watch(route, playRouteMotion);
watch(() => store.snapshot.hideDeviceStats, (hidden) => {
  if (hidden) statsVisible.value = false;
});
watch(gameSlot, (current, previous) => {
  if (previous) resizeObserver?.unobserve(previous);
  if (current) {
    resizeObserver?.observe(current);
    nextTick(reportViewport);
  }
});

onMounted(async () => {
  if (!bridge.available) {
    store.fail("This shell must run inside Grimoire Exodus.");
    return;
  }
  removeBridgeListener = bridge.onEvent(handleBridgeEvent);
  resizeObserver = new ResizeObserver(reportViewport);
  if (gameSlot.value) resizeObserver.observe(gameSlot.value);
  try {
    const snapshot = await bridge.request<AppSnapshot>("app.getBootstrap");
    store.apply(snapshot);
    syncLocation(snapshot);
    await bridge.request("ui.ready");
    await refreshHomeState();
    if (!store.snapshot.homeVisible) route.value = "bots";
    await nextTick();
    reportViewport();
    playRouteMotion();
  } catch (error) {
    store.fail(error instanceof Error ? error.message : "The modern UI could not start.");
  }
});

onBeforeUnmount(() => {
  resizeObserver?.disconnect();
  removeBridgeListener?.();
  window.cancelAnimationFrame(viewportFrame);
  window.clearTimeout(doctorPollTimer);
  window.clearTimeout(reportPollTimer);
  window.clearTimeout(autoLocationTimer);
  routeMotion?.stop();
});
</script>

<template>
  <div class="app-shell">
    <header class="topbar" @mousedown="beginWindowDrag">
      <button class="brand" type="button" title="Grimoire Exodus home" @click="selectRoute('home')">
        <img :src="emblemUrl" alt="" />
        <span class="brand-copy"><strong>Grimoire Exodus</strong><small>AQW Bot Manager</small></span>
      </button>
      <nav class="top-routes" aria-label="Main navigation">
        <button
          v-for="item in topRoutes"
          :key="item.id"
          type="button"
          :class="{ active: route === item.id }"
          :title="item.label"
          @click="selectRoute(item.id)"
        >
          <component :is="item.icon" :size="18" />
          <span>{{ item.label }}</span>
        </button>
      </nav>
      <button class="icon-button" type="button" title="Settings" aria-label="Open settings" @click="openTool('settings')">
        <Settings :size="19" />
      </button>
      <span class="window-drag-space" aria-hidden="true"></span>
      <div class="window-controls" aria-label="Window controls">
        <button type="button" title="Minimize" aria-label="Minimize" @mousedown.stop @click="runWindowAction('window.minimize')">
          <Minus :size="17" />
        </button>
        <button
          type="button"
          :title="isMaximized ? 'Restore' : 'Maximize'"
          :aria-label="isMaximized ? 'Restore' : 'Maximize'"
          @mousedown.stop
          @click="runWindowAction('window.toggleMaximize')"
        >
          <Copy v-if="isMaximized" :size="14" />
          <Maximize2 v-else :size="15" />
        </button>
        <button class="window-close" type="button" title="Close" aria-label="Close" @mousedown.stop @click="runWindowAction('window.close')">
          <X :size="17" />
        </button>
      </div>
    </header>

    <aside class="sidebar">
      <div class="side-heading">
        <component :is="currentSection.icon" :size="16" />
        <span>{{ currentSection.label }}</span>
      </div>
      <TransitionGroup name="side-action" tag="nav" class="side-tools" :aria-label="`${currentSection.label} actions`">
        <button
          v-for="(item, index) in routeActions"
          :key="item.id"
          type="button"
          :class="{ primary: route === 'home' && index === 0, 'active-toggle': item.kind === 'option' && item.checked }"
          :disabled="isActionDisabled(item)"
          :title="`${item.label} — ${item.description}`"
          @click="runShellAction(item)"
        >
          <component :is="item.icon" :size="19" />
          <span class="nav-label">{{ item.label }}</span>
          <span v-if="item.kind === 'option'" class="option-dot" :class="{ on: item.checked }"></span>
        </button>
      </TransitionGroup>

      <section class="client-summary">
        <div class="summary-heading"><span class="status-dot" :class="{ online: activeSession?.isLoggedIn }"></span>AQW Client</div>
        <dl>
          <div><dt>Engine</dt><dd>{{ activeSession?.engine ?? "Waiting" }}</dd></div>
          <div><dt>Status</dt><dd>{{ activeSession?.state ?? "No client" }}</dd></div>
        </dl>
        <button class="accent-button" type="button" :disabled="!activeSession || actionPending" @click="toggleBot">
          <Square v-if="store.snapshot.activeBot.running" :size="16" fill="currentColor" />
          <Play v-else :size="16" fill="currentColor" />
          {{ store.snapshot.activeBot.running ? "Stop Bot" : "Start Bot" }}
        </button>
      </section>
    </aside>

    <main class="workspace" :class="{ 'single-client': !store.snapshot.multiClientEnabled, 'stats-hidden': !showStatusCards }">
      <TransitionGroup v-if="store.snapshot.multiClientEnabled" class="client-tabs" name="client-tab" tag="section" aria-label="AQW clients">
        <button
          v-for="session in store.snapshot.sessions"
          :key="session.id"
          class="client-tab"
          :class="{ active: session.isActive && !store.snapshot.homeVisible }"
          type="button"
          @click="activateSession(session.id)"
        >
          <Monitor :size="16" />
          <span>{{ session.title }}</span>
          <span class="mini-dot" :class="{ running: session.botRunning }"></span>
          <X :size="14" class="tab-close" @click="closeSession(session.id, $event)" />
        </button>
        <button
          key="add-client"
          class="add-client"
          type="button"
          :disabled="!store.snapshot.canCreateClient || actionPending"
          :title="store.snapshot.multiClientEnabled ? 'Add AQW client' : 'Enable experimental multi-client mode in Settings first'"
          @click="invoke('sessions.create')"
        >
          <CirclePlus :size="17" /> <span>New Client</span>
        </button>
      </TransitionGroup>

      <section v-if="store.snapshot.homeVisible && route === 'home'" class="home-workspace" data-motion-surface :style="{ '--home-art': `url(${homeArtUrl})` }">
        <div class="home-hero">
          <div class="hero-copy">
            <span class="eyebrow">Modern shell preview</span>
            <h1>Command your AQW clients from one focused workspace.</h1>
            <p>The new interface is running over the unchanged native Grimoire engine. Your Flash client, bots, packets, and saved data remain C#-owned.</p>
            <div class="hero-actions">
              <button v-if="activeSession" class="accent-button" type="button" @click="activateSession(activeSession.id)">
                <Play :size="16" fill="currentColor" /> Open {{ activeSession.title }}
              </button>
              <button class="ghost-button" type="button" @click="openTool('bot-manager')"><ListTree :size="16" /> Bot Manager</button>
            </div>
          </div>
          <div class="hero-emblem"><img :src="emblemUrl" alt="" /></div>
        </div>
        <div class="home-cards">
          <article><Gauge :size="20" /><div><strong>Native game surface</strong><span>Flash.ocx and Ruffle remain isolated per client.</span></div></article>
          <article><ShieldCheck :size="20" /><div><strong>Compatibility first</strong><span>Legacy GBot loading and execution are unchanged.</span></div></article>
          <article><Layers3 :size="20" /><div><strong>Progressive migration</strong><span>Tools open their proven native forms while the shell is modernized.</span></div></article>
        </div>

        <Transition name="toast">
          <div v-if="homeNotice" class="home-notice" role="status">
            <span>{{ homeNotice }}</span>
            <button type="button" aria-label="Dismiss message" @click="homeNotice = ''"><X :size="14" /></button>
          </div>
        </Transition>

        <div class="home-dashboard">
          <section class="home-panel account-locker">
            <header class="home-panel-heading">
              <span class="home-panel-title"><LockKeyhole :size="18" /><span><strong>Account Locker</strong><small>Encrypted for your current Windows account.</small></span></span>
              <button class="compact-button" type="button" @click="accountFormOpen = !accountFormOpen"><CirclePlus :size="15" />Add</button>
            </header>
            <TransitionGroup name="list-item" tag="div" class="account-list">
              <p v-if="!homeState.accounts.length" key="empty-accounts" class="empty-state">No saved accounts yet.</p>
              <article v-for="account in homeState.accounts" :key="account.id" class="account-row" :class="{ active: account.active }">
                <button class="account-open" type="button" :disabled="account.active" @click="openAccount(account)">
                  <img v-if="account.portraitDataUri" :src="account.portraitDataUri" alt="" />
                  <span v-else class="account-initial">{{ (account.characterName || account.label || account.username || '?').charAt(0).toUpperCase() }}</span>
                  <span class="account-copy">
                    <strong>{{ account.label || account.characterName || account.username }}</strong>
                    <small>{{ account.characterName || account.username }}</small>
                    <em v-if="account.characterClass || account.characterLevel">
                      {{ account.characterClass || 'Class unavailable' }}<template v-if="account.characterLevel"> · Level {{ account.characterLevel }}</template>
                    </em>
                  </span>
                  <span v-if="account.active" class="account-active">In use</span>
                </button>
                <button class="account-remove" :class="{ confirm: accountRemovalId === account.id }" type="button" :title="accountRemovalId === account.id ? 'Select again to confirm removal' : 'Remove account'" :aria-label="`Remove ${account.label || account.username}`" @click="removeAccount(account)"><X v-if="accountRemovalId === account.id" :size="15" /><Trash2 v-else :size="15" /></button>
              </article>
            </TransitionGroup>
            <Transition name="form-panel">
              <form v-if="accountFormOpen" class="account-form" autocomplete="off" @submit.prevent="saveAccount">
                <label>Display name<input v-model="accountLabel" type="text" maxlength="60" placeholder="Main, farming alt, support…" /></label>
                <label>AQW username<input v-model="accountUsername" type="text" maxlength="100" autocomplete="off" required /></label>
                <label>AQW password<input v-model="accountPassword" type="password" maxlength="256" autocomplete="new-password" required /></label>
                <div class="form-actions">
                  <button class="ghost-button" type="button" @click="accountFormOpen = false">Cancel</button>
                  <button class="compact-button primary" type="submit" :disabled="!accountFormValid">Save securely</button>
                </div>
              </form>
            </Transition>
          </section>

          <section class="home-accordions" aria-label="Grimoire Home tools">
            <article class="home-accordion" :class="{ open: openAccordion === 'bots' }">
              <header>
                <button type="button" @click="toggleAccordion('bots')"><ListTree :size="17" /><span><strong>Bot Manager</strong><small>View GBots available to Quick Load.</small></span><ChevronDown :size="16" /></button>
                <button class="accordion-action" type="button" title="Open Bot folder" aria-label="Open Bot folder" @click="openBotFolder"><FolderOpen :size="16" /></button>
              </header>
              <Transition name="accordion">
                <div v-if="openAccordion === 'bots'" class="accordion-body bot-browser">
                  <p v-if="homeState.quickBotsLoading" class="empty-state">Refreshing available GBots…</p>
                  <p v-else-if="!homeState.quickBots.length" class="empty-state">No GBots are available yet.</p>
                  <button v-for="botName in homeState.quickBots" :key="botName" type="button" @click="openTool('quick-load')"><Bot :size="14" />{{ botName }}</button>
                </div>
              </Transition>
            </article>

            <article class="home-accordion" :class="{ open: openAccordion === 'doctor' }">
              <header>
                <button type="button" @click="toggleAccordion('doctor')"><Stethoscope :size="17" /><span><strong>Grimoire Doctor</strong><small>Check this device for compatibility issues.</small></span><ChevronDown :size="16" /></button>
                <button class="accordion-action" type="button" :disabled="doctorBusy" title="Run diagnostic" aria-label="Run diagnostic" @click="runDoctor"><RefreshCw :size="16" :class="{ spinning: doctorBusy }" /></button>
              </header>
              <Transition name="accordion">
                <div v-if="openAccordion === 'doctor'" class="accordion-body doctor-body">
                  <p>{{ doctorMessage }}</p>
                  <TransitionGroup v-if="doctorChecks.length" name="list-item" tag="ul" class="doctor-list">
                    <li v-for="check in doctorChecks" :key="check.id || check.title" :class="check.status"><span>{{ check.status === 'pass' ? '✓' : check.status === 'warning' ? '!' : check.status === 'fail' ? '×' : '…' }}</span><div><strong>{{ check.title }}</strong><small>{{ check.message }}</small></div></li>
                  </TransitionGroup>
                  <button v-if="doctorRequestId && !doctorBusy" class="compact-button" type="button" @click="copyDoctorResults">Copy privacy-safe results</button>
                </div>
              </Transition>
            </article>

            <article class="home-accordion" :class="{ open: openAccordion === 'roadmap' }">
              <header>
                <button type="button" @click="toggleAccordion('roadmap')"><BookOpen :size="17" /><span><strong>Development Roadmap</strong><small>Current work and what is coming next.</small></span><ChevronDown :size="16" /></button>
              </header>
              <Transition name="accordion">
                <div v-if="openAccordion === 'roadmap'" class="accordion-body">
                  <ul class="roadmap-list">
                    <li><span>✓</span>Current AQW client compatibility<strong>Done</strong></li>
                    <li><span>✓</span>Encrypted one-click Account Locker<strong>Done</strong></li>
                    <li><span>✓</span>Experimental multi-client workspace<strong>Done</strong></li>
                    <li><span>✓</span>Bundled native Flash runtime and verified updates<strong>Done</strong></li>
                    <li><span>✓</span>Startup diagnostics, tray controls, and crash recovery<strong>Done</strong></li>
                    <li><span>✓</span>Flash.ocx / Ruffle engine switcher<strong>Experimental</strong></li>
                    <li><span>•</span>Remote companion control<strong>Planned</strong></li>
                    <li><span>•</span>Community-maintained bot repository<strong>Planned</strong></li>
                  </ul>
                </div>
              </Transition>
            </article>

            <article class="home-accordion" :class="{ open: openAccordion === 'report' }">
              <header>
                <button type="button" @click="toggleAccordion('report')"><MessageSquare :size="17" /><span><strong>Contact / Report Issue</strong><small>Send public-safe feedback from Grimoire.</small></span><ChevronDown :size="16" /></button>
              </header>
              <Transition name="accordion">
                <form v-if="openAccordion === 'report'" class="accordion-body report-form" autocomplete="off" @submit.prevent="submitReport">
                  <div class="report-grid">
                    <label>Report type<select v-model="reportCategory"><option value="Bug">Bug report</option><option value="Feature">Feature request</option><option value="Support">General support</option></select></label>
                    <label>Grimoire version<input :value="store.snapshot.version" type="text" readonly /></label>
                  </div>
                  <label>Subject<input v-model="reportSubject" type="text" minlength="5" maxlength="100" placeholder="A short summary" required /><small>{{ reportSubject.trim().length }}/100 · minimum 5</small></label>
                  <label>What happened or what should change?<textarea v-model="reportDetails" minlength="20" maxlength="4000" rows="4" placeholder="Describe the issue, expected behavior, or feature idea." required></textarea><small>{{ reportDetails.trim().length }}/4000 · minimum 20</small></label>
                  <label>Steps or additional context <em>(optional)</em><textarea v-model="reportSteps" maxlength="2000" rows="3"></textarea></label>
                  <p class="privacy-note"><strong>Keep reports public-safe.</strong> Remove passwords, account data, tokens, private rooms, logs, and personal information. To attach a GBot, <button type="button" @click="openHomeLink('https://unknownsecurity.github.io/grimoire-exodus/#contact')">use the online form</button>. Report security issues <button type="button" @click="openHomeLink('https://github.com/UnknownSecurity/grimoire-exodus/security/advisories/new')">privately</button>.</p>
                  <p v-if="reportMessage" class="report-message">{{ reportMessage }}</p>
                  <div class="form-actions">
                    <button v-if="reportIssueUrl" class="ghost-button" type="button" @click="openHomeLink(reportIssueUrl)">View issue <ArrowRight :size="14" /></button>
                    <button class="compact-button primary" type="submit" :disabled="!reportFormValid || reportBusy">{{ reportBusy ? 'Sending…' : 'Send report' }}</button>
                  </div>
                </form>
              </Transition>
            </article>
          </section>
        </div>
      </section>

      <template v-else>
        <section ref="gameSlot" class="game-slot" aria-label="Native AQW game viewport">
          <div class="game-loading">
            <Monitor :size="28" />
            <strong>{{ activeSession?.title ?? "AQW Client" }}</strong>
            <span v-if="activeSession?.state === 'Faulted'">Client initialization failed</span>
            <span v-else>Loading AQW {{ activeSession?.loadProgress ?? 0 }}%</span>
          </div>
        </section>

        <Transition name="dock">
          <section v-if="showStatusCards" class="operations-dock">
            <article class="operation-card">
              <header><span><Activity :size="17" />Bot Status</span><em :class="{ running: store.snapshot.activeBot.running }">{{ store.snapshot.activeBot.running ? "Running" : "Stopped" }}</em></header>
              <dl>
                <div><dt>Runtime</dt><dd>{{ store.snapshot.activeBot.runtime }}</dd></div>
                <div><dt>Progress</dt><dd>{{ botProgress }}</dd></div>
                <div><dt>Ping</dt><dd>{{ activeSession?.ping == null ? "--" : `${activeSession.ping} ms` }}</dd></div>
              </dl>
            </article>
            <article class="operation-card active-script">
              <header><span><ListTree :size="17" />Active Bot</span></header>
              <div class="script-name">{{ store.snapshot.activeBot.name }}</div>
              <p>{{ store.snapshot.activeBot.activeAction ?? "No action is currently executing." }}</p>
              <button type="button" @click="openTool('bot-manager')"><Bot :size="16" />Manage Actions</button>
            </article>
            <article class="operation-card logs-preview">
              <header><span><SquareTerminal :size="17" />Session</span></header>
              <div class="log-line"><time>STATE</time><span>{{ activeSession?.state ?? "Unavailable" }}</span></div>
              <div class="log-line"><time>ENGINE</time><span>{{ activeSession?.engine ?? "Unknown" }}</span></div>
              <div class="log-line"><time>ACCOUNT</time><span>{{ activeSession?.isLoggedIn ? activeSession.title : "Not logged in" }}</span></div>
              <button type="button" @click="openTool('packet-logger')"><Radio :size="16" />Packet Logger</button>
            </article>
          </section>
        </Transition>
      </template>

      <Transition name="toast">
        <div v-if="store.error" class="error-toast" role="alert">{{ store.error }}</div>
      </Transition>
    </main>

    <footer class="statusbar">
      <span><span class="status-dot online"></span>v{{ store.snapshot.version }}</span>
      <span v-if="!store.snapshot.hideDeviceStats"><Activity :size="15" />PING <strong>{{ activeSession?.ping == null ? "--" : `${activeSession.ping} ms` }}</strong></span>
      <button
        v-if="showGame && !store.snapshot.hideDeviceStats"
        class="stats-toggle"
        type="button"
        :title="statsVisible ? 'Hide Bot Status, Active Bot, and Session cards' : 'Show Bot Status, Active Bot, and Session cards'"
        :aria-label="statsVisible ? 'Hide status cards' : 'Show status cards'"
        :aria-pressed="statsVisible"
        @click="statsVisible = !statsVisible"
      >
        <ChevronDown v-if="statsVisible" :size="16" />
        <ChevronUp v-else :size="16" />
      </button>
      <div class="status-actions">
        <div v-if="store.snapshot.location.available" class="location-controls">
          <label title="Current cell"><span>Cell</span><select v-model="selectedCell" @focus="refreshLocation"><option value="" disabled>Select cell</option><option v-for="cell in cellOptions" :key="cell" :value="cell">{{ cell }}</option></select></label>
          <label title="Current pad"><span>Pad</span><select v-model="selectedPad" @focus="refreshLocation"><option value="" disabled>Select pad</option><option v-for="pad in padOptions" :key="pad" :value="pad">{{ pad }}</option></select></label>
          <button class="icon-status-button" type="button" title="Read current cell and pad" aria-label="Read current cell and pad" :disabled="actionPending" @click="refreshLocation"><RefreshCw :size="14" /></button>
          <button class="icon-status-button" type="button" title="Move to selected cell and pad" aria-label="Move to selected cell and pad" :disabled="!selectedCell || !selectedPad || actionPending" @click="moveLocation"><MapPin :size="14" /></button>
        </div>
        <button type="button" @click="openTool('quick-load')"><ListTree :size="15" />Quick Load <ChevronDown :size="14" /></button>
      </div>
    </footer>
  </div>
</template>
