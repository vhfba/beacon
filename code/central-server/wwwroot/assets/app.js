const storageKeys = {
  endpoint: "beacon.endpoint",
  apiKey: "beacon.apiKey"
};

const state = {
  view: "fleet",
  probes: [],
  plugins: [],
  assignments: [],
  selectedProbeId: null,
  drawerOpen: false,
  settingsOpen: false,
  refreshing: false,
  runtime: null,
  probeConfig: null,
  pendingActions: [],
  actionHistory: []
};

const keyPresets = { admin: "", probe: "" };
const staleSeconds = 45;

const $ = id => document.getElementById(id);

initialize();

function initialize() {
  $("endpoint").value = localStorage.getItem(storageKeys.endpoint) || "/graphql";
  $("apiKey").value = localStorage.getItem(storageKeys.apiKey) || "";

  bindNavigation();
  bindGlobalUi();
  bindFleet();
  bindPlugins();
  bindActions();
  bindMonitoring();
  bindPanel();
  bindModals();

  setConnectionState(null);
  switchView("fleet");
  refreshAll();
  setInterval(refreshAll, 10000);
}

function bindNavigation() {
  document.querySelectorAll(".ntab").forEach(button => {
    button.addEventListener("click", () => switchView(button.dataset.view));
  });
}

function bindGlobalUi() {
  $("conn-pill").addEventListener("click", toggleSettings);
  $("settings-overlay").addEventListener("click", toggleSettings);
  $("save-settings-btn").addEventListener("click", () => {
    localStorage.setItem(storageKeys.endpoint, $("endpoint").value.trim() || "/graphql");
    localStorage.setItem(storageKeys.apiKey, $("apiKey").value.trim());
    toast("Connection settings saved", "ok");
    setConnectionState(true);
    if (state.settingsOpen) toggleSettings();
  });
  $("apply-preset-btn").addEventListener("click", () => {
    const preset = keyPresets[$("apiKeyPreset").value];
    if (!preset) {
      toast("No preset configured. Enter the key manually.", "warn");
      return;
    }
    $("apiKey").value = preset;
    toast("Preset applied", "ok");
  });
  $("test-conn-btn").addEventListener("click", async () => {
    try {
      await gql("query { fleetStatus { probes { id } } }");
      setConnectionState(true);
      toast("Connection is healthy", "ok");
    } catch (error) {
      setConnectionState(false);
      setLog({ error: String(error) });
      toast("Connection test failed", "err");
    }
  });
  $("refresh-btn").addEventListener("click", refreshAll);
  $("log-btn").addEventListener("click", toggleDrawer);
  $("close-drawer-btn").addEventListener("click", toggleDrawer);
  $("clear-log-btn").addEventListener("click", () => {
    $("output").value = "{}";
    $("log-time").textContent = "";
  });
  $("open-runtime-probe-btn").addEventListener("click", () => {
    if (!state.selectedProbeId && state.probes.length) state.selectedProbeId = state.probes[0].id;
    if (!state.selectedProbeId) {
      toast("Select a probe first", "warn");
      return;
    }
    openProbePanel(state.selectedProbeId);
  });
}

function bindFleet() {
  $("fleet-body").addEventListener("click", event => {
    const row = event.target.closest("tr[data-id]");
    if (row) openProbePanel(row.dataset.id);
  });
}

function bindPlugins() {
  $("open-register-plugin").addEventListener("click", () => openModal("modal-plugin"));
  $("register-plugin-form").addEventListener("submit", handleRegisterPlugin);
  $("plugin-reg-file").addEventListener("change", async event => {
    const file = event.target.files?.[0];
    if (!file) return;
    $("plugin-reg-json").value = await file.text();
    toast("JSON loaded from file", "info");
  });
  $("plugin-fetch-form").addEventListener("submit", handleFetchPlugin);
  $("bundle-form").addEventListener("submit", handleDownloadBundle);
  $("plugins-grid").addEventListener("click", async event => {
    const toggleButton = event.target.closest(".plugin-toggle");
    const deleteButton = event.target.closest(".plugin-delete");
    if (toggleButton) await handleTogglePlugin(toggleButton.dataset.id, toggleButton.dataset.next === "true");
    if (deleteButton) await handleDeletePlugin(deleteButton.dataset.id);
  });
}

function bindActions() {
  $("quick-action-form").addEventListener("submit", handleQuickAction);
  $("refresh-actions-view-btn").addEventListener("click", refreshActionsView);
}

function bindMonitoring() {
  $("open-dashboard-btn").addEventListener("click", handleOpenDashboard);
  $("service-discovery-form").addEventListener("submit", handleLoadServiceDiscovery);
}

function bindPanel() {
  $("close-panel-btn").addEventListener("click", closeProbePanel);
  $("panel-overlay").addEventListener("click", closeProbePanel);
  document.querySelectorAll(".ptab").forEach(button => {
    button.addEventListener("click", () => switchPanelTab(button.dataset.panelTab));
  });

  $("status-form").addEventListener("submit", handleUpdateProbeStatus);
  $("panel-config-form").addEventListener("submit", handleUpdateProbeTestConfig);
  $("toggle-form").addEventListener("submit", handleToggleProbeTest);
  $("probe-config-form").addEventListener("submit", handleFetchProbeConfig);
  $("probe-plugins-form").addEventListener("submit", handleSetProbePlugins);
  $("delete-probe-btn").addEventListener("click", handleDeleteProbe);

  $("refresh-runtime-btn").addEventListener("click", loadRuntime);
  $("heartbeat-btn").addEventListener("click", handleHeartbeat);
  $("load-pending-btn").addEventListener("click", loadPendingActions);

  $("trigger-action-form").addEventListener("submit", handleTriggerAction);
  $("update-action-status-form").addEventListener("submit", handleUpdateActionStatus);
  $("refresh-history-btn").addEventListener("click", loadActionHistory);
}

