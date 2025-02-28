function initLoginForm() {
    $('#login-form').submit(function (event) {
        event.preventDefault(); // prevent form from submitting
        removeErrorInputs($('#login-form'));

        // get form data
        var formData = {
            username: $('#username').val(),
            password: $('#password').val()
        };

        $('input').removeClass('error');

        disableForm($('#login-form'));
        // send ajax request
        $.ajax({
            type: 'POST',
            url: '/User/Login',
            data: formData
        })
            .done(async function (result) {
                if (!result.success) { //TODO: Something goes wrong here when signing in - console error
                    toastr.error(`Error: ${result.errorMessage}`);
                    addErrorInputs($('#login-form'));
                    return;
                }
                localStorage.setItem('token', result.token);
                localStorage.setItem('username', result.username);
                await cleanUpExpiredCache();
                window.location.href = '/Builds';
            })
            .fail(function (xhr, status, error) {
                // ajax error, show error messages
                console.log(error);
                toastr.error('An error occurred while logging in.');
            })
            .always(function () {
                enableForm($('#login-form'));
            });
    });
}

function initSignUpForm() {
    $('#signup-form').submit(function (event) {
        event.preventDefault(); // prevent the form from submitting normally
        removeErrorInputs($('#signup-form'));
        var validationError = false;

        const regex = /^[a-zA-Z0-9_.-]+$/;
        if (!regex.test($('#new-username').val())) {
            $('#new-username').addClass('error');
            toastr.error('Username may only contain letters, numbers, underscores, hyphens, or periods');
            validationError = true;
        }

        var password = $('#new-password').val();
        if (!validatePassword(password)) {
            $('#new-password').addClass('error');
            validationError = true;
        }

        if (validationError) {
            return;
        }

        toastr.info('Working...');

        // get the form data
        var formData = {
            'username': $('#new-username').val(),
            'email': $('#new-email').val(),
            'password': password
        };

        disableForm($('#signup-form'));
        // send an AJAX POST request to the CreateUser action method
        $.ajax({
            type: 'POST',
            url: '/User/CreateUser',
            data: formData,
            dataType: 'json',
            encode: true
        })
            .done(function (result) {
                if (!result.success) {
                    toastr.clear();
                    toastr.error(`Error: ` + result.errorMessage);
                    if (result.errorMessage.toLowerCase().includes('username')) {
                        $('#new-username').addClass('error');
                    }
                    if (result.errorMessage.toLowerCase().includes('email')) {
                        $('#new-email').addClass('error');
                    }
                    if (result.errorMessage.toLowerCase().includes('password')) {
                        $('#new-password').addClass('error');
                    }
                    return;
                }
                toastr.clear();
                toastr.success('User Created Successfully!')
                hideSignupForm();
            })
            .fail(function (xhr, status, error) {
                // handle the AJAX request failure
                toastr.clear();
                toastr.error('Something went wrong')
                console.log(error);
            })
            .always(function () {
                // re-enable the form and hide the loading screen
                enableForm($('#signup-form'));
            });
    });
}

function initPasswordResetForm() {
    $('#password-reset-popup').submit(function (event) {
        event.preventDefault(); // prevent form from submitting
        
        // get form data
        var formData = {
            email: $('#reset-email').val(),
        };

        $('input').removeClass('error');

        disableForm($('#password-reset-popup'));
        $.ajax({
            type: 'POST',
            url: '/User/RequestPasswordReset',
            data: formData
        })
            .done(function (result) {
                if (!result.success) {
                    toastr.error(`Error: ${result.errorMessage}`);
                    return;
                };
                toastr.success(result.message);
                hidePasswordResetForm();
            })
            .fail(function (xhr, status, error) {
                console.log(error);
                toastr.error('An error occurred, try again later.');
            })
            .always(function () {
                enableForm($('#password-reset-popup'));
            });
    });
}

function initNewPasswordForm() {
    $('#new-password-popup').submit(function (event) {
        event.preventDefault(); // prevent form from submitting
        var password = $('#token-new-password').val();
        if (!validatePassword(password)) {
            addErrorInputs($('#new-password-popup'));
            return;
        }
        removeErrorInputs($('#new-password-popup'));

        var formData = {
            password: password,
            token: window.location.pathname.split('/')[2]
        };

        disableForm($('#new-password-popup'));
        $.ajax({
            type: 'POST',
            url: '/User/UpdatePasswordWithToken',
            data: formData
        })
            .done(function (result) {
                if (!result.success) {
                    toastr.error(`Error: ${result.errorMessage}`);
                    return;
                };
                toastr.success(result.message);
                hideNewPasswordForm();
            })
            .fail(function (xhr, status, error) {
                console.log(error);
                toastr.error('An error occurred, try again later.');
            })
            .always(function () {
                enableForm($('#new-password-popup'));
            });
    });
}

function showSignupForm() {
    $('#new-user-form')[0].reset();  
    removeErrorInputs($('#new-user-form'));    
    $('#signup-form').show();
    $('#new-email').focus();

}

function hideSignupForm() {
    $('#signup-form').hide();
}

function showPasswordResetForm() {
    $('#password-reset-form')[0].reset();
    removeErrorInputs($('#password-reset-popup'));
    $('#password-reset-popup').show();
    $('#reset-email').focus();
}

function hidePasswordResetForm() {
    $('#password-reset-popup').hide();
}

function showNewPasswordForm() {
    $('#new-password-form')[0].reset();
    removeErrorInputs($('#new-password-popup'));
    $('#new-password-popup').show();
}

function hideNewPasswordForm() {
    $('#new-password-popup').hide();
}

function validatePassword(password) {
    minLength = 8;
    maxLength = 100;
    if (password.length < minLength) {
        toastr.error(`Password must be at least ${minLength} characters long.`);
        return false;
    }
    if (password.length > maxLength) {
        toastr.error(`Password must not exceed ${maxLength} characters.`);
        return false;
    }
    return true;
}

function submitActivePopup() {
    // Check if sign-up button is visible
    if ($('#signup-form').is(':visible')) {
        $('#signup-submit').trigger('click');
    }
    else if ($('#password-reset-form').is(':visible')) {
        $('#password-reset-submit').trigger('click');
    }
    else if ($('#new-password-form').is(':visible')) {
        $('#new-password-submit').trigger('click');
    }
    else {
        $('#login-button').trigger('click');
    }

}

function closeActivePopup() {
    if ($('#confirmModal').is(':visible')) {
        $('#confirmModal-cancel').trigger('click');
    }
    else if ($('#signup-form').is(':visible')) {
        $('#signup-cancel').trigger('click');
    }
    else if ($('#password-reset-popup').is(':visible')) {
        $('#password-reset-cancel').trigger('click');
    }
    else if ($('#new-password-popup').is(':visible')) {
        $('#new-password-cancel').trigger('click');
    }

}

async function parseUrl() {
    var resetToken = window.location.pathname.split('/')[2];
    if (resetToken) {
        showNewPasswordForm();
        $('#token-new-password').focus();
        return;
    }
    if (await validateUserToken()) {
        if (window.location.pathname != '/Builds') {
            window.location.href = '/Builds';
        }
    }
    $('#username').focus();
}

$(document).ready(function () {
    initToastrSettings();
    checkMobile();
    parseUrl();
    initLoginForm();
    initSignUpForm();   
    initPasswordResetForm();
    initNewPasswordForm();
});

