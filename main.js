import { $, show, hide, sendJSON, getInputType } from './utils.js';
const ws = new WebSocket("wss://hadlee-giggliest-unpopulously.ngrok-free.dev/");

const textInput = $('textInput');
const sendBtn = $('sendBtn');
const status = $('status');
const startBtn = $('startBtn');
const title = $('title');
const nameLabel = $('nameLabel');

ws.onopen = () => {
	console.log("WS connected");
	status.textContent = 'Connected';
	sendBtn.disabled = false;
};

ws.onerror = e => console.error("WS error", e);

ws.onmessage = (e) => {
	const message = JSON.parse(e.data);

	switch (message.type) {
		case "joined":
			hide(textInput);
			hide(sendBtn);
			hide(nameLabel);

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
			break;

		case "start_game":
			hide(startBtn);
			show(status);
			status.textContent = 'STARTING THE GAME!';
			break;

		case "show_prompt":
			status.textContent = message.text;
            show(getInputType(message.inputType));
            show(sendBtn);
			break;
	}
};

sendBtn.addEventListener('click', () => {
	const name = textInput.value.trim();
	if (!name) return;

	sendJSON(ws, { type: "join", playerName: name });
	status.textContent = `Sent: ${name}`;
});

startBtn.addEventListener('click', () => {
	sendJSON(ws, { type: "start_game" });
	status.textContent = 'Game started!';
});
