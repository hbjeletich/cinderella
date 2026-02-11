import { $, show, hide, sendJSON, getInputType, hideAllInputTypes } from './utils.js';
const ws = new WebSocket("wss://hadlee-giggliest-unpopulously.ngrok-free.dev/");

const textInput = $('textInput');
const sendBtn = $('sendBtn');
const status = $('status');
const startBtn = $('startBtn');
const title = $('title');
const textLabel = $('label');
const reactContainer = $('reactContainer');
const choiceContainer = $('choiceContainer');
const choiceGrid = $('choiceGrid');

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
			sendType = "send_react";
			hideAllInputTypes();
			show(status);
			if(message.myPrompt)
			{
				hide(reactContainer);
				status.textContent = "This is your answer... don't second guess it...";
				break;
			}
			status.textContent = message.text;
			show(reactContainer);
			break;
		case "show_choices":
			sendType = "send_choice";
			hideAllInputTypes();
			hide(reactContainer);
			show(status);
			
			if (message.myPrompt) {
				status.textContent = "This is your answer... don't second guess it...";
				break;
			}
			
			status.textContent = "Choose an answer carefully!";
			// parse choices and create buttons
			const choices = message.text.split('|');
			choiceGrid.innerHTML = ''; // clear previous choices
			
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
			break;
	}
};

sendBtn.addEventListener('click', () => {
	const name = textInput.value.trim();
	if (!name) return;

	sendJSON(ws, { type: sendType, text: name });
    show(status);
	
	status.textContent = 'Thanks!';
    textInput.value = '';  
	hide(textInput);
	hide(textLabel);
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

// idk why this doesnt work but whatever
// function submitText(string)
// {
// 	sendJSON(ws, { type: sendType, text: string });
//     show(status);
// 	status.textContent = 'Thanks!';
// }