function collectTestTypeOptions() {
  const values = new Set();

  (state.probeConfig?.enabledTests || []).forEach(test => {
    if (test?.testType) values.add(test.testType);
  });

  // Only include SCHEDULED plugins, exclude ACTION plugins
  (state.probeConfig?.availablePlugins || []).forEach(plugin => {
    if (plugin?.id && plugin?.executionMode !== "ACTION") values.add(plugin.id);
  });

  (state.runtime?.enabledTests || []).forEach(testType => {
    if (testType) values.add(testType);
  });

  state.assignments.forEach(assignment => {
    if (assignment?.pluginId) values.add(assignment.pluginId);
  });

  // Only include SCHEDULED plugins, exclude ACTION plugins
  state.plugins.forEach(plugin => {
    if (plugin?.id && plugin?.executionMode !== "ACTION") values.add(plugin.id);
  });

  return Array.from(values);
}

function renderTestTypeSelect(selectId, preferredValue) {
  const select = $(selectId);
  if (!select) return;

  const currentValue = preferredValue || select.value;
  const options = collectTestTypeOptions();
  if (!options.length) {
    select.innerHTML = `<option value="">No available tests</option>`;
    select.value = "";
    select.disabled = true;
    return;
  }

  select.disabled = false;
  select.innerHTML = options.map(value => `<option value="${escapeHtml(value)}">${escapeHtml(value)}</option>`).join("");
  select.value = options.includes(currentValue) ? currentValue : options[0];
}

function renderTestTypeSelectors() {
  renderTestTypeSelect("panel-cfg-type");
  renderTestTypeSelect("toggle-type", $("panel-cfg-type")?.value);
}

function bindModals() {
  document.querySelectorAll(".close-modal-btn").forEach(button => {
    button.addEventListener("click", () => closeModal(button.dataset.modal));
  });
  document.querySelectorAll(".modal-bg").forEach(bg => {
    bg.addEventListener("click", event => closeModal(event.target.parentElement.id));
  });
  document.addEventListener("keydown", event => {
    if (event.key !== "Escape") return;
    closeProbePanel();
    closeModal("modal-plugin");
    if (state.settingsOpen) toggleSettings();
  });
}

function switchView(view) {
  state.view = view;
  document.querySelectorAll(".view").forEach(panel => panel.classList.toggle("active", panel.id === `view-${view}`));
  document.querySelectorAll(".ntab").forEach(button => button.classList.toggle("active", button.dataset.view === view));
}

function switchPanelTab(tab) {
  document.querySelectorAll(".ptab").forEach(button => button.classList.toggle("active", button.dataset.panelTab === tab));
  document.querySelectorAll(".panel-view").forEach(panel => panel.classList.toggle("active", panel.dataset.panelView === tab));
}

function toggleSettings() {
  state.settingsOpen = !state.settingsOpen;
  $("settings").classList.toggle("open", state.settingsOpen);
  $("settings-overlay").classList.toggle("open", state.settingsOpen);
}

function toggleDrawer() {
  state.drawerOpen = !state.drawerOpen;
  $("output-drawer").classList.toggle("open", state.drawerOpen);
  $("main").classList.toggle("drawer-open", state.drawerOpen);
}

function openModal(id) {
  $(id).classList.add("open");
  document.body.style.overflow = "hidden";
}

function closeModal(id) {
  const modal = $(id);
  if (modal) modal.classList.remove("open");
  if (!$("probe-panel").classList.contains("open")) document.body.style.overflow = "";
}

async function refreshAll() {
  if (state.refreshing) return;
  state.refreshing = true;
  $("refresh-btn").querySelector("svg").classList.add("spinning");

  try {
    const [fleetData, pluginData] = await Promise.all([
      gql(`query { fleetStatus { probes { id name location ipAddress status lastHeartbeat lastConfigFetch } } }`),
      gql(`query { plugins { id name version checksum description releasedAt available executionMode bundleUrl bundleDownloadUrl } }`)
    ]);

    state.probes = fleetData.fleetStatus.probes || [];
    state.plugins = pluginData.plugins || [];
    if (!state.selectedProbeId && state.probes.length) state.selectedProbeId = state.probes[0].id;
    if (state.selectedProbeId && !state.probes.some(probe => probe.id === state.selectedProbeId)) {
      state.selectedProbeId = null;
      clearProbeState();
    }

    renderFleet();
    renderPlugins();
    renderActionSelectors();
    renderActionsView();
    renderTestTypeSelectors();
    setLog({ fleet: fleetData, plugins: pluginData });
    setConnectionState(true);

    if (state.selectedProbeId && $("probe-panel").classList.contains("open")) {
      await refreshProbeWorkspace();
    }
  } catch (error) {
    setConnectionState(false);
    setLog({ error: String(error) });
    toast("Failed to load data", "err");
  } finally {
    $("refresh-btn").querySelector("svg").classList.remove("spinning");
    state.refreshing = false;
  }
}

