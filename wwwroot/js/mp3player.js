const audio = document.getElementById("audio-src");
const play = document.getElementById("play");
const random = document.getElementById("random");
const list = document.getElementById("list");
const seek = document.getElementById("seek");
const volume = document.getElementById("volume");
const name = document.getElementById("media-name");
const list_modal = document.getElementById("mp-list-modal");

InitMediaSession();

audio.volume = volume.value;

// handle play/pause
play.onclick = function () {
	if (audio.paused) {
		audio.play();
	}
	else {
		audio.pause();
	}
};

random.onclick = PlayRandomMp3;

list.onclick = function (event) {
	list_modal.showModal();
	list_modal.dispatchEvent(new Event("mp-list-show-event"))
};

audio.onloadedmetadata = function () {
	seek.max = audio.duration;
};

// update seek bar
audio.ontimeupdate = function () {
	seek.value = audio.currentTime;
};

audio.onended = function () {
	seek.value = 0;
	audio.currentTime = 0;
};

volume.oninput = function () {
	audio.volume = volume.value;
}

seek.oninput = function () {
	audio.currentTime = seek.value;
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

	const filename = await response.text();
	ResetAndPlayMp3(filename);
}

function ResetAndPlayMp3(filename) {
	audio.src = "/mp3/" + filename;
	audio.load();
	audio.play();

	InitMediaMetadata(filename);

	name.innerHTML = filename;
	name.style.animation = "none";
	name.offsetHeight;
	name.style.animation = "marquee 15s linear infinite";
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

function UpdatePositionState() {
	if ('setPositionState' in navigator.mediaSession) {
		navigator.mediaSession.setPositionState({
			duration: audio.duration || 0,
			playbackRate: audio.playbackRate,
			position: audio.currentTime
		});
	}
}

function InitMediaMetadata(filename) {
	navigator.mediaSession.metadata = new MediaMetadata({
		title: filename,
		artist: '^ artist right there btw',
		album: 'Album Name',
		artwork: [
			{ src: 'cover-96x96.png', sizes: '96x96', type: 'image/png' },
			{ src: 'cover-512x512.png', sizes: '512x512', type: 'image/png' }
		]
	});
}