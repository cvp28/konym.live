let ws;
let fit;
let webgl;
let canvas;
let attach;
let term;

let Terminal;
let FitAddon;
let WebglAddon;
let CanvasAddon;
let AttachAddon;

async function ConnectToChat() {
	SetTermVisible(true);

	document.getElementById("loading-div").style.display = "";

	document.getElementById("xterm-css").rel = "stylesheet";
	Terminal = await import("/frag/xterm-core");
	FitAddon = await import("/frag/xterm-fit-addon");
	WebglAddon = await import("/frag/xterm-webgl-addon");
	CanvasAddon = await import("/frag/xterm-canvas-addon");
	AttachAddon = await import("/frag/xterm-attach-addon");

	document.getElementById("loading-div").style.display = "none";

	ws = new WebSocket(`wss://${location.host}/chat-ws`);

	ws.onerror = function (event) {
		ws = null;
		SetTermVisible(false);
	}

	ws.onopen = WsOpen;
	ws.onclose = WsClose;
}

function DisconnectFromChat() {
	ws?.close(1000, "Chat session closed");
}

async function WsOpen(event) {

	term = new Terminal.Terminal({
		cols: 120,
		rows: 30
	});

	try {
		webgl = new WebglAddon.WebglAddon();
		term.loadAddon(webgl);
	}
	catch (webgl_ex) {
		console.warn("Failed to load WebGL renderer for xterm.js:", webgl_ex);

		try {
			canvas = new CanvasAddon.CanvasAddon();
			term.loadAddon(canvas);
		}
		catch (canvas_ex) {
			console.warn("Failed to load Canvas renderer for xterm.js:", canvas_ex);
		}
	}

	fit = new FitAddon.FitAddon();
	attach = new AttachAddon.AttachAddon(ws);

	term.loadAddon(fit);
	term.loadAddon(attach);

	term.open(document.getElementById("terminal"));

	// hide the cursor
	term.write('\x1b[?25l');
	term.focus();
}

function WsClose(event) {
	Cleanup();
	SetTermVisible(false);
}

function SetTermVisible(mode) {
	document.getElementById("terminal").style.display = mode ? '' : 'none';
	document.getElementById("connect-div").style.display = mode ? 'none' : '';
}

function Cleanup() {
	fit?.dispose();
	webgl?.dispose();
	attach?.dispose();
	term?.dispose();

	fit = null;
	webgl = null;
	attach = null;
	term = null;

	Terminal = null;
	FitAddon = null;
	WebglAddon = null;
	CanvasAddon = null;
	AttachAddon = null;

	ws = null;
}