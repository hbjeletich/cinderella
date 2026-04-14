import { $, show, hide, sendJSON, getInputType, hideAllInputTypes, getOrCreateDeviceId } from './utils.js';

const WS_URL = "wss://hadlee-giggliest-unpopulously.ngrok-free.dev/";
const RECONNECT_DELAY_MS = 2000;
const MAX_RECONNECT_DELAY_MS = 15000;

const deviceId = getOrCreateDeviceId();
const textInput = $('textInput');
const sendBtn = $('sendBtn');
const status = $('status');
const startBtn = $('startBtn');
const title = $('title');
const textLabel = $('label');
const reactContainer = $('reactContainer');
const choiceContainer = $('choiceContainer');
const choiceGrid = $('choiceGrid');

var sendType = "join";
var ws = null;
var reconnectDelay = RECONNECT_DELAY_MS;
var reconnectTimeout = null;
var isIntentionalClose = false;

// ---- WebSocket connection with auto-reconnect ----

function connect() {
	if (reconnectTimeout) {
		clearTimeout(reconnectTimeout);
		reconnectTimeout = null;
	}

	ws = new WebSocket(WS_URL);

	ws.onopen = () => {
		console.log("WS connected");
		status.textContent = 'Connected';
		reconnectDelay = RECONNECT_DELAY_MS; // reset backoff on success
	};

	ws.onerror = (e) => {
		console.error("WS error", e);
	};

	ws.onclose = () => {
		console.log("WS closed");
		if (isIntentionalClose) return;

		status.textContent = 'Reconnecting...';
		// exponential backoff with cap
		reconnectTimeout = setTimeout(() => {
			console.log(`WS reconnecting (delay: ${reconnectDelay}ms)...`);
			connect();
		}, reconnectDelay);
		reconnectDelay = Math.min(reconnectDelay * 1.5, MAX_RECONNECT_DELAY_MS);
	};

	ws.onmessage = (e) => {
		const message = JSON.parse(e.data);
		handleMessage(message);
	};
}

// ---- Message handler ----

function handleMessage(message) {
	switch (message.type) {
		case "session":
			handleSession(message);
			break;

		case "joined":
			handleJoined(message);
			break;

		case "rejoined":
			handleRejoined(message);
			break;

		case "error":
			handleError(message);
			break;

		case "start_game":
			hide(startBtn);
			show(status);
			status.textContent = 'Game started! Waiting for instructions...';
			break;

		case "show_prompt":
			sendType = "send_prompt";
			hide(status);
			show(textLabel);
			textLabel.textContent = message.text;
			show(getInputType(message.inputType));
			show(sendBtn);
			break;

		case "show_answer":
			handleShowAnswer(message);
			break;

		case "show_choices":
			handleShowChoices(message);
			break;
	}
}

function handleSession(message) {
	const storedSession = localStorage.getItem('sessionId');
	if (storedSession && storedSession === message.sessionId) {
		// same server session — try to rejoin
		const savedName = localStorage.getItem('playerName');
		if (savedName) {
			sendJSON(ws, { type: "join", text: savedName, deviceId: deviceId });
			status.textContent = 'Reconnecting...';
			return;
		}
	}
	// new session — clear old data and let them enter a name
	localStorage.setItem('sessionId', message.sessionId);
	localStorage.removeItem('playerName');
	sendBtn.disabled = false;
}

function handleJoined(message) {
	hide(textInput);
	hide(sendBtn);
	hide(textLabel);

	// save name so we can auto-rejoin later
	localStorage.setItem('playerName', message.playerName);
	title.textContent = `Hi, ${message.playerName}!`;

	if (!message.readyToStart) {
		status.textContent = 'Waiting for players...';

		if (message.isHost) {
			show(startBtn);
			startBtn.disabled = true;
		}
	} else if (message.isHost) {
		hide(status);
		startBtn.disabled = false;
	} else {
		status.textContent = 'Waiting for host...';
	}
}

function handleRejoined(message) {
	hide(textInput);
	hide(sendBtn);
	hide(textLabel);
	hide(choiceContainer);
	hide(reactContainer);

	title.textContent = `Welcome back, ${message.playerName}!`;
	show(status);
	status.textContent = 'Reconnected! Waiting for next prompt...';

	// the server will re-send the current prompt/choices if there's
	// an active phase, which will be handled by show_prompt / show_answer /
	// show_choices message handlers automatically.
}

function handleError(message) {
	show(status);
	status.textContent = message.text;
	// if name was rejected, clear stored name so they can try again
	localStorage.removeItem('playerName');
	show(textLabel);
	show(textInput);
	show(sendBtn);
	sendBtn.disabled = false;
	sendType = "join";
}

function handleShowAnswer(message) {
	sendType = "send_react";
	hideAllInputTypes();
	show(status);
	if (message.myPrompt) {
		hide(reactContainer);
		status.textContent = "This is your answer... don't second guess it...";
		return;
	}
	if (message.promptText) {
		show(textLabel);
		textLabel.textContent = message.promptText;
	}
	status.textContent = message.text;
	show(reactContainer);
}

function handleShowChoices(message) {
	sendType = "send_choice";
	hideAllInputTypes();
	hide(reactContainer);
	show(status);

	if (message.myPrompt) {
		status.textContent = "This is your answer... don't second guess it...";
		return;
	}

	if (message.promptText) {
		show(textLabel);
		textLabel.textContent = message.promptText;
	}
	status.textContent = "Choose an answer carefully!";

	// parse choices and create buttons
	const choices = message.text.split('|');
	choiceGrid.innerHTML = '';

	choices.forEach(choice => {
		const btn = document.createElement('button');
		btn.className = 'choice-btn';
		btn.textContent = choice.trim();
		btn.addEventListener('click', () => {
			sendJSON(ws, { type: sendType, text: choice.trim() });
			hide(choiceContainer);
			show(status);
			status.textContent = 'Thanks!';
		});
		choiceGrid.appendChild(btn);
	});

	show(choiceContainer);
}

// ---- UI event listeners ----

sendBtn.addEventListener('click', () => {
	const text = textInput.value.trim();
	if (!text) return;

	if (sendType === "join") {
		sendJSON(ws, { type: "join", text: text, deviceId: deviceId });
	} else {
		sendJSON(ws, { type: sendType, text: text });
	}
	show(status);

	status.textContent = 'Thanks!';
	textInput.value = '';
	hide(textInput);
	hide(textLabel);
	hide(sendBtn);
});

startBtn.addEventListener('click', () => {
	sendJSON(ws, { type: "start_game" });
	status.textContent = 'Game started!';
});

textInput.addEventListener('keydown', (e) => {
	if (e.key === 'Enter') {
		sendBtn.click();
	}
});

document.querySelectorAll('.react-btn').forEach(btn => {
	btn.addEventListener('click', () => {
		const reaction = btn.dataset.react;

		sendJSON(ws, { type: sendType, text: reaction });
		show(status);
		status.textContent = 'Thanks!';
		hide(reactContainer);
	});
});

// ---- Start connection ----
connect();