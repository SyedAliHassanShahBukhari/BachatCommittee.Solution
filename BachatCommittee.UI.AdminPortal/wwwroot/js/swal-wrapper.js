function successMsg(message) {
    Swal.fire({
        text: message,
        icon: "success",
        buttonsStyling: false,
        confirmButtonText: "Ok, got it!",
        customClass: {
            confirmButton: "btn btn-primary"
        }
    });
}

function infoMsg(message) {
    Swal.fire({
        text: message,
        icon: "info",
        buttonsStyling: false,
        confirmButtonText: "Ok, got it!",
        customClass: {
            confirmButton: "btn btn-info"
        }
    });
}

function errorMsg(message) {
    Swal.fire({
        text: message,
        icon: "error",
        buttonsStyling: false,
        confirmButtonText: "Ok, got it!",
        customClass: {
            confirmButton: "btn btn-danger"
        }
    });
}

function warnMsg(message) {
    Swal.fire({
        text: message,
        icon: "warning",
        buttonsStyling: false,
        confirmButtonText: "Ok, got it!",
        customClass: {
            confirmButton: "btn btn-warning"
        }
    });
}

function showAlert(type, msg, isHtml = false) {
    Swal.fire({
        [isHtml ? 'html' : 'text']: msg,
        icon: type,
        buttonsStyling: false,
        confirmButtonText: "Ok, got it!",
        customClass: {
            confirmButton: "btn btn-" + (
                type === 'success' ? 'primary' :
                    type === 'error' ? 'danger' :
                        type === 'warning' ? 'warning text-white' :
                            type === 'info' ? 'info text-white' : 'secondary'
            )
        }
    });
}