function renderFleet() {
  const online = state.probes.filter(probe => heartbeatState(probe.lastHeartbeat, probe.status).label === "ONLINE").length;
  $("stat-total").textContent = state.probes.length;
  $("stat-online").textContent = online;
  $("stat-stale").textContent = state.probes.length - online;

  if (!state.probes.length) {
    $("fleet-body").innerHTML = `<tr><td colspan="6"><div class="empty"><p>No probes found</p><small>Start a probe agent to auto-register it, then come back here to manage it.</small></div></td></tr>`;
    return;
  }

  $("fleet-body").innerHTML = state.probes.map(probe => {
    const hb = heartbeatState(probe.lastHeartbeat, probe.status);
    return `
      <tr data-id="${escapeHtml(probe.id)}" class="${probe.id === state.selectedProbeId ? "selected" : ""}">
        <td><div class="probe-name">${escapeHtml(probe.name)}</div><div class="probe-id">${escapeHtml(probe.id)}</div></td>
        <td>${escapeHtml(probe.location)}</td>
        <td class="mono">${escapeHtml(probe.ipAddress)}</td>
        <td>${statusBadge(probe.status)}</td>
        <td><span class="badge ${hb.className}">${hb.label}</span><div class="stack-item-meta">${escapeHtml(hb.detail)}</div></td>
        <td><button class="btn btn-ghost compact-btn" type="button">Open</button></td>
      </tr>`;
  }).join("");
}

function renderPlugins() {
  const options = state.plugins.map(plugin => `<option value="${escapeHtml(plugin.id)}">${escapeHtml(plugin.name)} - v${escapeHtml(plugin.version)}</option>`).join("");
  $("plugin-fetch-select").innerHTML = state.plugins.length ? options : `<option value="">No plugins loaded</option>`;
  $("bundle-plugin-select").innerHTML = state.plugins.length ? options : `<option value="">No plugins loaded</option>`;

  const actionPlugins = state.plugins.filter(plugin => plugin.executionMode === "ACTION");
  $("action-plugin-summary").textContent = actionPlugins.length
    ? `${actionPlugins.length} action plugin(s) available for manual execution.`
    : "No action-capable plugins registered yet.";

  if (!state.plugins.length) {
    $("plugins-grid").innerHTML = `<div class="empty" style="grid-column:1/-1"><p>No plugins registered</p><small>Register a plugin to manage bundles and assignments.</small></div>`;
    return;
  }

  $("plugins-grid").innerHTML = state.plugins.map(plugin => `
    <div class="plugin-card">
      <div class="plugin-card-top">
        <div>
          <div class="plugin-card-name">${escapeHtml(plugin.name)}</div>
          <div class="plugin-card-meta">${escapeHtml(plugin.id)} · v${escapeHtml(plugin.version)} · ${escapeHtml(plugin.executionMode || "SCHEDULED")}</div>
        </div>
        <span class="badge ${plugin.available ? "badge-ok" : "badge-gray"}">${plugin.available ? "Active" : "Disabled"}</span>
      </div>
      <div class="plugin-card-desc">${escapeHtml(plugin.description || "No description provided.")}</div>
      <div class="plugin-card-actions">
        <button class="btn ${plugin.available ? "btn-danger" : "btn-ghost"} plugin-toggle" data-id="${escapeHtml(plugin.id)}" data-next="${String(!plugin.available)}" type="button">${plugin.available ? "Disable" : "Enable"}</button>
        <button class="btn btn-danger plugin-delete" data-id="${escapeHtml(plugin.id)}" type="button">Delete</button>
      </div>
    </div>`).join("");
}

function renderActionSelectors() {
  const probeOptions = state.probes.map(probe => `<option value="${escapeHtml(probe.id)}">${escapeHtml(probe.name)} (${escapeHtml(probe.id)})</option>`).join("");
  const pluginOptions = state.plugins
    .filter(plugin => plugin.executionMode === "ACTION")
    .map(plugin => `<option value="${escapeHtml(plugin.id)}">${escapeHtml(plugin.name)} (${escapeHtml(plugin.id)})</option>`)
    .join("");

  $("quick-action-probe").innerHTML = probeOptions || `<option value="">No probes</option>`;
  $("quick-action-plugin").innerHTML = pluginOptions || `<option value="">No action plugins</option>`;
  if (state.selectedProbeId) $("quick-action-probe").value = state.selectedProbeId;
  renderPanelActionPluginSelect();
}

function collectPanelActionPluginOptions() {
  const optionsById = new Map();

  (state.probeConfig?.availablePlugins || []).forEach(plugin => {
    if (plugin?.executionMode !== "ACTION" || plugin?.available === false || !plugin?.id) return;
    optionsById.set(plugin.id, { id: plugin.id, label: `${plugin.name || plugin.id} (${plugin.id})` });
  });

  state.assignments.forEach(assignment => {
    if (!assignment?.pluginId || assignment?.pluginAvailable === false) return;
    const plugin = state.plugins.find(item => item.id === assignment.pluginId);
    if (plugin && plugin.executionMode !== "ACTION") return;
    const name = assignment.pluginName || plugin?.name || assignment.pluginId;
    if (!optionsById.has(assignment.pluginId)) {
      optionsById.set(assignment.pluginId, { id: assignment.pluginId, label: `${name} (${assignment.pluginId})` });
    }
  });

  return Array.from(optionsById.values());
}

