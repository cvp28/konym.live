document.body.addEventListener("htmx:after:swap", function () {
	// Get the current page's URL
	const currentUrl = window.location.href;

	// Select all anchor tags within the navbar
	const navLinks = document.querySelectorAll("#navlist a");

	// Loop through the links and add the 'active' class to the matching one
	navLinks.forEach(link => {
		if (link.href === currentUrl)
			link.classList.add('active');
		else
			link.classList.remove('active');

	});

	const clipboard = document.getElementById("button-image");

	clipboard.onclick = async function (event) {
		await navigator.clipboard.writeText(`<a target="_blank" href="https://konym.live" title="To konym.live"><img style="width: 88px; height: 31px" src="https://konym.live/images/button.png" /></a>`);
		alert("Copied button html!");
	};
});