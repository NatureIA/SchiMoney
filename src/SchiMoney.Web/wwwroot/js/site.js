(() => {
    const menuButton = document.querySelector("[data-menu]");
    const sidebar = document.getElementById("sidebar");
    if (menuButton && sidebar) {
        menuButton.addEventListener("click", () => sidebar.classList.toggle("open"));
    }

    const recurrence = document.querySelector("[data-recurrence]");
    const installments = document.querySelector("[data-installments]");
    const refreshInstallments = () => {
        if (!recurrence || !installments) return;
        installments.hidden = recurrence.value !== "Parcelado";
    };
    recurrence?.addEventListener("change", refreshInstallments);
    refreshInstallments();

    const service = document.querySelector("[data-service]");
    const amount = document.querySelector("[data-sale-amount]");
    service?.addEventListener("change", () => {
        const selected = service.options[service.selectedIndex];
        const price = selected?.dataset?.price;
        if (price && amount) amount.value = Number(price).toFixed(2);
    });
})();