function renderPanelActionPluginSelect(preferredValue) {
  const select = $("trigger-action-plugin-id");
  if (!select) return;

  const current = preferredValue || select.value;
  const options = collectPanelActionPluginOptions();
  if (!options.length) {
    select.innerHTML = `<option value="">No action plugins for this probe</option>`;
    select.value = "";
    select.disabled = true;
    return;
  }

  select.disabled = false;
  select.innerHTML = options.map(option => `<option value="${escapeHtml(option.id)}">${escapeHtml(option.label)}</option>`).join("");
  select.value = options.some(option => option.id === current) ? current : options[0].id;
}

function renderActionsView() {
  $("actions-pending-list").innerHTML = state.pendingActions.length
    ? state.pendingActions.map(renderActionCard).join("")
    : emptyList("No pending actions claimed yet.");

  $("actions-history-list").innerHTML = state.actionHistory.length
    ? state.actionHistory.map(renderActionCard).join("")
    : emptyList("No action executions loaded.");
}

async function openProbePanel(probeId) {
  state.selectedProbeId = probeId;
  syncProbeHiddenFields(probeId);

  const probe = state.probes.find(item => item.id === probeId);
  $("panel-name").textContent = probe?.name || probeId;
  $("panel-id").textContent = probeId;
  $("status-value").value = probe?.status || "REGISTERED";
  $("panel-status-badge").outerHTML = statusBadge(probe?.status || "REGISTERED", "panel-status-badge");

  $("probe-panel").classList.add("open");
  $("panel-overlay").classList.add("open");
  document.body.style.overflow = "hidden";

  renderFleet();
  await refreshProbeWorkspace();
}

function closeProbePanel() {
  $("probe-panel").classList.remove("open");
  $("panel-overlay").classList.remove("open");
  document.body.style.overflow = "";
}

function syncProbeHiddenFields(probeId) {
  ["status-probe-id", "panel-cfg-probe-id", "toggle-probe-id", "probe-config-probe-id", "probe-plugins-probe-id", "trigger-action-probe-id", "update-action-probe-id"].forEach(id => {
    $(id).value = probeId;
  });
}

async function refreshProbeWorkspace() {
  if (!state.selectedProbeId) return;
  await Promise.allSettled([
    loadAssignments(),
    loadRuntime(),
    loadProbeConfigSummary(),
    loadActionHistory(false)
  ]);
}

async function loadAssignments() {
  try {
    const data = await gql(`query($probeId:String!){probePluginAssignments(probeId:$probeId){probeId pluginId pluginName pluginVersion pluginAvailable assignedAt}}`, { probeId: state.selectedProbeId });
    state.assignments = data.probePluginAssignments || [];
    renderProbePluginAssignments();
  } catch {
    state.assignments = [];
    renderProbePluginAssignments();
  }
}

function renderProbePluginAssignments() {
  const scheduledSelect = $("probe-scheduled-plugins-select");
  const actionSelect = $("probe-action-plugins-select");
  const legacySelect = $("probe-plugins-select");
  const assigned = new Set(state.assignments.map(assignment => assignment.pluginId));

  const scheduledPlugins = state.plugins.filter(plugin => plugin.executionMode !== "ACTION");
  const actionPlugins = state.plugins.filter(plugin => plugin.executionMode === "ACTION");

  if (scheduledSelect) {
    scheduledSelect.innerHTML = scheduledPlugins.map(plugin => {
      const selected = assigned.has(plugin.id);
      return `<option value="${escapeHtml(plugin.id)}" ${selected ? "selected" : ""}>${escapeHtml(plugin.name)} v${escapeHtml(plugin.version)}${plugin.available ? "" : " [disabled]"}</option>`;
    }).join("") || `<option value="" disabled>No scheduled plugins available</option>`;
  }

  if (actionSelect) {
    actionSelect.innerHTML = actionPlugins.map(plugin => {
      const selected = assigned.has(plugin.id);
      return `<option value="${escapeHtml(plugin.id)}" ${selected ? "selected" : ""}>${escapeHtml(plugin.name)} v${escapeHtml(plugin.version)}${plugin.available ? "" : " [disabled]"}</option>`;
    }).join("") || `<option value="" disabled>No action plugins available</option>`;
  }

  if (legacySelect && !scheduledSelect && !actionSelect) {
    legacySelect.innerHTML = state.plugins.map(plugin => {
      const selected = assigned.has(plugin.id);
      return `<option value="${escapeHtml(plugin.id)}" ${selected ? "selected" : ""}>${escapeHtml(plugin.name)} v${escapeHtml(plugin.version)}${plugin.available ? "" : " [disabled]"}${plugin.executionMode === "ACTION" ? " [action]" : ""}</option>`;
    }).join("") || `<option value="">No plugins available</option>`;
  }

  renderTestTypeSelectors();
  renderPanelActionPluginSelect();
}

function getSelectedAssignedPluginIds() {
  const scheduled = $("probe-scheduled-plugins-select");
  const action = $("probe-action-plugins-select");
  const legacy = $("probe-plugins-select");

  if (scheduled || action) {
    const scheduledIds = scheduled
      ? Array.from(scheduled.selectedOptions).map(option => option.value)
      : [];
    const actionIds = action
      ? Array.from(action.selectedOptions).map(option => option.value)
      : [];
    return Array.from(new Set([...scheduledIds, ...actionIds])).filter(Boolean);
  }

  return legacy
    ? Array.from(legacy.selectedOptions).map(option => option.value).filter(Boolean)
    : [];
}

