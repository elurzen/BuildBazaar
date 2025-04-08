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

function initHeaderSlider() {
    const slider = $('#header-slider');
    const button = $('#header-slider-button');
    const options = $('.header-slider-option');

    const menuSlider = $('#header-menu-slider');
    const menuButton = $('#header-menu-slider-button');
    const menuOptions = $('.header-menu-slider-option');

    const poeHeader = $('#poe-header');
    const poe2Header = $('#poe2-header');

    const poeLis = $('.poe-li');
    const poe2Lis = $('.poe2-li');

    let activeSliderOption = parseInt(localStorage.getItem('headerSliderPosition')) || 0; // Tracks the current active option (0 = PoE, 1 = PoE 2)

    const updateHeaderSlider = () => {
        // Update active state
        options.each(function (index) {
            $(this).toggleClass('active', index === activeSliderOption);
        });

        menuOptions.each(function (index) {
            $(this).toggleClass('active', index === activeSliderOption);
        });

        // Move the slider button
        const position = activeSliderOption === 0 ? 0 : (slider.width() / 2) - 1;
        button.css('transform', `translateX(${position}px)`);

        const menuPosition = activeSliderOption === 0 ? 0 : (menuSlider.width() / 2) - 1;
        menuButton.css('transform', `translateX(${menuPosition}px)`);

        poeHeader.css('display', activeSliderOption === 0 ? 'inline-flex' : 'none');
        poe2Header.css('display', activeSliderOption === 1 ? 'inline-flex' : 'none');

        poeHeader.css('display', activeSliderOption === 0 ? 'inline-flex' : 'none');
        poe2Header.css('display', activeSliderOption === 1 ? 'inline-flex' : 'none');

        poeLis.css('display', activeSliderOption === 0 ? 'flex' : 'none');
        poe2Lis.css('display', activeSliderOption === 1 ? 'flex' : 'none');
    };

    slider.on('click', function () {
        // Toggle between the two options
        activeSliderOption = activeSliderOption === 0 ? 1 : 0;
        localStorage.setItem('headerSliderPosition', activeSliderOption);
        updateHeaderSlider();
    });

    menuSlider.on('click', function () {
        // Toggle between the two options
        activeSliderOption = activeSliderOption === 0 ? 1 : 0;
        localStorage.setItem('headerSliderPosition', activeSliderOption);
        updateHeaderSlider();
    });

    // Initialize the slider with the first option active
    updateHeaderSlider();
    slider.removeClass('hidden');
    button.removeClass('hidden');
    menuSlider.removeClass('hidden');
    menuButton.removeClass('hidden');
}


async function setLogoutButton() {    
    var loggedIn = await validateUserToken();
    if (!loggedIn) {
        $('#logout-image').attr('src', '/media/flyout.png');
        $('#logout-image').css('height', '30px');
        $('#logout-image').css('width', '45px');
        $('#logout-text').text('Log In');
    }
    else {
        $('#logout-image').attr('src', '/media/logout.png');
        $('#logout-image').css('height', '30px');
        $('#logout-image').css('width', '30px');
        $('#logout-text').text('Log Out');
    }
}
async function logout() {
    var loggedIn = await validateUserToken();
    if (loggedIn) {
        localStorage.clear();
        await cleanUpExpiredCache();
        toastr.success('Logged out successfully');
        if (window.location.href.includes('/Builds/')) {
            window.location.href = "/";
        }
        if (window.location.href.includes('/Public/')) {
            populatePublicBuilds();
        }
        setLogoutButton();
    }
    else {
        showLoginForm();
    }
}

function logoClick() {
    var userToken = localStorage.getItem("token");
    if (userToken == null) {
        window.location.href = "/";
        return;
    }

    if (window.location.pathname == '/') {
        window.location.href = '/Builds';
        return;
    }
    
    window.location.href = '/';
}

function toggleHeaderMenu() {
    $('#header-menu').toggle();
}

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
                if (window.location.href.includes('/Builds/')) {
                    populateBuilds();
                }
                if (window.location.href.includes('/Public/')) {
                    populatePublicBuilds();
                }
                setLogoutButton();
                hideLoginForm();
                //window.location.href = '/Builds';
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

function showLoginForm() {
    $('#login-form')[0].reset();
    removeErrorInputs($('#login-form'));
    $('#login-popup').show();
    $('#username').focus();
}

function hideLoginForm() {
    $('#login-popup').hide();
}

function showSignupForm() {
    $('#signup-form')[0].reset();
    removeErrorInputs($('#signup-form'));
    hideLoginForm();
    $('#signup-popup').show();
    $('#new-email').focus();
}

