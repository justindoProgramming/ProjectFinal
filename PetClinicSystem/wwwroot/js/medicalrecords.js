// ================== LOAD MODALS ==================

function loadRecordCreate() {
    $("#recordCreateContent").load("/MedicalRecords/Create", function (response, status, xhr) {
        if (status === "error") {
            mrShowError("Error loading create form", xhr);
        } else {
            $("#recordCreateModal").modal("show");
        }
    });
}

function loadRecordEdit(id) {
    $("#recordEditContent").load("/MedicalRecords/Edit/" + id, function (response, status, xhr) {
        if (status === "error") {
            mrShowError("Error loading edit form", xhr);
        } else {
            $("#recordEditModal").modal("show");
        }
    });
}

function loadRecordDelete(id) {
    $("#recordDeleteContent").load("/MedicalRecords/Delete/" + id, function (response, status, xhr) {
        if (status === "error") {
            mrShowError("Error loading delete confirmation", xhr);
        } else {
            $("#recordDeleteModal").modal("show");
        }
    });
}

// ================== FORM SUBMIT (AJAX) ==================

// CREATE
$(document).on("submit", "#createRecordForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#recordCreateModal").modal("hide");
            mrShowSuccess(res && res.message ? res.message : "Medical record created successfully!");
            location.reload();
        },
        error: function (xhr) {
            mrShowError(xhr.responseText || "Error creating medical record.", xhr);
        }
    });
});

// EDIT
$(document).on("submit", "#editRecordForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#recordEditModal").modal("hide");
            mrShowSuccess(res && res.message ? res.message : "Medical record updated successfully!");
            location.reload();
        },
        error: function (xhr) {
            mrShowError(xhr.responseText || "Error updating medical record.", xhr);
        }
    });
});

// DELETE
$(document).on("submit", "#deleteRecordForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#recordDeleteModal").modal("hide");
            mrShowSuccess(res && res.message ? res.message : "Medical record deleted successfully!");
            location.reload();
        },
        error: function (xhr) {
            mrShowError(xhr.responseText || "Error deleting medical record.", xhr);
        }
    });
});

// ================== SEARCH FILTER ==================

function filterMedicalRecords(term) {
    term = (term || "").toLowerCase();
    $("#recordsContainer .medical-record-row").each(function () {
        var text = $(this).text().toLowerCase();
        $(this).toggle(text.indexOf(term) !== -1);
    });
}

// ================== TOAST HELPERS ==================

function mrShowSuccess(message) {
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

function mrShowError(message, xhr) {
    if (!message && xhr) {
        message = xhr.statusText || "Error.";
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
