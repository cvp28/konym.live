const audio = document.getElementById("audio-src");
const play = document.getElementById("play");
const random = document.getElementById("random");
const list = document.getElementById("list");
const seek = document.getElementById("seek");
const volume = document.getElementById("volume");
const name = document.getElementById("media-name");
const list_modal = document.getElementById("mp-list-modal");

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

random.onclick = async function (event) {
	const response = await fetch("/frag/random-song");

	if (!response.ok)
		throw new Error("Failed to fetch random song via /frag/random-song");

	const filename = await response.text();
	ResetAndPlayMp3(filename);
};

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

function ResetAndPlayMp3(filename) {
	audio.src = "/mp3/" + filename;
	audio.load();
	audio.play();

	name.innerHTML = filename;
	name.style.animation = "none";
	name.offsetHeight;
	name.style.animation = "marquee 15s linear infinite";
}