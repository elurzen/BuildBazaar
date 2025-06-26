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

function initSlider(sliderPrefix, localStorageVar, updateCallback, callbackParam) {
    const slider = $(`#${sliderPrefix}-slider`);
    const button = $(`#${sliderPrefix}-slider-button`);
    const options = $(`.${sliderPrefix}-slider-option`);
    //updateCallback = functionNoParens;
    //localStorageVar = 'localStorageVarName';

    let activeSliderOption = 0;
    if (localStorageVar) {
        activeSliderOption  = parseInt(localStorage.getItem(localStorageVar)) || 0; // Tracks the current active option (0 = PoE, 1 = PoE 2)
    }

    const updateSlider = () => {
        // Update active state
        options.each(function (index) {
            $(this).toggleClass('active', index === activeSliderOption);
        });

        // Move the slider button
        const position = activeSliderOption === 0 ? 0 : (slider.width() / 2) - 1;
        button.css('transform', `translateX(${position}px)`);

        if (updateCallback) {
            updateCallback(activeSliderOption, callbackParam);
        }
    };

    slider.on('click', function () {
        if (localStorageVar) {
            activeSliderOption = parseInt(localStorage.getItem(localStorageVar)) || 0;
        }
        // Toggle between the two options
        activeSliderOption = activeSliderOption === 0 ? 1 : 0;
        if (localStorageVar) {
            localStorage.setItem(localStorageVar, activeSliderOption);
        }
        updateSlider();
    });

    // Initialize the slider with the first option active
    updateSlider();
    slider.removeClass('hidden');
    button.removeClass('hidden');
}


function updateHiddenHeaderSlider(activeSliderOption, callbackParam) {
    const slider = $(`#${callbackParam}-slider`);
    const button = $(`#${callbackParam}-slider-button`);
    const options = $(`.${callbackParam}-slider-option`);

    options.each(function (index) {
        $(this).toggleClass('active', index === activeSliderOption);
    });
    const position = activeSliderOption === 0 ? 0 : (slider.width() / 2) - 1;
    button.css('transform', `translateX(${position}px)`);

    const poeHeader = $('#poe-header');
    const poe2Header = $('#poe2-header');

    const poeLis = $('.poe-li');
    const poe2Lis = $('.poe2-li');

    poeHeader.css('display', activeSliderOption === 0 ? 'inline-flex' : 'none');
    poe2Header.css('display', activeSliderOption === 1 ? 'inline-flex' : 'none');

    poeLis.css('display', activeSliderOption === 0 ? 'flex' : 'none');
    poe2Lis.css('display', activeSliderOption === 1 ? 'flex' : 'none');
}

function updateClassDropdown(activeSliderOption, dropdownID) {
    const classDropdown = $(`#${dropdownID}`);
    classDropdown.find('option:not(:first)').hide();
    classDropdown.find('option').filter(function () {
        return $(this).data('game-id') === activeSliderOption + 1 || $(this).val() === "0";
    }).show();
    classDropdown.val("");
}

