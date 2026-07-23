const audio = document.getElementById("audio-src");
const play = document.getElementById("play");
const random = document.getElementById("random");
const seek = document.getElementById("seek");
const volume = document.getElementById("volume");
const name = document.getElementById("media-name");

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

	audio.src = "/mp3/" + filename;
	audio.load();
	audio.play();

	name.innerHTML = filename;
	name.style.animation = "none";
	name.offsetHeight;
	name.style.animation = "marquee 15s linear infinite";
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
	play.textContent = "▶";
};

volume.oninput = function () {
	audio.volume = volume.value;
}

seek.oninput = function () {
	audio.currentTime = seek.value;
};