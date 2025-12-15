// Helper: show Bootstrap 5 modal
function owShowModal(id) {
    var el = document.getElementById(id);
    if (!el || !window.bootstrap) return;
    var modal = bootstrap.Modal.getOrCreateInstance(el);
    modal.show();
}

function owHideModal(id) {
    var el = document.getElementById(id);
    if (!el || !window.bootstrap) return;
    var modal = bootstrap.Modal.getInstance(el);
    if (modal) modal.hide();
}

// =============== MODAL LOADERS ===============

function loadOwnerCreate() {
    $("#ownerCreateContent").load("/Owners/CreateOwner", function (response, status, xhr) {
        if (status === "error") {
            owShowError("Error loading create form", xhr);
        } else {
            owShowModal("ownerCreateModal");
        }
    });
}

function loadOwnerView(id) {
    $("#ownerViewContent").load("/Owners/ViewOwner/" + id, function (response, status, xhr) {
        if (status === "error") {
            owShowError("Error loading owner details", xhr);
        } else {
            owShowModal("ownerViewModal");
        }
    });
}

function loadOwnerEdit(id) {
    $("#ownerEditContent").load("/Owners/EditOwner/" + id, function (response, status, xhr) {
        if (status === "error") {
            owShowError("Error loading edit form", xhr);
        } else {
            owShowModal("ownerEditModal");
        }
    });
}

function loadOwnerDelete(id) {
    $("#ownerDeleteContent").load("/Owners/DeleteOwner/" + id, function (response, status, xhr) {
        if (status === "error") {
            owShowError("Error loading delete confirmation", xhr);
        } else {
            owShowModal("ownerDeleteModal");
        }
    });
}

// =============== AJAX FORMS ===============

// CREATE
$(document).on("submit", "#createOwnerForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            owHideModal("ownerCreateModal");
            owShowSuccess(res && res.message ? res.message : "Owner created.");
            location.reload();
        },
        error: function (xhr) {
            owShowError(xhr.responseText || "Error creating owner.", xhr);
        }
    });
});

// EDIT
$(document).on("submit", "#editOwnerForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            owHideModal("ownerEditModal");
            owShowSuccess(res && res.message ? res.message : "Owner updated.");
            location.reload();
        },
        error: function (xhr) {
            owShowError(xhr.responseText || "Error updating owner.", xhr);
        }
    });
});

// DELETE
$(document).on("submit", "#deleteOwnerForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            owHideModal("ownerDeleteModal");
            owShowSuccess(res && res.message ? res.message : "Owner deleted.");
            location.reload();
        },
        error: function (xhr) {
            owShowError(xhr.responseText || "Error deleting owner.", xhr);
        }
    });
});

// =============== SEARCH FILTER ===============

$("#ownerSearchBox").on("input", function () {
    var term = ($(this).val() || "").toLowerCase();
    $("#ownersContainer .owner-card-wrapper").each(function () {
        var text = $(this).text().toLowerCase();
        $(this).toggle(text.indexOf(term) !== -1);
    });
});

// =============== TOAST HELPERS ===============

function owShowSuccess(message) {
    if (typeof showAppToast === "function") {
        showAppToast(message, "success");
    } else if (typeof showBpToast === "function") {
        showBpToast(message, "success");
    } else {
        alert(message);
    }
}

function owShowError(message, xhr) {
    if (!message && xhr) {
        message = xhr.responseText || xhr.statusText || "Error.";
    }
    if (typeof showAppToast === "function") {
        showAppToast(message, "error");
    } else if (typeof showBpToast === "function") {
        showBpToast(message, "error");
    } else {
        alert(message);
    }
}
