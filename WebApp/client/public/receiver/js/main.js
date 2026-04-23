import { getServerConfig, getRTCConfiguration } from "../../js/config.js";
import { RenderStreaming } from "../../module/renderstreaming.js";
import { Signaling, WebSocketSignaling } from "../../module/signaling.js";

/** @type {HTMLImageElement} */
let playButton;
/** @type {boolean} */
let useWebSocket;
/** @type {boolean} */
let streamingActive = false;

const playerGrid = document.getElementById('playerGrid');
const messageDiv = document.getElementById('message');
messageDiv.style.display = 'none';

/** @type {HTMLInputElement[]} */
const cameraCheckboxes = Array.from(document.querySelectorAll('input.cameraCheckbox'));

/**
 * Per-camera stream record.
 * @typedef {{
 *   cameraValue: string,
 *   label: string,
 *   signaling: any,
 *   renderstreaming: any,
 *   container: HTMLDivElement,
 *   videoElement: HTMLVideoElement,
 * }} StreamEntry
 * @type {Map<string, StreamEntry>}
 */
const streams = new Map();

setup();

window.document.oncontextmenu = function () {
  return false; // cancel default menu
};

window.addEventListener('beforeunload', async () => {
  for (const value of Array.from(streams.keys())) {
    await stopStream(value);
  }
}, true);

async function setup() {
  const res = await getServerConfig();
  useWebSocket = res.useWebSocket;
  showWarningIfNeeded(res.startupMode);

  // Wire checkbox handlers up-front so their state is observable,
  // but creating connections is deferred until the user clicks Play.
  for (const cb of cameraCheckboxes) {
    cb.addEventListener('change', onCheckboxChange);
  }

  showPlayButton();
}

function showWarningIfNeeded(startupMode) {
  const warningDiv = document.getElementById("warning");
  if (startupMode == "private") {
    warningDiv.innerHTML = "<h4>Warning</h4> This sample is not working on Private Mode.";
    warningDiv.hidden = false;
  }
}

function showPlayButton() {
  if (!document.getElementById('playButton')) {
    const elementPlayButton = document.createElement('img');
    elementPlayButton.id = 'playButton';
    elementPlayButton.src = '../../images/Play.png';
    elementPlayButton.alt = 'Start Streaming';
    playButton = playerGrid.appendChild(elementPlayButton);
    playButton.addEventListener('click', onClickPlayButton);
  }
}

async function onClickPlayButton() {
  playButton.style.display = 'none';
  streamingActive = true;

  for (const cb of cameraCheckboxes) {
    if (cb.checked) {
      await startStream(cb.value, cb.dataset.label || cb.value);
    }
  }
  updateGridLayout();
}

async function onCheckboxChange(evt) {
  const cb = /** @type {HTMLInputElement} */ (evt.target);
  if (!streamingActive) {
    // User is just pre-selecting before clicking Play. Do nothing yet.
    return;
  }

  if (cb.checked) {
    await startStream(cb.value, cb.dataset.label || cb.value);
  } else {
    await stopStream(cb.value);
  }
  updateGridLayout();
}

async function startStream(cameraValue, label) {
  if (streams.has(cameraValue)) return;

  const container = document.createElement('div');
  container.className = 'streamTile';

  const videoElement = document.createElement('video');
  videoElement.playsInline = true;
  videoElement.autoplay = true;
  videoElement.muted = true;
  videoElement.srcObject = new MediaStream();
  container.appendChild(videoElement);

  const labelEl = document.createElement('div');
  labelEl.className = 'streamLabel';
  labelEl.textContent = label;
  container.appendChild(labelEl);

  playerGrid.appendChild(container);

  const signaling = useWebSocket ? new WebSocketSignaling() : new Signaling();
  const config = getRTCConfiguration();
  const renderstreaming = new RenderStreaming(signaling, config);

  renderstreaming.onTrackEvent = (data) => {
    if (videoElement.srcObject) {
      videoElement.srcObject.addTrack(data.track);
      videoElement.play().catch(() => { /* autoplay may be gated; muted helps */ });
    }
  };
  renderstreaming.onConnect = () => {
    // A data channel must be created so the peer generates an SDP offer
    // with `a=rid:<cameraValue>`. Without it, no renegotiation is triggered
    // and Unity never sends back a video track. The channel itself is
    // unused for passive viewers but is harmless.
    try {
      renderstreaming.createDataChannel('input');
    } catch (e) {
      console.warn(`createDataChannel failed for ${label}:`, e);
    }
  };
  renderstreaming.onDisconnect = () => {
    stopStream(cameraValue).then(updateGridLayout);
  };

  /** @type {StreamEntry} */
  const entry = { cameraValue, label, signaling, renderstreaming, container, videoElement };
  streams.set(cameraValue, entry);

  try {
    await renderstreaming.start();
    await renderstreaming.createConnection(null, cameraValue);
  } catch (e) {
    console.error(`Failed to start stream for ${label}:`, e);
    await stopStream(cameraValue);
  }
}

async function stopStream(cameraValue) {
  const entry = streams.get(cameraValue);
  if (!entry) return;
  streams.delete(cameraValue);

  try {
    await entry.renderstreaming.stop();
  } catch (e) {
    console.warn(`stop failed for ${entry.label}:`, e);
  }

  if (entry.container.parentElement) {
    entry.container.parentElement.removeChild(entry.container);
  }
}

function updateGridLayout() {
  const n = streams.size;
  if (n === 0) {
    playerGrid.style.gridTemplateColumns = '1fr';
    playerGrid.style.gridTemplateRows = '1fr';
    if (streamingActive && !document.getElementById('playButton')) {
      // No streams but session was started: let user re-add by clicking the button again
      showPlayButton();
      playButton.style.display = '';
      streamingActive = false;
    }
    return;
  }
  const cols = Math.ceil(Math.sqrt(n));
  const rows = Math.ceil(n / cols);
  playerGrid.style.gridTemplateColumns = `repeat(${cols}, 1fr)`;
  playerGrid.style.gridTemplateRows = `repeat(${rows}, 1fr)`;
}
