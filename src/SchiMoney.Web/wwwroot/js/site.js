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
    const newServiceBlock = document.querySelector("[data-new-service-block]");
    const newServiceName = document.querySelector("[data-new-service-name]");

    const refreshService = () => {
        if (!service) return;

        const selected = service.options[service.selectedIndex];
        const isNew = selected?.value === "__new__";

        if (newServiceBlock) {
            newServiceBlock.hidden = !isNew;
        }

        if (newServiceName) {
            newServiceName.required = isNew;

            if (!isNew) {
                newServiceName.value = "";
            }
        }
    };

    service?.addEventListener("change", refreshService);
    refreshService();

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
    const expenseRecurring = document.querySelector("[data-expense-recurring]");
    const expenseInstallments = document.querySelector("[data-expense-installments]");
    const expenseInstallmentCount = document.querySelector("[data-expense-installment-count]");
    const expenseInstallmentLabel = document.querySelector("[data-expense-installment-label]");
    const expenseInstallmentHelp = document.querySelector("[data-expense-installment-help]");
    const expenseAmountLabel = document.querySelector("[data-expense-amount-label]");
    const expenseAmountHelp = document.querySelector("[data-expense-amount-help]");
    const expenseBoletoDateBlock = document.querySelector("[data-expense-boleto-date]");
    const expenseBoletoDateLabel = document.querySelector("[data-expense-boleto-date-label]");
    const expenseBoletoDateHelp = document.querySelector("[data-expense-boleto-date-help]");
    const firstBoletoDate = document.querySelector("[data-first-boleto-date]");
    const expensePaidBlock = document.querySelector("[data-expense-paid-block]");
    const expensePaymentMessage = document.querySelector("[data-expense-payment-message]");

    const refreshExpensePayment = () => {
        if (!expensePayment) return;

        const method = expensePayment.value;
        const isRecurring = expenseRecurring?.checked === true;
        const isCredit = method === "Crédito";
        const isBoleto = method === "Boleto";
        const isInstallment = !isRecurring && (isCredit || isBoleto);

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
                ? "Cada boleto terá exatamente o valor informado."
                : "Cada parcela terá exatamente o valor informado.";
        }

        if (expenseAmountLabel) {
            expenseAmountLabel.textContent = isRecurring
                ? "Valor mensal"
                : isCredit
                    ? "Valor da parcela"
                    : isBoleto
                        ? "Valor do boleto"
                        : "Valor";
        }

        if (expenseAmountHelp) {
            expenseAmountHelp.textContent = isRecurring
                ? "Este valor será aplicado a cada ocorrência mensal até a recorrência ser encerrada."
                : isCredit
                    ? "Informe o valor de cada parcela, não o valor total da compra."
                    : isBoleto
                        ? "Informe o valor de cada boleto, não o valor total do parcelamento."
                        : "Informe o valor desta despesa.";
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

        if (expenseBoletoDateLabel) {
            expenseBoletoDateLabel.textContent = isRecurring
                ? "Data do primeiro boleto recorrente"
                : "Data do primeiro boleto";
        }

        if (expenseBoletoDateHelp) {
            expenseBoletoDateHelp.textContent = isRecurring
                ? "Essa data define a primeira ocorrência; as seguintes serão mensais."
                : "Os próximos boletos serão projetados mensalmente a partir desta data.";
        }

        if (expensePaidBlock) {
            expensePaidBlock.hidden = isInstallment || (isRecurring && (isCredit || isBoleto));
        }

        if (expensePaymentMessage) {
            if (isRecurring && isCredit) {
                expensePaymentMessage.textContent =
                    "Despesa recorrente mensal. A primeira ocorrência entra no próximo mês e continua até você encerrar.";
            } else if (isRecurring && isBoleto) {
                expensePaymentMessage.textContent =
                    "Despesa recorrente mensal. O primeiro boleto usa a data informada e os próximos seguem mês a mês até você encerrar.";
            } else if (isRecurring) {
                expensePaymentMessage.textContent =
                    "Despesa recorrente mensal. A primeira ocorrência entra hoje e continuará nos próximos meses até você encerrar.";
            } else if (isCredit) {
                expensePaymentMessage.textContent =
                    "A primeira parcela entra automaticamente no próximo mês; cada parcela terá o valor informado.";
            } else if (isBoleto) {
                expensePaymentMessage.textContent =
                    "O primeiro boleto usa a data informada; os demais seguem mês a mês com o mesmo valor.";
            } else {
                expensePaymentMessage.textContent =
                    "Este lançamento será registrado automaticamente na data atual.";
            }
        }
    };

    expensePayment?.addEventListener("change", refreshExpensePayment);
    expenseRecurring?.addEventListener("change", refreshExpensePayment);
    refreshExpensePayment();
})();


(() => {
    const installButton = document.querySelector("[data-pwa-install]");
    let deferredInstallPrompt = null;

    const isStandalone = () =>
        window.matchMedia("(display-mode: standalone)").matches ||
        window.navigator.standalone === true;

    window.addEventListener("beforeinstallprompt", (event) => {
        event.preventDefault();
        deferredInstallPrompt = event;

        if (installButton && !isStandalone()) {
            installButton.hidden = false;
        }
    });

    installButton?.addEventListener("click", async () => {
        if (!deferredInstallPrompt) return;

        deferredInstallPrompt.prompt();
        await deferredInstallPrompt.userChoice;
        deferredInstallPrompt = null;
        installButton.hidden = true;
    });

    window.addEventListener("appinstalled", () => {
        deferredInstallPrompt = null;
        if (installButton) installButton.hidden = true;
    });

    if ("serviceWorker" in navigator) {
        window.addEventListener("load", async () => {
            try {
                const registration = await navigator.serviceWorker.register(
                    "/service-worker.js",
                    {
                        scope: "/",
                        updateViaCache: "none"
                    }
                );

                await registration.update();
            } catch {
                // O SchiMoney continua funcionando normalmente mesmo sem suporte a PWA.
            }
        });
    }
})();
