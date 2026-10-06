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
    const newServiceBlock = document.querySelector("[data-new-service-block]");
    const newServiceName = document.querySelector("[data-new-service-name]");

    const refreshService = (clearAmount = false) => {
        if (!service) return;

        const selected = service.options[service.selectedIndex];
        const isNew = selected?.value === "__new__";
        const price = selected?.dataset?.price;

        if (newServiceBlock) {
            newServiceBlock.hidden = !isNew;
        }

        if (newServiceName) {
            newServiceName.required = isNew;
            if (!isNew) {
                newServiceName.value = "";
            }
        }

        if (amount && clearAmount) {
            if (price) {
                amount.value = Number(price).toFixed(2);
            } else if (isNew || selected?.value === "Pomada") {
                amount.value = "";
            }
        }
    };

    service?.addEventListener("change", () => refreshService(true));
    refreshService(false);

    const expenseCategory = document.querySelector("[data-expense-category]");
    const newExpenseCategoryBlock = document.querySelector("[data-new-expense-category-block]");
    const newExpenseCategoryName = document.querySelector("[data-new-expense-category-name]");

    const refreshExpenseCategory = () => {
        if (!expenseCategory) return;

        const isNew = expenseCategory.value === "__new__";

        if (newExpenseCategoryBlock) {
            newExpenseCategoryBlock.hidden = !isNew;
        }

        if (newExpenseCategoryName) {
            newExpenseCategoryName.required = isNew;

            if (!isNew) {
                newExpenseCategoryName.value = "";
            }
        }
    };

    expenseCategory?.addEventListener("change", refreshExpenseCategory);
    refreshExpenseCategory();

    const expensePayment = document.querySelector("[data-expense-payment]");
    const expenseInstallments = document.querySelector("[data-expense-installments]");
    const expenseInstallmentCount = document.querySelector("[data-expense-installment-count]");
    const expenseInstallmentLabel = document.querySelector("[data-expense-installment-label]");
    const expenseInstallmentHelp = document.querySelector("[data-expense-installment-help]");
    const expenseBoletoDateBlock = document.querySelector("[data-expense-boleto-date]");
    const firstBoletoDate = document.querySelector("[data-first-boleto-date]");
    const expensePaidBlock = document.querySelector("[data-expense-paid-block]");
    const expensePaymentMessage = document.querySelector("[data-expense-payment-message]");

    const refreshExpensePayment = () => {
        if (!expensePayment) return;

        const method = expensePayment.value;
        const isCredit = method === "Crédito";
        const isBoleto = method === "Boleto";
        const isInstallment = isCredit || isBoleto;

        if (expenseInstallments) {
            expenseInstallments.hidden = !isInstallment;
        }

        if (expenseInstallmentCount) {
            expenseInstallmentCount.required = isInstallment;
            if (!isInstallment) {
                expenseInstallmentCount.value = "1";
            }
        }

        if (expenseInstallmentLabel) {
            expenseInstallmentLabel.textContent = isBoleto
                ? "Quantidade de boletos"
                : "Quantidade de parcelas";
        }

        if (expenseInstallmentHelp) {
            expenseInstallmentHelp.textContent = isBoleto
                ? "O valor total será dividido entre os boletos."
                : "O valor total será dividido entre as parcelas.";
        }

        if (expenseBoletoDateBlock) {
            expenseBoletoDateBlock.hidden = !isBoleto;
        }

        if (firstBoletoDate) {
            firstBoletoDate.required = isBoleto;
            if (!isBoleto) {
                firstBoletoDate.value = "";
            }
        }

        if (expensePaidBlock) {
            expensePaidBlock.hidden = isInstallment;
        }

        if (expensePaymentMessage) {
            if (isCredit) {
                expensePaymentMessage.textContent =
                    "A primeira parcela entra automaticamente no próximo mês; as demais seguem mês a mês.";
            } else if (isBoleto) {
                expensePaymentMessage.textContent =
                    "O primeiro boleto usa a data informada e os demais são projetados mensalmente.";
            } else {
                expensePaymentMessage.textContent =
                    "Este lançamento será registrado automaticamente na data atual.";
            }
        }
    };

    expensePayment?.addEventListener("change", refreshExpensePayment);
    refreshExpensePayment();
})();