async function loadRuntime() {
  if (!state.selectedProbeId) return;
  try {
    state.runtime = await api(`/probes/${encodeURIComponent(state.selectedProbeId)}/runtime-state`);
    $("runtime-summary").innerHTML = `
      ${stackLine("Status", state.runtime.status)}
      ${stackLine("Metrics emission", state.runtime.canEmitMetrics ? "Enabled" : "Blocked")}
      ${stackLine("Enabled tests", (state.runtime.enabledTests || []).join(", ") || "None")}
      ${stackLine("Polled", formatDate(state.runtime.polledAtUtc))}
    `;
    renderTestTypeSelectors();
  } catch (error) {
    $("runtime-summary").innerHTML = emptyList(error.message);
  }
}

async function loadProbeConfigSummary() {
  if (!state.selectedProbeId) return;
  try {
    const data = await gql(`query($probeId:String!){probeConfig(probeId:$probeId){probeId enabledTests{testType intervalSeconds enabled} availablePlugins{id name version executionMode available}}}`, { probeId: state.selectedProbeId });
    state.probeConfig = data.probeConfig;
    $("probe-config-summary").innerHTML = [
      stackLine("Enabled tests", (state.probeConfig.enabledTests || []).map(test => `${test.testType} (${test.intervalSeconds}s)`).join(", ") || "None"),
      stackLine("Available plugins", (state.probeConfig.availablePlugins || []).map(plugin => `${plugin.id} [${plugin.executionMode}]`).join(", ") || "None")
    ].join("");
    renderTestTypeSelectors();
    renderPanelActionPluginSelect();
  } catch (error) {
    $("probe-config-summary").innerHTML = emptyList(error.message);
  }
}

async function loadPendingActions(showToast = true) {
  if (!state.selectedProbeId) return;
  try {
    const payload = await api(`/probes/${encodeURIComponent(state.selectedProbeId)}/actions/pending?limit=10`);
    state.pendingActions = payload.actions || [];
    $("panel-pending-actions").innerHTML = state.pendingActions.length ? state.pendingActions.map(renderActionCard).join("") : emptyList("No pending actions claimed.");
    renderActionsView();
    if (showToast) toast(`Claimed ${state.pendingActions.length} pending action(s)`, "ok");
  } catch (error) {
    $("panel-pending-actions").innerHTML = emptyList(error.message);
    if (showToast) toast("Failed to claim pending actions", "err");
  }
}

async function loadActionHistory(showToast = true) {
  if (!state.selectedProbeId) return;
  try {
    const data = await gql(`query($probeId:String!,$limit:Int){probeActionExecutions(probeId:$probeId,limit:$limit){executionId probeId pluginId triggeredBy status requestedAtUtc deliveredAtUtc startedAtUtc completedAtUtc errorMessage}}`, { probeId: state.selectedProbeId, limit: 20 });
    state.actionHistory = data.probeActionExecutions || [];
    $("panel-action-history").innerHTML = state.actionHistory.length ? state.actionHistory.map(renderActionCard).join("") : emptyList("No executions found.");
    renderActionsView();
    if (showToast) toast("Action history refreshed", "ok");
  } catch (error) {
    $("panel-action-history").innerHTML = emptyList(error.message);
    if (showToast) toast("Failed to load action history", "err");
  }
}

async function refreshActionsView() {
  if (!state.selectedProbeId && state.probes.length) state.selectedProbeId = $("quick-action-probe").value || state.probes[0].id;
  await Promise.allSettled([loadActionHistory(false)]);
  renderActionsView();
  toast("Actions view refreshed", "ok");
}

async function handleRegisterPlugin(event) {
  event.preventDefault();
  try {
    const data = await gql(`mutation($input:RegisterPluginInputTypeInput!){registerPlugin(input:$input){success message plugin{id}}}`, {
      input: {
        id: $("plugin-reg-id").value.trim(),
        name: $("plugin-reg-name").value.trim(),
        version: $("plugin-reg-version").value.trim(),
        checksum: $("plugin-reg-checksum").value.trim(),
        description: $("plugin-reg-desc").value.trim() || null,
        bundleDownloadUrl: $("plugin-reg-url").value.trim() || null,
        dashboardJson: $("plugin-reg-json").value.trim() || null,
        executionMode: $("plugin-reg-mode").value
      }
    });
    ensureSuccess(data.registerPlugin);
    setLog(data);
    closeModal("modal-plugin");
    toast(data.registerPlugin.message || "Plugin registered", "ok");
    await refreshAll();
  } catch (error) {
    setLog({ error: String(error) });
    toast("Plugin registration failed", "err");
  }
}

async function handleFetchPlugin(event) {
  event.preventDefault();
  const pluginId = $("plugin-fetch-select").value;
  if (!pluginId) return toast("Select a plugin first", "warn");
  try {
    const data = await gql(`query($id:String!){plugin(id:$id){id name version description available executionMode bundleUrl bundleDownloadUrl}}`, { id: pluginId });
    setLog(data);
    if (!state.drawerOpen) toggleDrawer();
    toast("Plugin details loaded into the log", "info");
  } catch (error) {
    setLog({ error: String(error) });
    toast("Failed to fetch plugin details", "err");
  }
}

