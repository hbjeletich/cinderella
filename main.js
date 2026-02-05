import { $, show, hide, sendJSON, getInputType } from './utils.js';
const ws = new WebSocket("wss://hadlee-giggliest-unpopulously.ngrok-free.dev/");

const textInput = $('textInput');
const sendBtn = $('sendBtn');
const status = $('status');
const startBtn = $('startBtn');
const title = $('title');
const textLabel = $('label');

ws.onopen = () => {
	console.log("WS connected");
	status.textContent = 'Connected';
	sendBtn.disabled = false;
};

var sendType = "join";

ws.onerror = e => console.error("WS error", e);

ws.onmessage = (e) => {
	const message = JSON.parse(e.data);

	switch (message.type) {
		case "joined":
			hide(textInput);
			hide(sendBtn);
			hide(textLabel);

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
			status.textContent = 'Game started! Waiting for instructions...';
			break;

		case "show_prompt":
			// status.textContent = message.text;
            sendType = "send_prompt";
            hide(status);
            show(textLabel);
            textLabel.textContent = message.text;
            show(getInputType(message.inputType));
            show(sendBtn);
			break;
		case "show_answer":
			hideAllInputTypes();
			show(status);
			status.textContent = message.text;
			show($('reactContainer'));
			break;
			
	}
};

sendBtn.addEventListener('click', () => {
	const name = textInput.value.trim();
	if (!name) return;

	sendJSON(ws, { type: sendType, playerName: name });
    show(status);
	status.textContent = 'Thanks!';
    textInput.value = '';  
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
        sendJSON(ws, { type: "send_react", reaction: reaction });
        hide($('reactContainer'));
        show(status);
        status.textContent = 'Reaction sent!';
    });
});
