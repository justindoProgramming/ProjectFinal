// ======================= MODAL LOADERS =======================

function loadAccountCreate() {
    $("#accCreateContent").load("/ManageAccounts/CreateAccount", function (response, status, xhr) {
        if (status === "error") {
            accShowError("Error loading create form", xhr);
        } else {
            $("#accCreateModal").modal("show");
        }
    });
}

function loadAccountView(id) {
    $("#accViewContent").load("/ManageAccounts/ViewAccount/" + id, function (response, status, xhr) {
        if (status === "error") {
            accShowError("Error loading user details", xhr);
        } else {
            $("#accViewModal").modal("show");
        }
    });
}

function loadAccountEdit(id) {
    $("#accEditContent").load("/ManageAccounts/EditAccount/" + id, function (response, status, xhr) {
        if (status === "error") {
            accShowError("Error loading edit form", xhr);
        } else {
            $("#accEditModal").modal("show");
        }
    });
}

function loadAccountDelete(id) {
    $("#accDeleteContent").load("/ManageAccounts/DeleteAccount/" + id, function (response, status, xhr) {
        if (status === "error") {
            accShowError("Error loading delete confirmation", xhr);
        } else {
            $("#accDeleteModal").modal("show");
        }
    });
}

// ======================= FORM SUBMITS =======================

// CREATE
$(document).on("submit", "#createAccountForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#accCreateModal").modal("hide");
            accShowSuccess(res && res.message ? res.message : "User created successfully.");
            location.reload(); // refresh cards + Active Users count
        },
        error: function (xhr) {
            accShowError(xhr.responseText || "Error creating user.", xhr);
        }
    });
});

// EDIT
$(document).on("submit", "#editAccountForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#accEditModal").modal("hide");
            accShowSuccess(res && res.message ? res.message : "User updated successfully.");
            location.reload();
        },
        error: function (xhr) {
            accShowError(xhr.responseText || "Error updating user.", xhr);
        }
    });
});

// DELETE
$(document).on("submit", "#deleteAccountForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#accDeleteModal").modal("hide");
            accShowSuccess(res && res.message ? res.message : "User deleted.");
            location.reload();
        },
        error: function (xhr) {
            accShowError(xhr.responseText || "Error deleting user.", xhr);
        }
    });
});

// ======================= SEARCH FILTER =======================

function filterAccountCards(term) {
    term = (term || "").toLowerCase();
    $("#accountsContainer .account-card-wrapper").each(function () {
        var text = $(this).text().toLowerCase();
        $(this).toggle(text.indexOf(term) !== -1);
    });
}

// ======================= TOAST HELPERS =======================

function accShowSuccess(message) {
    if (typeof showAppToast === "function") {
        showAppToast(message, "success");
    } else if (typeof showBpToast === "function") {
        showBpToast(message, "success");
    } else if (typeof showSuccessToast === "function") {
        showSuccessToast(message);
    } else {
        alert(message);
    }
}

function accShowError(message, xhr) {
    if (!message && xhr) {
        message = xhr.responseText || xhr.statusText || "Error.";
    }

    if (typeof showAppToast === "function") {
        showAppToast(message, "error");
    } else if (typeof showBpToast === "function") {
        showBpToast(message, "error");
    } else if (typeof showSuccessToast === "function") {
        showSuccessToast(message);
    } else {
        alert(message);
    }
}
