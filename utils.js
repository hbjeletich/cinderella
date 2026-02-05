export function $(id) {
	return document.getElementById(id);
}

export function show(el) {
	el.classList.remove('hidden');
}

export function hide(el) {
	el.classList.add('hidden');
}

export function sendJSON(ws, data) {
	ws.send(JSON.stringify(data));
}

export function getInputType(string){
    switch(string)
    {
        case("text"):
            return $("textInput");
    }
    return null;
}