// ===== OPEN MODALS =====

function openEditProfile() {
    $("#settingsModalContent").load("/Settings/EditProfile", function (response, status, xhr) {
        if (status === "error") {
            settingsShowError("Error loading profile form.", xhr);
        } else {
            $("#settingsModal").modal("show");
        }
    });
}

function openChangePassword() {
    $("#settingsModalContent").load("/Settings/ChangePassword", function (response, status, xhr) {
        if (status === "error") {
            settingsShowError("Error loading password form.", xhr);
        } else {
            $("#settingsModal").modal("show");
        }
    });
}

// ===== AJAX SUBMITS =====

// Edit profile
$(document).on("submit", "#editProfileForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#settingsModal").modal("hide");
            settingsShowSuccess(res && res.message ? res.message : "Profile updated successfully.");
            location.reload();   // refresh name/email in header and page
        },
        error: function (xhr) {
            var msg = xhr.responseText || "Error updating profile.";
            settingsShowError(msg, xhr);
        }
    });
});

// Change password
$(document).on("submit", "#changePasswordForm", function (e) {
    e.preventDefault();
    var $form = $(this);

    $.ajax({
        url: $form.attr("action"),
        method: "POST",
        data: $form.serialize(),
        success: function (res) {
            $("#settingsModal").modal("hide");
            settingsShowSuccess(res && res.message ? res.message : "Password changed successfully.");
        },
        error: function (xhr) {
            var msg = xhr.responseText || "Error changing password.";
            settingsShowError(msg, xhr);
        }
    });
});

// ===== TOAST HELPERS (reuse your global toast) =====

function settingsShowSuccess(message) {
    if (typeof showAppToast === "function") {
        showAppToast(message, "success");
    } else if (typeof showBpToast === "function") {
        showBpToast(message, "success");
    } else {
        alert(message);
    }
}

function settingsShowError(message, xhr) {
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
