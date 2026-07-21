let offsetX, offsetY;
//const element = document.getElementById('active-users');

const elements = document.querySelectorAll("[draggable]")

document.body.addEventListener("dragover", (event) => {
    event.preventDefault();
});

document.body.addEventListener("dragenter", (event) => {
    event.preventDefault();
});

elements.forEach(element => {
    //element.setAttribute('draggable', 'true');

    element.addEventListener('dragstart', (event) => {
        offsetX = event.clientX - element.getBoundingClientRect().left;
        offsetY = event.clientY - element.getBoundingClientRect().top;

        event.target.style.position = "absolute";
        event.target.style.opacity = '0.4';
        event.target.style.left = `${event.clientX - offsetX}px`;
        event.target.style.top = `${event.clientY - offsetY}px`;

        event.target.style.cursor = 'grab';

        return false;
    });

    element.addEventListener('dragend', (event) => {
        event.target.style.cursor = 'grab';
        event.target.style.opacity = '1.0';

        event.target.style.left = `${event.clientX - offsetX}px`;
        event.target.style.top = `${event.clientY - offsetY}px`;
    });

    element.addEventListener('dblclick', (event) => {
        element.style.position = "";
        element.style.left = "";
        element.style.top = "";
    });
});