async function handleDownloadBundle(event) {
  event.preventDefault();
  const pluginId = $("bundle-plugin-select").value;
  const version = $("bundle-version").value.trim();
  if (!pluginId || !version) return toast("Choose a plugin and version", "warn");

  try {
    const response = await fetch(`/plugins/${encodeURIComponent(pluginId)}/${encodeURIComponent(version)}/bundle`, {
      method: "GET",
      headers: authHeaders()
    });
    if (!response.ok) throw new Error(await response.text() || `HTTP ${response.status}`);
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const link = Object.assign(document.createElement("a"), { href: url, download: `${pluginId}-${version}.zip` });
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    setLog({ pluginId, version, status: "downloaded" });
    toast("Bundle downloaded", "ok");
  } catch (error) {
    setLog({ error: String(error) });
    toast("Bundle download failed", "err");
  }
}

async function handleTogglePlugin(pluginId, nextAvailable) {
  try {
    const data = await gql(`mutation($input:SetPluginAvailabilityInputTypeInput!){setPluginAvailability(input:$input){success message plugin{id available}}}`, {
      input: { pluginId, available: nextAvailable }
    });
    ensureSuccess(data.setPluginAvailability);
    setLog(data);
    toast(`Plugin ${nextAvailable ? "enabled" : "disabled"}`, "ok");
    await refreshAll();
  } catch (error) {
    setLog({ error: String(error) });
    toast("Plugin toggle failed", "err");
  }
}

async function handleDeletePlugin(pluginId) {
  if (!confirm(`Delete plugin '${pluginId}'?`)) return;
  try {
    const data = await gql(`mutation($pluginId:String!){deletePlugin(pluginId:$pluginId){success message pluginId}}`, { pluginId });
    ensureSuccess(data.deletePlugin);
    setLog(data);
    toast(`Plugin ${pluginId} deleted`, "ok");
    await refreshAll();
  } catch (error) {
    setLog({ error: String(error) });
    toast("Plugin delete failed", "err");
  }
}

async function handleUpdateProbeStatus(event) {
  event.preventDefault();
  if (!state.selectedProbeId) return toast("No probe selected", "warn");
  try {
    const data = await gql(`mutation($probeId:String!,$status:String!){updateProbeStatus(probeId:$probeId,status:$status){success message probe{id status}}}`, {
      probeId: state.selectedProbeId,
      status: $("status-value").value
    });
    ensureSuccess(data.updateProbeStatus);
    setLog(data);
    toast("Probe status updated", "ok");
    await refreshAll();
  } catch (error) {
    setLog({ error: String(error) });
    toast("Status update failed", "err");
  }
}

async function handleUpdateProbeTestConfig(event) {
  event.preventDefault();
  if (!state.selectedProbeId) return toast("No probe selected", "warn");
  const testType = $("panel-cfg-type").value;
  if (!testType) return toast("No available tests for this probe", "warn");
  try {
    const data = await gql(`mutation($input:UpdateProbeTestConfigInputTypeInput!){updateProbeTestConfig(input:$input){success message config{probeId testType intervalSeconds enabled}}}`, {
      input: {
        probeId: state.selectedProbeId,
        testType,
        intervalSeconds: Number($("panel-cfg-interval").value),
        enabled: $("panel-cfg-enabled").value === "true"
      }
    });
    ensureSuccess(data.updateProbeTestConfig);
    setLog(data);
    toast("Test configuration updated", "ok");
    await loadProbeConfigSummary();
  } catch (error) {
    setLog({ error: String(error) });
    toast("Config update failed", "err");
  }
}

async function handleToggleProbeTest(event) {
  event.preventDefault();
  if (!state.selectedProbeId) return toast("No probe selected", "warn");
  const testType = $("toggle-type").value;
  if (!testType) return toast("No available tests for this probe", "warn");
  try {
    const data = await gql(`mutation($input:SetProbeTestEnabledInputTypeInput!){setProbeTestEnabled(input:$input){success message config{probeId testType enabled}}}`, {
      input: {
        probeId: state.selectedProbeId,
        testType,
        enabled: $("toggle-enabled").value === "true"
      }
    });
    ensureSuccess(data.setProbeTestEnabled);
    setLog(data);
    toast("Test toggle applied", "ok");
    await loadProbeConfigSummary();
  } catch (error) {
    setLog({ error: String(error) });
    toast("Toggle failed", "err");
  }
}

async function handleFetchProbeConfig(event) {
  event.preventDefault();
  await loadProbeConfigSummary();
  setLog({ probeConfig: state.probeConfig });
  if (!state.drawerOpen) toggleDrawer();
  toast("Probe configuration loaded", "info");
}

async function handleSetProbePlugins(event) {
  event.preventDefault();
  if (!state.selectedProbeId) return toast("No probe selected", "warn");
  const pluginIds = getSelectedAssignedPluginIds();
  try {
    const data = await gql(`mutation($input:SetProbePluginsInputTypeInput!){setProbePlugins(input:$input){success message assignments{pluginId pluginName pluginVersion pluginAvailable}}}`, {
      input: { probeId: state.selectedProbeId, pluginIds }
    });
    ensureSuccess(data.setProbePlugins);
    state.assignments = data.setProbePlugins.assignments || [];
    renderProbePluginAssignments();
    setLog(data);
    toast("Assignments saved", "ok");
  } catch (error) {
    setLog({ error: String(error) });
    toast("Failed to save assignments", "err");
  }
}

