// Refresh vaccinations list
function loadVaccinations() {
    $("#vaccinationsContainer").load("/BillingPharmacy/VaccinationList");
}

// ===== OPEN MODALS =====
function loadVaccinationCreate() {
    $("#vaccCreateContent").load("/BillingPharmacy/CreateVaccination", function () {
        $("#vaccCreateModal").modal("show");
    });
}

function loadVaccinationView(id) {
    $("#vaccViewContent").load("/BillingPharmacy/ViewVaccination/" + id, function () {
        $("#vaccViewModal").modal("show");
    });
}

function loadVaccinationEdit(id) {
    $("#vaccEditContent").load("/BillingPharmacy/EditVaccination/" + id, function () {
        $("#vaccEditModal").modal("show");
    });
}

function loadVaccinationDelete(id) {
    $("#vaccDeleteContent").load("/BillingPharmacy/DeleteVaccination/" + id, function () {
        $("#vaccDeleteModal").modal("show");
    });
}

// ===== AJAX FORM SUBMIT =====
$(document).on("submit", "#createVaccinationForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#vaccCreateModal").modal("hide");
            loadVaccinations();

            // ✅ use toast instead of alert
            if (typeof showBpToast === "function") {
                showBpToast(res && res.message
                    ? res.message
                    : "Vaccination record added successfully.", "success");
            } else {
                alert(res && res.message ? res.message : "Vaccination record added successfully.");
            }
        },
        error: function (xhr) {
            if (typeof showBpToast === "function") {
                showBpToast(xhr.responseText || "Error creating vaccination.", "error");
            } else {
                alert(xhr.responseText || "Error creating vaccination.");
            }
        }
    });
});

$(document).on("submit", "#editVaccinationForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#vaccEditModal").modal("hide");
            loadVaccinations();

            if (typeof showBpToast === "function") {
                showBpToast(res && res.message
                    ? res.message
                    : "Vaccination record updated successfully.", "success");
            } else {
                alert(res && res.message ? res.message : "Vaccination record updated successfully.");
            }
        },
        error: function (xhr) {
            if (typeof showBpToast === "function") {
                showBpToast(xhr.responseText || "Error updating vaccination.", "error");
            } else {
                alert(xhr.responseText || "Error updating vaccination.");
            }
        }
    });
});

$(document).on("submit", "#deleteVaccinationForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function () {
            $("#vaccDeleteModal").modal("hide");
            loadVaccinations();

            if (typeof showBpToast === "function") {
                showBpToast("Vaccination record deleted.", "success");
            } else {
                alert("Vaccination record deleted.");
            }
        },
        error: function (xhr) {
            if (typeof showBpToast === "function") {
                showBpToast(xhr.responseText || "Error deleting vaccination.", "error");
            } else {
                alert(xhr.responseText || "Error deleting vaccination.");
            }
        }
    });
});

// ===== SIMPLE SEARCH =====
function filterVaccinations(term) {
    term = term.toLowerCase();
    $("#vaccinationsTable tbody tr").each(function () {
        const text = $(this).text().toLowerCase();
        $(this).toggle(text.indexOf(term) !== -1);
    });
}