function hideSignupForm() {
    $('#signup-popup').hide();
}

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

async function populateImageCache(records) {
    var userToken = localStorage.getItem('token');
    let missingCacheImages = [];
    for (const record of records) {
        const cachedFilePath = await getCachedImageUrl(record.filePath);

        if (!cachedFilePath) {
            missingCacheImages.push(record.imageID);
        }

    };
    // If there are missing files, make a bulk request to fetch the URLs
    if (missingCacheImages.length > 0) {
        await $.ajax({
            type: 'POST',
            url: '/BuildData/GetBulkImageUrls',
            headers: {
                Authorization: userToken
            },
            data: { imageIDs: missingCacheImages }
        })
            .done(function (result) {
                if (!result.success) {
                    toastr.error(result.errorMessage);
                }
                result.urls.forEach((urlObj) => {
                    cacheImageUrl(urlObj.filePath, urlObj.url);
                })
            })
            .fail(function (xhr, status, error) {
                toastr.error('Something went wrong fetching images');
                console.error(error);
            });
    }
}

function storeInCache(filePath, url, expirationInDays = 1) {
    const expiration = Date().getTime() + (expirationInDays * 24 * 60 * 60 * 1000); // 1-day expiry
    const data = { url, expiration };
    localStorage.setItem(filePath, JSON.stringify(data));
}

function openIndexedDB(dbName = DB_NAME, storeName = STORE_NAME) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, 1);

        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(storeName)) {
                const store = db.createObjectStore(storeName, { keyPath: 'filePath' });
                store.createIndex('expiration', 'expiration', { unique: false });
            }
        };

        request.onsuccess = (event) => {
            resolve(event.target.result);
        };

        request.onerror = (event) => {
            reject(event.target.error);
        };
    });
}

function addOrUpdateCacheEntry(filePath, url, expiration, dbName, storeName) {
    return new Promise(async (resolve, reject) => {
        const db = await openIndexedDB(dbName, storeName);
        const transaction = db.transaction(storeName, 'readwrite');
        const store = transaction.objectStore(storeName);
        const data = { filePath, url, expiration };

        const request = store.put(data);

        request.onsuccess = () => resolve(true);
        request.onerror = (event) => reject(event.target.error);
    });
}

function getCacheEntry(filePath, dbName, storeName) {
    return new Promise(async (resolve, reject) => {
        const db = await openIndexedDB(dbName, storeName);
        const transaction = db.transaction(storeName, 'readonly');
        const store = transaction.objectStore(storeName);

        const request = store.get(filePath);

        request.onsuccess = (event) => resolve(event.target.result);
        request.onerror = (event) => reject(event.target.error);
    });
}

function removeExpiredEntries(dbName, storeName) {
    return new Promise(async (resolve, reject) => {
        const db = await openIndexedDB(dbName, storeName);
        const transaction = db.transaction(storeName, 'readwrite');
        const store = transaction.objectStore(storeName);
        const index = store.index('expiration');
        const now = Date.now();

        const request = index.openCursor(IDBKeyRange.upperBound(now));

        request.onsuccess = (event) => {
            const cursor = event.target.result;
            if (cursor) {
                cursor.delete(); // Delete the expired entry
                cursor.continue(); // Continue iterating
            } else {
                resolve(true); // No more expired entries
            }
        };

        request.onerror = (event) => reject(event.target.error);
    });
}

async function cacheImageUrl(filePath, url, expirationInDays = 1) {
    const dbName = 'ImageCacheDB';
    const storeName = 'ImageCacheStore';
    const expiration = Date.now() + expirationInDays * 24 * 60 * 60 * 1000;

    try {
        await addOrUpdateCacheEntry(filePath, url, expiration, dbName, storeName);
    } catch (err) {
        console.error('Failed to cache the image URL:', err);
    }
}

async function getCachedImageUrl(filePath) {
    const dbName = 'ImageCacheDB';
    const storeName = 'ImageCacheStore';

    try {
        const entry = await getCacheEntry(filePath, dbName, storeName);
        if (entry && entry.expiration > Date.now()) {
            return entry.url;
        }
        return null; // Expired or not found
    } catch (err) {
        console.error('Failed to retrieve cached image URL:', err);
        return null;
    }
}

$(document).ready(function () {
    initToastrSettings();
    initHeaderSlider();
    setLogoutButton();
    initLoginForm();
    initSignUpForm();   
    //cleanUpExpiredCache();
    //populateImageCache();
    //checkMobile();
});