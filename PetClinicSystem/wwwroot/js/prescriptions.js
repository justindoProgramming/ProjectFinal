// This lets us keep using $("#...").modal("show") even though Bootstrap 5
// removed the jQuery plugin.
(function ($, bootstrap) {
    if (!$ || !bootstrap) return;
    if (!$.fn.modal) {
        $.fn.modal = function (command) {
            return this.each(function () {
                var modalEl = this;
                var instance = bootstrap.Modal.getOrCreateInstance(modalEl);
                if (command === "show") {
                    instance.show();
                } else if (command === "hide") {
                    instance.hide();
                } else if (command === "toggle") {
                    instance.toggle();
                }
            });
        };
    }
})(window.jQuery, window.bootstrap);

// ================== LIST RELOAD ==================
function loadPrescriptions() {
    $("#prescriptionsList").load("/BillingPharmacy/PrescriptionList");
}

// ================== LOAD MODALS ==================

function loadPrescriptionCreate() {
    $("#prescCreateContent").load("/BillingPharmacy/CreatePrescription", function (response, status, xhr) {
        if (status === "error") {
            if (typeof showBpToast === "function") {
                showBpToast("Error loading create form: " + (xhr.statusText || ""), "error");
            } else {
                alert("Error loading create form: " + (xhr.statusText || ""));
            }
        } else {
            $("#prescCreateModal").modal("show");
        }
    });
}

function loadPrescriptionView(id) {
    $("#prescViewContent").load("/BillingPharmacy/ViewPrescription/" + id, function (response, status, xhr) {
        if (status === "error") {
            if (typeof showBpToast === "function") {
                showBpToast("Error loading prescription details: " + (xhr.statusText || ""), "error");
            } else {
                alert("Error loading prescription details: " + (xhr.statusText || ""));
            }
        } else {
            $("#prescViewModal").modal("show");
        }
    });
}

function loadPrescriptionEdit(id) {
    $("#prescEditContent").load("/BillingPharmacy/EditPrescription/" + id, function (response, status, xhr) {
        if (status === "error") {
            if (typeof showBpToast === "function") {
                showBpToast("Error loading edit form: " + (xhr.statusText || ""), "error");
            } else {
                alert("Error loading edit form: " + (xhr.statusText || ""));
            }
        } else {
            $("#prescEditModal").modal("show");
        }
    });
}

function loadPrescriptionDelete(id) {
    $("#prescDeleteContent").load("/BillingPharmacy/DeletePrescription/" + id, function (response, status, xhr) {
        if (status === "error") {
            if (typeof showBpToast === "function") {
                showBpToast("Error loading delete confirmation: " + (xhr.statusText || ""), "error");
            } else {
                alert("Error loading delete confirmation: " + (xhr.statusText || ""));
            }
        } else {
            $("#prescDeleteModal").modal("show");
        }
    });
}

// ================== FORM SUBMIT (AJAX) ==================

// CREATE
$(document).on("submit", "#createPrescriptionForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#prescCreateModal").modal("hide");
            loadPrescriptions();

            if (typeof showBpToast === "function") {
                showBpToast(res && res.message ? res.message : "Prescription created.", "success");
            } else {
                alert(res && res.message ? res.message : "Prescription created.");
            }
        },
        error: function (xhr) {
            if (typeof showBpToast === "function") {
                showBpToast(xhr.responseText || "Error creating prescription.", "error");
            } else {
                alert(xhr.responseText || "Error creating prescription.");
            }
        }
    });
});

// EDIT
$(document).on("submit", "#editPrescriptionForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#prescEditModal").modal("hide");
            loadPrescriptions();

            if (typeof showBpToast === "function") {
                showBpToast(res && res.message ? res.message : "Prescription updated.", "success");
            } else {
                alert(res && res.message ? res.message : "Prescription updated.");
            }
        },
        error: function (xhr) {
            if (typeof showBpToast === "function") {
                showBpToast(xhr.responseText || "Error updating prescription.", "error");
            } else {
                alert(xhr.responseText || "Error updating prescription.");
            }
        }
    });
});

// DELETE
$(document).on("submit", "#deletePrescriptionForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#prescDeleteModal").modal("hide");
            loadPrescriptions();

            if (typeof showBpToast === "function") {
                showBpToast(res && res.message ? res.message : "Prescription deleted.", "success");
            } else {
                alert(res && res.message ? res.message : "Prescription deleted.");
            }
        },
        error: function (xhr) {
            if (typeof showBpToast === "function") {
                showBpToast(xhr.responseText || "Error deleting prescription.", "error");
            } else {
                alert(xhr.responseText || "Error deleting prescription.");
            }
        }
    });
});

// ================== SEARCH FILTER ==================

function filterPrescriptions(term) {
    term = (term || "").toLowerCase();
    $("#prescriptionsTable tbody tr").each(function () {
        var text = $(this).text().toLowerCase();
        $(this).toggle(text.indexOf(term) !== -1);
    });
}

// Ensure our functions win over any legacy ones (e.g. in _POS_Scripts)
(function () {
    function bindPrescriptionGlobals() {
        window.loadPrescriptions = loadPrescriptions;
        window.loadPrescriptionCreate = loadPrescriptionCreate;
        window.loadPrescriptionView = loadPrescriptionView;
        window.loadPrescriptionEdit = loadPrescriptionEdit;
        window.loadPrescriptionDelete = loadPrescriptionDelete;
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", bindPrescriptionGlobals);
    } else {
        // DOM already parsed; run soon so we override any earlier definitions
        setTimeout(bindPrescriptionGlobals, 0);
    }
})();
