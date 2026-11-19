let offsetX, offsetY;

const elements = document.querySelectorAll("[draggable]")
const elements_nodrag = document.querySelectorAll(".drag-disable");

elements.forEach(element => {
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
        event.target.style.cursor = 'auto';
        event.target.style.opacity = '1.0';

        event.target.style.left = `${event.clientX - offsetX}px`;
        event.target.style.top = `${event.clientY - offsetY}px`;
    });

    element.addEventListener('dblclick', (event) => {
        if (element.classList.contains('nodblclick'))
            return;

        element.style.position = "";
        element.style.left = "";
        element.style.top = "";
    });
});

elements_nodrag.forEach(element => {
    element.draggable = true;

    element.ondragstart = event => {
        event.preventDefault();
        event.stopPropagation();
    }

    element.ondragend = event => {
        event.preventDefault();
        event.stopPropagation();
    }
});