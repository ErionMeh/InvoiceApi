
const users = [
    { id: 1, name: "Finance Admin", role: "Finance" },
    { id: 2, name: "Postman One", role: "Postman" },
    { id: 3, name: "Client One", role: "Client" },
    { id: 4, name: "Client Two", role: "Client" }
];
let currentUser = users[0];

const userSelect = document.getElementById("userSelect");
const clientSelect = document.getElementById("clientSelect");
const pendingOnly = document.getElementById("pendingOnly");

async function api(url, method = "GET", body = null) {
    const options = { method: method, headers: {} };
    if (body) {
        options.headers["Content-Type"] = "application/json";
        options.body = JSON.stringify(body);
    }
    const response = await fetch(url, options);
    const text = await response.text();

    let data = null;
    try { data = text ? JSON.parse(text) : null; } catch { }

    if (!response.ok) {
        const message = (data && (data.message || data.title)) || "Request failed (" + response.status + ")";
        throw new Error(message);
    }
    return data;
}

function showMessage(text, isError) {
    const message = document.getElementById("message");
    message.textContent = text;
    message.className = isError ? "error" : "success";
}

function fillSelects() {
    for (const u of users) {
        const option = document.createElement("option");
        option.value = u.id;
        option.textContent = u.name + " (" + u.role + ")";
        userSelect.appendChild(option);
    }
    for (const u of users.filter(u => u.role === "Client")) {
        const option = document.createElement("option");
        option.value = u.id;
        option.textContent = u.name;
        clientSelect.appendChild(option);
    }
}

function applyRole() {
    document.getElementById("createSection").hidden = currentUser.role !== "Finance";
    document.getElementById("pendingLabel").hidden = currentUser.role !== "Client";
    showMessage("", false);
    loadInvoices();
}

async function loadInvoices() {
    try {
        let url;
        if (currentUser.role === "Finance") {
            url = "/api/invoices?userId=" + currentUser.id;
        } else if (currentUser.role === "Postman") {
            url = "/api/postmen/" + currentUser.id + "/invoices";
        } else {
            url = "/api/clients/" + currentUser.id + "/invoices?pending=" + pendingOnly.checked;
        }
        renderInvoices(await api(url));
    } catch (err) {
        renderInvoices([]);
        showMessage(err.message, true);
    }
}

function renderInvoices(invoices) {
    const tbody = document.querySelector("#invoicesTable tbody");
    tbody.innerHTML = "";

    for (const inv of invoices) {
        const row = tbody.insertRow();
        row.insertCell().textContent = inv.invoiceNumber;
        row.insertCell().textContent = inv.clientName;
        row.insertCell().textContent = inv.deliveryAddress;
        row.insertCell().textContent = inv.postmanName ?? "-";
        row.insertCell().textContent = inv.totalAmount.toFixed(2);
        row.insertCell().textContent = inv.paidAmount.toFixed(2);
        row.insertCell().textContent = inv.remainingAmount.toFixed(2);
        row.insertCell().textContent = inv.status;
        addActions(row.insertCell(), inv);
    }
}

function makeButton(text, onClick) {
    const button = document.createElement("button");
    button.textContent = text;
    button.addEventListener("click", onClick);
    return button;
}

function addActions(cell, inv) {
    const pdf = document.createElement("a");
    pdf.href = "/api/invoices/" + inv.id + "/pdf?userId=" + currentUser.id;
    pdf.target = "_blank";
    pdf.textContent = "PDF";
    cell.appendChild(pdf);

    if (currentUser.role === "Finance" && inv.status === "Created") {
        const select = document.createElement("select");
        for (const u of users.filter(u => u.role === "Postman")) {
            const option = document.createElement("option");
            option.value = u.id;
            option.textContent = u.name;
            select.appendChild(option);
        }
        cell.append(" ", select, makeButton("Assign", () => assign(inv.id, Number(select.value))));
    }

    if (currentUser.role === "Postman" && inv.status === "Assigned") {
        cell.append(" ", makeButton("Mark delivered", () => deliver(inv.id)));
    }

    if (currentUser.role === "Client" && (inv.status === "Delivered" || inv.status === "PartiallyPaid")) {
        const input = document.createElement("input");
        input.type = "number";
        input.min = "0.01";
        input.step = "0.01";
        input.value = inv.remainingAmount;
        cell.append(" ", input, makeButton("Pay", () => pay(inv.id, Number(input.value))));
    }
}

async function runAction(action, successText) {
    try {
        await action();
        showMessage(successText, false);
        await loadInvoices();
    } catch (err) {
        showMessage(err.message, true);
    }
}

function assign(id, postmanId) {
    return runAction(
        () => api("/api/invoices/" + id + "/assign?userId=" + currentUser.id, "PUT", { postmanId: postmanId }),
        "Invoice assigned.");
}

function deliver(id) {
    return runAction(
        () => api("/api/invoices/" + id + "/deliver?userId=" + currentUser.id, "PUT"),
        "Marked as delivered.");
}

function pay(id, amount) {
    return runAction(
        () => api("/api/invoices/" + id + "/payments?userId=" + currentUser.id, "POST", { amount: amount }),
        "Payment recorded.");
}

function createInvoice() {
    return runAction(
        () => api("/api/invoices?userId=" + currentUser.id, "POST", {
            clientId: Number(clientSelect.value),
            description: document.getElementById("descriptionInput").value,
            totalAmount: Number(document.getElementById("totalInput").value)
        }),
        "Invoice created.");
}

userSelect.addEventListener("change", () => {
    currentUser = users.find(u => u.id === Number(userSelect.value));
    applyRole();
});
pendingOnly.addEventListener("change", loadInvoices);
document.getElementById("createBtn").addEventListener("click", createInvoice);

fillSelects();
applyRole();