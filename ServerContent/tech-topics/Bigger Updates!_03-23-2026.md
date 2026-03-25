# March 23, 2026

*at \~8 PM, PST*



<br>



This past week, I have once again been ripping my hair out trying to find an even better way to handle Full/Partial HTML responses on the backend. The good news is, now that I have ripped out the requisite amount of hair, konym.live is basically a single-page web app! Of course, most of the improvements are to the server code, so beyond the obvious additional update I made to the Home page layout, things will still look about the same.



Diving deeper, the website's current architecture uses HTMX, ASP.NET Minimal APIs, and Razor Components (what is apparently known as the "MARCH" stack). Basically, every time you request a new page by clicking a button, the server will send down only the new content and HTMX will swap it in on the client side, instead of having to load an entire rendered webpage.



This now means that navigating through the site normally does not incur page refreshes and should be even friendlier to those with slower internet connections.

