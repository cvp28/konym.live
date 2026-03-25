# March 14, 2026



I've finally gathered the motivation to resume working on this site after a couple days of inactivity. The site has also been down for much of that time, my apologies.



The zones are laid out a little better now, with that sidebar gone and the "Go Back" buttons renamed to be a little more descriptive. On the backend, clicking on a topic while already on the zones page pulls down an HTML fragment from the server to swap into the DOM inline. This has the benefit of not requiring a page refresh, which looks better and is friendlier to lower-bandwidth connections.



However, this created a new problem when I thought about visiting articles directly from a permanent link - which requires the server to be smart enough to determine when to send down a fragment, and when to send the entire rendered webpage. Suffice to say that, after a few days of ***slowly*** ripping my hair out, the server **is** now officially smart enough to do that. My only real gripe with it is that it requires a whoooole bunch of extra logic. At the very least, I now have it organized well enough that looking at the code no longer makes me want to kill myself (/j).

