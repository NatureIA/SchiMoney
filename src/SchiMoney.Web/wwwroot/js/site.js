(() => {
    const menuButton = document.querySelector("[data-menu]");
    const sidebar = document.getElementById("sidebar");
    if (menuButton && sidebar) {
        menuButton.addEventListener("click", () => sidebar.classList.toggle("open"));
        document.addEventListener("click", (event) => {
            if (window.innerWidth <= 760 && sidebar.classList.contains("open") &&
                !sidebar.contains(event.target) && event.target !== menuButton) {
                sidebar.classList.remove("open");
            }
        });
    }

    const recurrence = document.querySelector("[data-recurrence]");
    const installments = document.querySelector("[data-installments]");
    const installmentLabel = document.querySelector("[data-installment-label]");
    const refreshInstallments = () => {
        if (!recurrence || !installments) return;
        const show = recurrence.value === "Parcelado" || recurrence.value === "Recorrente";
        installments.hidden = !show;
        if (installmentLabel) {
            installmentLabel.textContent = recurrence.value === "Recorrente"
                ? "Quantidade de meses"
                : "Quantidade de parcelas";
        }
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