function initUniversalJsSliders() {
    initSlider('header', 'headerSliderPosition', updateHiddenHeaderSlider, 'header-menu');
    initSlider('header-menu', 'headerSliderPosition', updateHiddenHeaderSlider, 'header');
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
    const dbName = 'BuildBazaarIndexedDB';
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

function openIndexedDB(dbName = 'BuildBazaarIndexedDB') {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, 1);

        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            if (!db.objectStoreNames.contains('ImageCacheStore')) {
                const imageStore = db.createObjectStore('ImageCacheStore', { keyPath: 'filePath' });
                imageStore.createIndex('expiration', 'expiration', { unique: false });
            }

            if (!db.objectStoreNames.contains('TagCacheStore')) {
                const tagsStore = db.createObjectStore('TagCacheStore', { keyPath: 'tagID' });
                tagsStore.createIndex('tagName', 'tagName', { unique: false });
                tagsStore.createIndex('gameID', 'gameID', { unique: false });
                tagsStore.createIndex('tagName_gameID', ['tagName', 'gameID'], { unique: true });
            }

            if (!db.objectStoreNames.contains('ClassCacheStore')) {
                const classesStore = db.createObjectStore('ClassCacheStore', { keyPath: 'classID' });
                classesStore.createIndex('gameID', 'gameID', { unique: false });
            }

            if (!db.objectStoreNames.contains('MetadataStore')) {
                db.createObjectStore('MetadataStore', { keyPath: 'metadataName' });            
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

function addOrUpdateCacheEntry(data, dbName, storeName) {
    return new Promise(async (resolve, reject) => {
        const db = await openIndexedDB(dbName);
        const transaction = db.transaction(storeName, 'readwrite');
        const store = transaction.objectStore(storeName);
        //const data = { key, value, expiration };
        if (storeName === 'TagCacheStore') {
            var x = 3;
        }

        const request = store.put(data);

        request.onsuccess = () => resolve(true);
        request.onerror = (event) => reject(event.target.error);
    });
}

function getCacheEntry(key, dbName, storeName) {
    return new Promise(async (resolve, reject) => {
        const db = await openIndexedDB(dbName);
        const transaction = db.transaction(storeName, 'readonly');
        const store = transaction.objectStore(storeName);

        const request = store.get(key);

        request.onsuccess = (event) => resolve(event.target.result);
        request.onerror = (event) => reject(event.target.error);
    });
}

function removeExpiredEntries(dbName, storeName) {
    return new Promise(async (resolve, reject) => {
        const db = await openIndexedDB(dbName);
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
    const dbName = 'BuildBazaarIndexedDB';
    const storeName = 'ImageCacheStore';
    const expiration = Date.now() + (expirationInDays * 24 * 60 * 60 * 1000)
    const data = { filePath: filePath, url: url, expiration: expiration };

    try {
        await addOrUpdateCacheEntry(data, dbName, storeName);
    } catch (err) {
        console.error('Failed to cache the image URL:', err);
    }
}

async function getCachedImageUrl(filePath) {
    const dbName = 'BuildBazaarIndexedDB';
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

async function cacheClass(classID, className, gameID) {
    const dbName = 'BuildBazaarIndexedDB';
    const storeName = 'ClassCacheStore';
    const data = { classID: classID, className: className, gameID: gameID};

    try {
        await addOrUpdateCacheEntry(data, dbName, storeName);
    } catch (err) {
        console.error('Failed to cache the class:', err);
    }
}

async function cacheTag(TagID, TagName, GameID) {
    const dbName = 'BuildBazaarIndexedDB';
    const storeName = 'TagCacheStore';
    const data = { tagID: TagID, tagName: TagName, gameID: GameID };

    try {
        await addOrUpdateCacheEntry(data, dbName, storeName);
    } catch (err) {
        console.error('Failed to cache the Tag:', err);
    }
}

async function cacheMetadata(MetadataName, MetadataValue) {
    const dbName = 'BuildBazaarIndexedDB';
    const storeName = 'MetadataStore';
    const data = { metadataName: MetadataName, metadataValue: MetadataValue };

    try {
        await addOrUpdateCacheEntry(data, dbName, storeName);
    } catch (err) {
        console.error('Failed to cache the Tag:', err);
    }
}

async function getMetadata(MetadataName) {
    const dbName = 'BuildBazaarIndexedDB';
    const storeName = 'MetadataStore';

    try {
        const entry = await getCacheEntry(MetadataName, dbName, storeName);
        if (entry) {
            return entry.metadataValue;
        }
        return 0; // Not found
    } catch (err) {
        console.error('Failed to retrieve metadata:', err);
        return 0;
    }
}


async function getClassesAndTags() {

    const maxTagID = await getMetadata('maxTagID');
    const maxClassID = await getMetadata('maxClassID');
    return new Promise((resolve, reject) => {
        $.ajax({
            type: 'POST',
            url: '/Build/GetClassesAndTags',
            data: { lastTagId: maxTagID, lastClassId: maxClassID }
        })
            .done(function (result) {
                if (!result.success) {
                    console.log(result.errorMessage); // Fixed to use result.errorMessage
                    toastr.error('An error occurred, try again later.');
                    reject(new Error(result.errorMessage));
                    return;
                }
                var newMaxClassID = maxTagID;
                var newMaxTagID = maxClassID;
                $.each(result.classes, function (index, classItem) {
                    cacheClass(classItem.classID, classItem.className, classItem.gameID)
                    if(classItem.classID > newMaxClassID) {
                        newMaxClassID = classItem.classID;
                    }
                });
                $.each(result.tags, function (index, tagItem) {
                    cacheTag(tagItem.tagID, tagItem.tagName, tagItem.gameID)
                    if(tagItem.tagID > newMaxTagID) {
                        newMaxTagID = tagItem.tagID;
                    }
                });
                cacheMetadata('maxTagID', newMaxTagID);
                cacheMetadata('maxClassID', newMaxClassID);
                resolve();
            })
            .fail(function (xhr, status, error) {
                console.log(error);
                toastr.error('An error occurred, try again later.');
                reject(error);
            });
    });
}

function initTagAutocomplete() {
    // Select all tag input fields (those with class 'tags' and specific IDs)
    const tagInputs = $(".tag-autocomplete");

    // Initialize autocomplete for each input
    tagInputs.each(function () {
        const tagInput = $(this);

        // Create unique autocomplete box for this input
        const autocompleteBox = $("<div>")
            .addClass("autocomplete-box")
            .attr("id", "autocomplete-" + tagInput.attr("id"))
            .hide();

        //todo: changing this to body instead
        //tagInput.after(autocompleteBox);
        $("body").append(autocompleteBox);

        // Store current focus as a data attribute on the input
        tagInput.data("currentFocus", -1);

        // Input event for the tag field
        tagInput.on("input", function () {
            updateAutocomplete(tagInput);
        });

        // Handle arrow key navigation and Enter selection
        tagInput.on("keydown", function (e) {
            const autocompleteBox = $("#autocomplete-" + tagInput.attr("id"));
            const items = autocompleteBox.find(".autocomplete-item");
            let currentFocus = tagInput.data("currentFocus");

            if (autocompleteBox.is(":visible")) {
                if (e.key === "ArrowDown") {
                    currentFocus++;
                    if (currentFocus >= items.length) currentFocus = 0;
                    highlightItem(tagInput, currentFocus);
                    e.preventDefault();
                } else if (e.key === "ArrowUp") {
                    currentFocus--;
                    if (currentFocus < 0) currentFocus = items.length - 1;
                    highlightItem(tagInput, currentFocus);
                    e.preventDefault();
                } else if (e.key === "Enter") {
                    if (currentFocus > -1) {
                        e.preventDefault();
                        e.stopPropagation();
                        items.eq(currentFocus).click();
                    }
                }
            }
        });
    });

    // Update positions of all visible autocomplete boxes
    function updateAutocompletePositions() {
        tagInputs.each(function () {
            const tagInput = $(this);
            const autocompleteBox = $("#autocomplete-" + tagInput.attr("id"));

            if (autocompleteBox.is(":visible")) {
                positionAutocompleteBox(tagInput, autocompleteBox); //builds and index js have their own version of this
            }
        });
    }

    // Add event listeners for repositioning
    $(window).on("resize scroll", updateAutocompletePositions);

     //Also update positions when any element scrolls
    //$(document).on("scroll", "*", function () {
    //    updateAutocompletePositions();
    //});

    const container = $('.container')[0];
    container.addEventListener('scroll', function () {
        updateAutocompletePositions();
    });


    // Close autocomplete when clicking elsewhere
    $(document).on("click", function (e) {
        const autocompleteBoxes = $(".autocomplete-box");
        if (!$(e.target).hasClass("tag-autocomplete") &&
            !$(e.target).closest(".autocomplete-box").length) {
            autocompleteBoxes.hide();
        }
    });
}

function positionAutocompleteBox(tagInput, autocompleteBox) {

    // Get the position of the input in the viewport
    const inputRect = tagInput[0].getBoundingClientRect();
    autocompleteBox = autocompleteBox[0];

    // Position the autocomplete box below the input
    autocompleteBox.style.top = inputRect.bottom + "px";
    autocompleteBox.style.left = inputRect.left + "px";
    autocompleteBox.style.width = inputRect.width + "px";
}



async function getAllTags() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open("BuildBazaarIndexedDB");

        request.onsuccess = function (event) {
            const db = event.target.result;
            const transaction = db.transaction(["TagCacheStore"], "readonly");
            const objectStore = transaction.objectStore("TagCacheStore");
            const tags = [];

            objectStore.openCursor().onsuccess = function (event) {
                const cursor = event.target.result;
                if (cursor) {
                    tags.push({
                        tagName: cursor.value.tagName
                    });
                    cursor.continue();
                } else {
                    resolve(tags);
                }
            };

            transaction.onerror = function (event) {
                reject("Error fetching tags");
            };
        };

        request.onerror = function (event) {
            reject("Error opening database");
        };
    });
}

async function updateAutocomplete(tagInput) {
    const inputText = tagInput.val();
    const lastCommaIndex = inputText.lastIndexOf(",");
    const currentTagInput = lastCommaIndex !== -1 ?
        inputText.slice(lastCommaIndex + 1).trim() :
        inputText.trim();

    const autocompleteBox = $("#autocomplete-" + tagInput.attr("id"));

    // If current input is empty, hide the dropdown
    if (currentTagInput === "") {
        autocompleteBox.hide();
        return;
    }

    // Get matching tags
    const allTags = await getAllTags();
    const matchingTags = allTags.filter(tag =>
        tag.tagName.toLowerCase().startsWith(currentTagInput.toLowerCase())
    );

    // Clear previous suggestions
    autocompleteBox.empty();

    if (matchingTags.length > 0) {
        // Add matching tags to dropdown
        matchingTags.forEach(tag => {
            const item = $("<div>")
                .addClass("autocomplete-item")
                .text(tag.tagName)
                .click(function () {
                    selectTag(tagInput, tag.tagName);
                });
            autocompleteBox.append(item);
        });

        // Position and show dropdown
        positionAutocompleteBox(tagInput, autocompleteBox);
        autocompleteBox.show();


        tagInput.data("currentFocus", -1);
    } else {
        autocompleteBox.hide();
    }
}

function selectTag(tagInput, tagName) {
    const inputText = tagInput.val();
    const lastCommaIndex = inputText.lastIndexOf(",");

    if (lastCommaIndex !== -1) {
        // Replace text after last comma with selected tag
        tagInput.val(inputText.slice(0, lastCommaIndex + 1) + " " + tagName + ", ");
    } else {
        // Replace entire input with selected tag
        tagInput.val(tagName + ", ");
    }

    $("#autocomplete-" + tagInput.attr("id")).hide();
    tagInput.focus();
}

function highlightItem(tagInput, currentFocus) {
    const autocompleteBox = $("#autocomplete-" + tagInput.attr("id"));
    autocompleteBox.find(".autocomplete-item").removeClass("active");

    if (currentFocus > -1) {
        autocompleteBox.find(".autocomplete-item").eq(currentFocus).addClass("active");
    }

    tagInput.data("currentFocus", currentFocus);
}

$(document).ready(async function () {
    initToastrSettings();
    await getClassesAndTags();
    initTagAutocomplete();
    initUniversalJsSliders();
    setLogoutButton();
    initLoginForm();
    initSignUpForm();   
});