async function handleDeleteProbe() {
  if (!state.selectedProbeId || !confirm(`Delete probe '${state.selectedProbeId}'?`)) return;
  try {
    const data = await gql(`mutation($probeId:String!){deleteProbe(probeId:$probeId){success message probeId}}`, { probeId: state.selectedProbeId });
    ensureSuccess(data.deleteProbe);
    setLog(data);
    toast(`Probe ${state.selectedProbeId} deleted`, "ok");
    state.selectedProbeId = null;
    clearProbeState();
    closeProbePanel();
    await refreshAll();
  } catch (error) {
    setLog({ error: String(error) });
    toast("Probe delete failed", "err");
  }
}

async function handleHeartbeat() {
  if (!state.selectedProbeId) return toast("No probe selected", "warn");
  try {
    const data = await api(`/probes/${encodeURIComponent(state.selectedProbeId)}/heartbeat`, { method: "POST" });
    setLog(data);
    toast("Heartbeat recorded", "ok");
    await Promise.allSettled([refreshAll(), loadRuntime()]);
  } catch (error) {
    setLog({ error: String(error) });
    toast("Heartbeat failed", "err");
  }
}

async function handleTriggerAction(event) {
  event.preventDefault();
  if (!state.selectedProbeId) return toast("No probe selected", "warn");
  const pluginId = $("trigger-action-plugin-id").value;
  if (!pluginId) return toast("No action plugin available for this probe", "warn");
  try {
    const data = await gql(`mutation($input:TriggerProbeActionInputTypeInput!){triggerProbeAction(input:$input){success message execution{executionId probeId pluginId status requestedAtUtc}}}`, {
      input: {
        probeId: state.selectedProbeId,
        pluginId,
        triggeredBy: $("trigger-action-triggered-by").value.trim()
      }
    });
    ensureSuccess(data.triggerProbeAction);
    setLog(data);
    $("update-action-execution-id").value = data.triggerProbeAction.execution.executionId;
    toast(`Action queued as ${data.triggerProbeAction.execution.executionId}`, "ok");
    await Promise.allSettled([loadActionHistory(false)]);
    renderActionsView();
  } catch (error) {
    setLog({ error: String(error) });
    toast("Action queue failed", "err");
  }
}

async function handleQuickAction(event) {
  event.preventDefault();
  const probeId = $("quick-action-probe").value;
  const pluginId = $("quick-action-plugin").value;
  if (!probeId || !pluginId) return toast("Choose a probe and action plugin", "warn");
  state.selectedProbeId = probeId;
  syncProbeHiddenFields(probeId);
  try {
    const data = await gql(`mutation($input:TriggerProbeActionInputTypeInput!){triggerProbeAction(input:$input){success message execution{executionId probeId pluginId status requestedAtUtc}}}`, {
      input: {
        probeId,
        pluginId,
        triggeredBy: $("quick-action-triggered-by").value.trim()
      }
    });
    ensureSuccess(data.triggerProbeAction);
    setLog(data);
    toast("Action queued", "ok");
    await Promise.allSettled([loadActionHistory(false)]);
    renderActionsView();
  } catch (error) {
    setLog({ error: String(error) });
    toast("Quick action failed", "err");
  }
}

async function handleUpdateActionStatus(event) {
  event.preventDefault();
  if (!state.selectedProbeId) return toast("No probe selected", "warn");
  try {
    const data = await api(`/probes/${encodeURIComponent(state.selectedProbeId)}/actions/${encodeURIComponent($("update-action-execution-id").value.trim())}/status`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        status: $("update-action-status").value,
        errorMessage: $("update-action-error").value.trim() || null
      })
    });
    setLog(data);
    toast("Execution status updated", "ok");
    await Promise.allSettled([loadActionHistory(false)]);
    renderActionsView();
  } catch (error) {
    setLog({ error: String(error) });
    toast("Failed to update execution", "err");
  }
}

async function handleOpenDashboard() {
  try {
    const payload = await api("/monitoring/grafana/embed-session", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ site: $("monitor-site").value.trim() || "default" })
    });
    $("monitor-frame").src = payload.embedUrl;
    $("monitor-frame").style.display = "block";
    $("dash-placeholder").style.display = "none";
    setLog(payload);
    toast(`Dashboard loaded for ${payload.site}`, "ok");
  } catch (error) {
    setLog({ error: String(error) });
    toast("Failed to open dashboard", "err");
  }
}

async function handleLoadServiceDiscovery(event) {
  event.preventDefault();
  try {
    const response = await fetch(`/monitoring/prometheus/service-discovery?token=${encodeURIComponent($("service-discovery-token").value.trim())}`);
    const payload = await response.json();
    if (!response.ok) throw new Error(payload?.message || payload?.detail || `HTTP ${response.status}`);
    $("service-discovery-list").innerHTML = payload.length
      ? payload.map(group => `<div class="stack-item"><div class="stack-item-title">${escapeHtml(group.labels.probe_id)}<span class="badge badge-sky">${escapeHtml(group.labels.site)}</span></div><div class="stack-item-sub">${escapeHtml(group.targets.join(", "))}</div></div>`).join("")
      : emptyList("No active probes returned.");
    setLog(payload);
    toast("Service discovery loaded", "ok");
  } catch (error) {
    setLog({ error: String(error) });
    toast("Failed to load service discovery", "err");
  }
}

