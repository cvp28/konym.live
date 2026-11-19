const audio = document.getElementById("audio-src");

const name_marquee = document.getElementById("media-name");
const list_modal = document.getElementById("mp-list-modal");

// Just a list of paths with filenames for songs the user wants to hear in sequence.
let playlist = [];
let refresh_modal = true;
let folder;
let filename;

ClearDiscordActivity();
InitMediaSession();

audio.volume = document.getElementById("volume-slider").value;

document.getElementById("media-player").addEventListener("click", OnMp3Input);
document.getElementById("media-player").addEventListener("input", OnMp3Input);

function OnMp3Input(event) {
	switch (event.target.dataset?.action) {
		case "play":
			PlayPause();
			break;

		case "random":
			PlayRandomMp3();
			break;

		case "list":
			ShowList();
			break;

		case "volume":
			audio.volume = document.getElementById("volume-slider").value;
			break;

		case "seek":
			audio.currentTime = document.getElementById("seek-slider").value;

			if (!audio.paused)
				SetDiscordActivity();
			break;
	}
}

function PlayPause() {
	if (audio.paused) {
		audio.play();
		SetDiscordActivity();
	}
	else {
		audio.pause();
		ClearDiscordActivity();
	}
}

function ShowList() {
	list_modal.showModal();

	if (refresh_modal) {
		list_modal.dispatchEvent(new Event("mp-list-show-event"))
		refresh_modal = false;
	}
}

audio.onloadedmetadata = function () {
	document.getElementById("seek-slider").max = audio.duration;
};

// update seek bar
audio.ontimeupdate = function () {
	document.getElementById("seek-slider").value = audio.currentTime;
};

audio.onended = function () {
	ClearDiscordActivity();

	document.getElementById("seek-slider").value = 0;
	audio.currentTime = 0;
};

list_modal.onclick = function (e) {
	const dialogDimensions = list_modal.getBoundingClientRect();

	if (
		e.clientX < dialogDimensions.left ||
		e.clientX > dialogDimensions.right ||
		e.clientY < dialogDimensions.top ||
		e.clientY > dialogDimensions.bottom
	) {
		list_modal.close();
	}
}

async function PlayRandomMp3() {
	const response = await fetch("/frag/random-song");

	if (!response.ok)
		throw new Error("Failed to fetch random song via /frag/random-song");

	const path = await response.text();
	ResetAndPlayMp3(path);
}

function UpdatePlaylistSelection(event) {
	const elem = event.target;

	const data_type = elem.dataset.type;

	if (data_type === "folder") {
		let cbs = elem.parentElement.parentElement.querySelectorAll("#mp3-pl-checkbox");

		for (let cb of cbs)
			cb.checked = elem.checked;
	}
	else if (data_type === "file") {
		let cbs = elem.parentElement.parentElement.querySelectorAll("#mp3-pl-checkbox");

		let check = true;

		for (let cb of cbs)
			check &= cb.checked;

		elem.parentElement.parentElement.parentElement.querySelector("#mp3-pl-checkbox").checked = check;
	}
}

function ResetAndPlayMp3(path) {
	// adding date.now to force the browser to always fetch the file from the origin server
	audio.src = `${path}?t=${Date.now()}`;
	audio.load();
	audio.play();

	let elems = path.split("\\");

	filename = elems.pop();
	folder = elems.join('\\');

	SetMediaMetadata(filename);

	name_marquee.innerHTML = filename;
	name_marquee.style.animation = "none";
	name_marquee.offsetHeight;
	name_marquee.style.animation = "marquee 15s linear infinite";

	SetDiscordActivity();
}

function InitMediaSession() {
	if (!("mediaSession" in navigator)) return;

	navigator.mediaSession.setActionHandler('play', () => {
		audio.play();
	});

	navigator.mediaSession.setActionHandler('pause', () => {
		audio.pause();
	});

	navigator.mediaSession.setActionHandler('seekbackward', (details) => {
		// Fall back to a default value if no specific offset time is provided
		const offset = details.seekOffset || 10;
		audio.currentTime = Math.max(audio.currentTime - offset, 0);
	});

	navigator.mediaSession.setActionHandler('seekforward', (details) => {
		const offset = details.seekOffset || 10;
		audio.currentTime = Math.min(audio.currentTime + offset, audio.duration);
	});

	navigator.mediaSession.setActionHandler("previoustrack", null);
	navigator.mediaSession.setActionHandler("nexttrack", PlayRandomMp3);

	audio.addEventListener('play', () => {
		navigator.mediaSession.playbackState = 'playing';
	});

	audio.addEventListener('pause', () => {
		navigator.mediaSession.playbackState = 'paused';
	});

	audio.addEventListener('timeupdate', UpdatePositionState);
	audio.addEventListener('ratechange', UpdatePositionState);
	audio.addEventListener('durationchange', UpdatePositionState);
}

function SetDiscordActivity() {
	const elapsedTimeMs = Math.ceil(audio.currentTime * 1000);

	window.postMessage({
		type: "SET_DISCORD_RPC",
		payload: {
			application_id: "1570619339789574244",
			name: "konym.live",
			type: 2,
			details: `▶️ ${filename}`,
			state: `📂 ${folder}`,
			timestamps: {
				start: Date.now() - elapsedTimeMs
			}
		}
	}, "*");
}

function ClearDiscordActivity() {
	window.postMessage({
		type: "SET_DISCORD_RPC",
		payload: null
	}, "*");
}

function UpdatePositionState() {
	if ('setPositionState' in navigator.mediaSession) {
		navigator.mediaSession.setPositionState({
			duration: audio.duration || 0,
			playbackRate: audio.playbackRate,
			position: audio.currentTime
		});
	}
}

function SetMediaMetadata(filename) {
	navigator.mediaSession.metadata = new MediaMetadata({
		title: filename
	});
}