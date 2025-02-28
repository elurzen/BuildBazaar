function initToastrSettings() {
    // Set Toastr global options
    toastr.options = {
        "closeButton": true,
        "debug": false,
        "newestOnTop": true,
        "progressBar": true,
        "positionClass": "toast-top-center", // You can change this to top-left, bottom-right, etc.
        "preventDuplicates": false,
        "onclick": null,
        "showDuration": "3000",
        "hideDuration": "1000",
        "timeOut": "5000",  // Time to show before auto-dismiss
        "extendedTimeOut": "1000", // Time for user to interact with the toast before auto-dismiss
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };
}

function validateUserToken() {
    var userToken = localStorage.getItem("token");
    if (userToken == null) {
        if (window.location.pathname == '/') {
            return Promise.resolve();
        }
        return Promise.resolve(false);
    }
    return new Promise(function (resolve, reject) {
        $.ajax({
            type: 'POST',
            url: '/User/WebValidateToken',
            headers: {
                Authorization: userToken
            }
        })
            .then(function (result) {
                resolve(result.success);
            })
            .catch(function (xhr, status, error) {
                toastr.error('An error occurred while logging in');
                console.log(error);
                resolve(false);
            });
    });
}

function disableForm(form) {
    form = $(form);

    form.find('input').prop('disabled', true);
    form.find('button').prop('disabled', true);
}

function enableForm(form) {
    form = $(form);

    form.find('input').prop('disabled', false);
    form.find('button').prop('disabled', false);
}

function addErrorInputs(form) {
    form.find('input').addClass('error');
}

function removeErrorInputs(form) {
    form.find('input').removeClass('error');
}

$(document).on('keydown', function (event) {
    if (event.key === "Escape") {
        closeActivePopup();
    }
    if (event.key === "Enter") {
        submitActivePopup();
    }
});

async function cleanUpExpiredCache() {
    const dbName = 'ImageCacheDB';
    const storeName = 'ImageCacheStore';

    try {
        await removeExpiredEntries(dbName, storeName);
        console.log('Expired cache entries cleaned up.');
    } catch (err) {
        console.error('Failed to clean up expired cache:', err);
    }
}

function checkMobile() {
    const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    return isMobile;
}