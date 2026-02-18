const grid = document.getElementById("card-grid");
const newInstanceBtn = document.getElementById("new-instance");
const cardTemplate = document.getElementById("pwsh-card-template");

class PwshCard {
    constructor(requestId, name) {
        this.requestId = requestId;
        this.element = cardTemplate.content.firstElementChild.cloneNode(true);
        this.title = this.element.querySelector(".card-title");
        this.statusBadge = this.element.querySelector(".status-badge");
        this.output = this.element.querySelector(".output");
        this.commandInput = this.element.querySelector(".command-input");
        this.sendBtn = this.element.querySelector(".send-btn");
        this.stopBtn = this.element.querySelector(".stop-btn");
        this.killBtn = this.element.querySelector(".kill-btn");

        this.title.textContent = name;
        this.bindEvents();
    }

    bindEvents() {
        this.sendBtn.addEventListener("click", () => this.sendCommand());
        this.commandInput.addEventListener("keydown", (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                this.sendCommand();
            }
        });
        this.stopBtn.addEventListener("click", () => this.stop());
        this.killBtn.addEventListener("click", () => this.kill());
        this.element.addEventListener("dragstart", (event) => {
            event.dataTransfer.setData("text/plain", this.requestId);
            this.element.classList.add("dragging");
        });
        this.element.addEventListener("dragend", () => {
            this.element.classList.remove("dragging");
        });
    }

    sendCommand() {
        const command = this.commandInput.value.trim();
        if (!command) {
            return;
        }
        webWrap.sendCommand(this.requestId, command);
        this.appendOutput(`\n_ >> ${command}`);
        this.commandInput.value = "";
    }

    stop() {
        webWrap.stopCommand(this.requestId);
        this.appendOutput("\n[Command interrupted]");
    }

    kill() {
        webWrap.killPwsh(this.requestId);
        this.setStatus("stopped");
    }

    appendOutput(text) {
        if (!text) {
            return;
        }
        this.output.textContent += `${text}`;
        // Get the parent container that has overflow
        const container = this.output.parentElement;
        // Use a small timeout to ensure DOM has updated
        setTimeout(() => {
            container.scrollTop = container.scrollHeight;
        }, 0);
    }

    setStatus(status) {
        this.statusBadge.textContent = status;
        this.statusBadge.classList.toggle("stopped", status !== "running");
    }
}

class PwshCardManager {
    constructor() {
        this.cards = new Map();
        this.setupEventListeners();
    }

    setupEventListeners() {
        newInstanceBtn.addEventListener("click", () => {
            const requestId = webWrap.createPwsh(`Pwsh-${this.cards.size + 1}`, true);
            this.createCard(requestId, `Pwsh-${this.cards.size + 1}`);
        });

        grid.addEventListener("dragover", (event) => {
            event.preventDefault();
            const afterElement = this.getDragAfterElement(event.clientY);
            const dragging = document.querySelector(".pwsh-card.dragging");
            if (!dragging) return;
            if (afterElement == null) {
                grid.appendChild(dragging);
            } else {
                grid.insertBefore(dragging, afterElement);
            }
        });
    }

    getDragAfterElement(y) {
        const draggableElements = [...grid.querySelectorAll(".pwsh-card:not(.dragging)")];
        return draggableElements.reduce((closest, child) => {
            const box = child.getBoundingClientRect();
            const offset = y - box.top - box.height / 2;
            if (offset < 0 && offset > closest.offset) {
                return { offset, element: child };
            }
            return closest;
        }, { offset: Number.NEGATIVE_INFINITY }).element;
    }

    createCard(requestId, name) {
        if (this.cards.has(requestId)) {
            return;
        }
        const card = new PwshCard(requestId, name);
        this.cards.set(requestId, card);
        grid.appendChild(card.element);
    }

    updateCardOutput(requestId, output, isRunning = true) {
        const card = this.cards.get(requestId);
        if (!card) {
            return;
        }
        card.appendOutput(output);
        card.setStatus(isRunning ? "running" : "stopped");
    }


    removeCard(requestId) {
        const card = this.cards.get(requestId);
        if (!card) {
            return;
        }
        card.element.remove();
        this.cards.delete(requestId);
    }
}

const manager = new PwshCardManager();

webWrap.onMessage("pwshAsyncOutput", (message) => {
    manager.updateCardOutput(message.requestId, message.output, message.isRunning);
});

webWrap.onMessage("pwshResult", (message) => {
    if (message.status !== 0) {
        manager.updateCardOutput(message.requestId, `\n${message.output}`, false);
        return;
    }
    if (message.output) {
        manager.updateCardOutput(message.requestId, `\n${message.output}`, message.isRunning ?? true);
    }
    if (message.isRunning === false) {
        manager.removeCard(message.requestId);
    }
});

webWrap.onMessage("error", (message) => {
    console.error(message.message);
});
