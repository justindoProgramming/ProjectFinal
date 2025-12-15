// =============== DRUGS INVENTORY JS =================

// Reload the inventory list into the Inventory tab
function loadDrugs() {
    $("#inventoryContainer").load("/BillingPharmacy/DrugList");
}

// ---------- OPEN MODALS (same pattern as prescriptions) ----------

function loadDrugCreate() {
    $("#drugCreateContent").html("Loading...");
    $("#drugCreateContent").load("/BillingPharmacy/CreateDrug", function () {
        new bootstrap.Modal(document.getElementById("drugCreateModal")).show();
    });
}

function loadDrugEdit(id) {
    $("#drugEditContent").html("Loading...");
    $("#drugEditContent").load("/BillingPharmacy/EditDrug/" + id, function () {
        new bootstrap.Modal(document.getElementById("drugEditModal")).show();
    });
}

function loadDrugDelete(id) {
    $("#drugDeleteContent").html("Loading...");
    $("#drugDeleteContent").load("/BillingPharmacy/DeleteDrug/" + id, function () {
        new bootstrap.Modal(document.getElementById("drugDeleteModal")).show();
    });
}

// ---------- FORM SUBMIT (AJAX) ----------

$(document).on("submit", "#createDrugForm", function (e) {
    e.preventDefault();

    const $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize()
    }).done(function (res) {
        if (window.showBpToast) {
            showBpToast((res && res.message) || "Drug created successfully.", "success");
        }
        const modal = bootstrap.Modal.getInstance(document.getElementById("drugCreateModal"));
        if (modal) modal.hide();
        loadDrugs();
    }).fail(function (xhr) {
        const msg = xhr.responseText || "Error creating drug.";
        if (window.showBpToast) {
            showBpToast(msg, "error");
        } else {
            alert(msg);
        }
    });
});

$(document).on("submit", "#editDrugForm", function (e) {
    e.preventDefault();

    const $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize()
    }).done(function (res) {
        if (window.showBpToast) {
            showBpToast((res && res.message) || "Drug updated successfully.", "success");
        }
        const modal = bootstrap.Modal.getInstance(document.getElementById("drugEditModal"));
        if (modal) modal.hide();
        loadDrugs();
    }).fail(function (xhr) {
        const msg = xhr.responseText || "Error updating drug.";
        if (window.showBpToast) {
            showBpToast(msg, "error");
        } else {
            alert(msg);
        }
    });
});

$(document).on("submit", "#deleteDrugForm", function (e) {
    e.preventDefault();

    const $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize()
    }).done(function (res) {
        if (window.showBpToast) {
            showBpToast((res && res.message) || "Drug deleted successfully.", "success");
        }
        const modal = bootstrap.Modal.getInstance(document.getElementById("drugDeleteModal"));
        if (modal) modal.hide();
        loadDrugs();
    }).fail(function (xhr) {
        const msg = xhr.responseText || "Error deleting drug.";
        if (window.showBpToast) {
            showBpToast(msg, "error");
        } else {
            alert(msg);
        }
    });
});

// ---------- SUPER SIMPLE DRUG SEARCH (no classes needed) ----------

$(document).on("input", "#drugSearchBox", function () {
    const q = ($(this).val() || "").toLowerCase().trim();

    // look at every row inside the drugs table
    $("#drugTableBody tr").each(function () {
        const rowText = $(this).text().toLowerCase();

        if (!q || rowText.indexOf(q) !== -1) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
});