function clearProbeState() {
  state.assignments = [];
  state.runtime = null;
  state.probeConfig = null;
  state.pendingActions = [];
  state.actionHistory = [];
  $("runtime-summary").innerHTML = emptyList("No runtime data loaded.");
  $("probe-config-summary").innerHTML = emptyList("No configuration snapshot loaded.");
  $("panel-pending-actions").innerHTML = emptyList("No pending actions claimed.");
  $("panel-action-history").innerHTML = emptyList("No executions loaded.");
  renderActionsView();
}

function heartbeatState(lastHeartbeat, status) {
  const normalized = String(status || "").toUpperCase();
  if (normalized === "DECOMMISSIONED") return { label: "DECOMM", className: "badge-danger", detail: "-" };
  if (normalized === "INACTIVE") return { label: "INACTIVE", className: "badge-warn", detail: "-" };
  if (!lastHeartbeat) return { label: "UNKNOWN", className: "badge-gray", detail: "-" };
  const date = new Date(lastHeartbeat);
  if (Number.isNaN(date.getTime())) return { label: "UNKNOWN", className: "badge-gray", detail: String(lastHeartbeat) };
  const age = Math.max(0, Math.floor((Date.now() - date.getTime()) / 1000));
  return age <= staleSeconds
    ? { label: "ONLINE", className: "badge-ok", detail: `${age}s ago` }
    : { label: "STALE", className: "badge-warn", detail: `${Math.floor(age / 60)}m ago` };
}

function statusBadge(status, id = null) {
  const normalized = String(status || "REGISTERED").toUpperCase();
  const className =
    normalized === "ACTIVE" || normalized === "SUCCEEDED" ? "badge-ok" :
    normalized === "INACTIVE" || normalized === "RUNNING" || normalized === "DELIVERED" ? "badge-warn" :
    normalized === "FAILED" || normalized === "TIMED_OUT" || normalized === "DECOMMISSIONED" ? "badge-danger" :
    "badge-sky";
  return `<span${id ? ` id="${id}"` : ""} class="badge ${className}">${escapeHtml(normalized)}</span>`;
}

function renderActionCard(action) {
  return `
    <div class="stack-item">
      <div class="stack-item-title">
        <span>${escapeHtml(action.pluginId)} ${statusBadge(action.status)}</span>
        <span class="mono">${escapeHtml(action.executionId)}</span>
      </div>
      <div class="stack-item-sub">Probe: ${escapeHtml(action.probeId)} · Triggered by ${escapeHtml(action.triggeredBy || "system")}</div>
      <div class="stack-item-meta">Requested ${formatDate(action.requestedAtUtc)}${action.errorMessage ? ` · ${escapeHtml(action.errorMessage)}` : ""}</div>
    </div>`;
}

function stackLine(label, value) {
  return `<div class="stack-item"><div class="stack-item-title">${escapeHtml(label)}</div><div class="stack-item-sub">${escapeHtml(value)}</div></div>`;
}

function emptyList(message) {
  return `<div class="empty"><p>${escapeHtml(message)}</p></div>`;
}

function setConnectionState(isOk) {
  const endpoint = $("endpoint").value.trim() || "/graphql";
  if (isOk === null) {
    $("conn-dot").style.background = "var(--hint)";
    $("conn-text").textContent = "Configure connection";
  } else if (isOk) {
    $("conn-dot").style.background = "var(--success)";
    $("conn-text").textContent = endpoint;
  } else {
    $("conn-dot").style.background = "var(--danger)";
    $("conn-text").textContent = "Connection error";
  }
}

function setLog(data) {
  $("output").value = JSON.stringify(data, null, 2);
  $("log-time").textContent = new Date().toLocaleTimeString();
}

function toast(message, type = "info") {
  const root = $("toast-root");
  const toast = document.createElement("div");
  toast.className = `toast ${{
    ok: "toast-ok",
    err: "toast-err",
    warn: "toast-warn",
    info: "toast-info"
  }[type] || "toast-info"}`;
  toast.textContent = message;
  root.appendChild(toast);
  setTimeout(() => {
    toast.style.animation = "tOut .2s ease forwards";
    toast.addEventListener("animationend", () => toast.remove(), { once: true });
  }, 3500);
}

function ensureSuccess(result) {
  if (!result?.success) throw new Error(result?.message || "Operation failed.");
}

async function gql(query, variables = {}) {
  const response = await fetch($("endpoint").value.trim() || "/graphql", {
    method: "POST",
    headers: authHeaders({ "Content-Type": "application/json" }),
    body: JSON.stringify({ query, variables })
  });
  const payload = await response.json().catch(() => null);
  if (!response.ok) {
    if (payload?.errors) throw new Error(JSON.stringify(payload.errors, null, 2));
    throw new Error(`HTTP ${response.status}`);
  }
  if (!payload) throw new Error("Empty or invalid JSON response");
  if (payload.errors) throw new Error(payload.errors.map(error => error.message).join("\n"));
  if (!Object.prototype.hasOwnProperty.call(payload, "data")) throw new Error("Response missing data field");
  return payload.data;
}

async function api(url, options = {}) {
  const response = await fetch(url, {
    ...options,
    headers: authHeaders(options.headers || {})
  });
  const payload = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(payload?.message || payload?.detail || `HTTP ${response.status}`);
  return payload;
}

function authHeaders(extra = {}) {
  const headers = new Headers(extra);
  const apiKey = $("apiKey").value.trim();
  if (apiKey) headers.set("X-Api-Key", apiKey);
  return headers;
}

function formatDate(value) {
  if (!value) return "-";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString();
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}
