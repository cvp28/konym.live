
const root = document.getElementById("root");
const menu = document.getElementById("context-menu");
const player = document.getElementById("media-player");
const chat = document.getElementById("chat-badge");

var ContextX;
var ContextY;

root.addEventListener("contextmenu", function (event) {
    event.preventDefault();

    ContextX = event.pageX;
    ContextY = event.pageY;

    menu.style.display = "flex";
    menu.style.left = ContextX + "px";
    menu.style.top = ContextY + "px";

    document.addEventListener("click", HideMenuHandler);
});

function HideMenuHandler() {
    menu.style.display = "none";
    document.removeEventListener("click", HideMenuHandler)
}

function IsVisible(element) {
    return element.style.display === "flex";
}

function Toggle(element) {
    element.style.display = IsVisible(element) ? "none" : "flex";
    element.style.left = ContextX + "px";
    element.style.top = ContextY + "px";
}