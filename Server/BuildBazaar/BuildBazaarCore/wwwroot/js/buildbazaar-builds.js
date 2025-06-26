async function populateBuilds() {
    var userToken = localStorage.getItem('token');
    $.ajax({
        type: 'POST',
        url: '/Build/GetBuilds',
        headers: {
            Authorization: userToken
        }
    })
        .done(async function (result) {
            if (!result.success) {
                toastr.error(result.errorMessage);
                return;
            }
            var list = $('#sidebar-build-list');
            var headerMenuList = $('#header-menu-list');
            var foundBuild = false;
            var currentBuildID = window.location.pathname.split('/')[2] || localStorage.buildID;

            list.empty();
            headerMenuList.find('li.build-item').remove();

            let li = $('<li>').addClass('build-item');
            li.on('click', async function () {
                if (await checkEditing()) {
                    showAddBuildForm();
                    $('#header-menu').hide();
                }
            });
            let buildImg = $('<img>').attr('src', '/media/add-thumbnail.png').addClass('thumbnail');
            let buildName = $('<h4>').text('Create Build');
            li.append(buildImg).append(buildName);
            list.append(li);
            let headerLi = li.clone(true);
            headerMenuList.append(headerLi);

            await populateImageCache(result.builds);

            // Iterate through the rows returned by the database
            for (const record of result.builds) {
                let li = $('<li>').addClass('build-item');
                li.data('buildID', record.buildID);
                li.data('isPublic', record.isPublic);
                if (currentBuildID == record.buildID) {
                    foundBuild = true;
                    li.addClass('selected-build');
                    localStorage.setItem('buildID', currentBuildID);
                    history.replaceState(null, '', `/Builds/${currentBuildID}`);
                }
                li.on('click', async function () {
                    if (await checkEditing()) {
                        var buildID = $(this).data('buildID');
                        localStorage.setItem('buildID', buildID);
                        history.replaceState(null, '', `/Builds/${buildID}`);
                        $('#sidebar-build-list li').removeClass('selected-build');
                        $('#header-menu-list li').removeClass('selected-build');

                        var selectedSidebarBuild = list.children().filter(function () {
                            return $(this).data('buildID') == buildID;
                        });
                        selectedSidebarBuild.addClass('selected-build');

                        var selectedHeaderBuild = headerMenuList.children().filter(function () {
                            return $(this).data('buildID') == buildID;
                        });
                        selectedHeaderBuild.addClass('selected-build');
                        
                        $('#header-menu').hide();
                        //$(this).addClass('selected-build');
                        populateNote();
                        populateReferenceImages();
                        populateBuildUrls();
                        $('#main-row').removeClass('hidden-children');
                    }
                });

                let cachedFilePath = await getCachedImageUrl(record.filePath) || '/media/question-mark.png';
                let buildImg = $('<img>').attr('src', cachedFilePath).addClass('thumbnail');
                let buildName = $('<h4>').text(record.buildName);
                let editImg = $('<img>').attr('src', '/media/edit.png').addClass('editImage').on('click', async function (e) {
                    e.stopPropagation();
                    if (await checkEditing()) {
                        showEditBuildForm(record.buildID, record.imageID, record.filePath, record.buildName, record.isPublic, record.gameID, record.classID, record.tags)
                    }
                });

                li.append(buildImg).append(buildName).append(editImg);
                list.append(li);
                let headerLi = li.clone(true);
                headerMenuList.append(headerLi);
            };

            if (result.builds.length < 1 && !foundBuild) {
                localStorage.removeItem('buildID');
                $('#main-row').addClass('hidden-children')
                $('#sidebar').show();
            } else if (!foundBuild) {
                var secondLi = $('#sidebar li').eq(1);
                var buildID = secondLi.data('buildID');
                localStorage.setItem('buildID', buildID);
                history.replaceState(null, '', `/Builds/${buildID}`);
                secondLi.addClass('selected-build');
            }
            else {
                $('#main-row').removeClass('hidden-children');
            }
            populateNote();
            populateReferenceImages();
            populateBuildUrls();
        })
        .fail(function (xhr, status, error) {
            toastr.error('Failed fetching builds');
            console.log(`Error: ${error}`);
        });
}

async function initAddBuildForm() {
    var selectedThumbnail = null;

    initSlider('add-build', 'addBuildSliderPosition', updateClassDropdown, 'add-build-class',);

    // Handle form submission
    $('#add-build-form').submit(function (event) {
        event.preventDefault(); // prevent form from submitting

        var userToken = localStorage.getItem('token');
        if (userToken == null) {
            window.location.href = '/';
            toastr.error(`Please log in again`);
            return;
        }

        var name = $('#add-build-name').val();
        var image = $('#add-build-upload')[0].files[0];
        var isPublic = $('#add-build-isPublic-checkbox').is(':checked');
        var classID = $('#add-build-class').val();
        var gameID = $('.add-build-slider-option.active').data('value');
        var tags = $('#add-build-tags').val();
        tags = tags.split(',')
            .map(tag => tag.trim())
            .filter(tag => tag !== ''); // trim & remove empty tags)

        var formData = new FormData();
        formData.append('buildName', name);
        formData.append('isPublic', isPublic);
        formData.append('classID', classID);
        formData.append('gameID', gameID);
        formData.append('tags', tags);

        // Check if a custom image is selected
        if (image) {
            if ((image.size / 1024 / 1024) > 10) { //size in MB
                toastr.error('Max file size is 10MB')
                $('#add-build-upload').val('');
                $('#add-build-thumbnail-preview').attr('src', '/media/question-mark.png');
                return;
            }
            formData.append('image', image);
        } else if (selectedThumbnail) {
            // If a default thumbnail is selected, add it to form data
            formData.append('selectedThumbnail', selectedThumbnail);
        } else {
            toastr.info('Please select or upload a build image.');
            return;
        }

        disableForm($('#add-build-form'));
        $.ajax({
            url: '/Build/CreateBuild',
            headers: {
                Authorization: userToken
            },
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        })
            .done(function (result) {
                // Hide the form and refresh the page to show the new build
                if (!result.success) {
                    toastr.error(result.errorMessage);
                    return;
                }
                hideAddBuildForm();
                localStorage.setItem('buildID', result.message);
                history.replaceState(null, '', `/Builds/${result.message}`);
                populateBuilds();
                populateNote();
                populateReferenceImages();
                populateBuildUrls();
            })
            .fail(function (xhr, status, error) {
                toastr.error('Failed to create build');
                console.log(`Error: ${error}`);

            })
            .always(function () {
                enableForm($('#add-build-form'));

            });
    });

    // Toggle the thumbnail grid when the dropdown button is clicked
    $('#add-build-toggle-thumbnails').click(function () {
        $('#add-build-thumbnail-grid').toggle();
        $('#add-build-content').toggleClass('popup-expanded');
        toggleThumbnailButtonText($('#add-build-toggle-thumbnails'), $('#add-build-content'));
    });

    // Handle thumbnail selection
    $('#add-build-thumbnail-grid img').click(function () {
        // Remove 'selected' class from any previously selected thumbnail
        $('#add-build-thumbnail-grid img').removeClass('selected-thumbnail');

        // Add 'selected' class to the clicked thumbnail
        $(this).addClass('selected-thumbnail');

        // Store the selected thumbnail's filename
        selectedThumbnail = $(this)[0].dataset.filename;

        // Close the grid
        $('#add-build-thumbnail-grid').hide();
        //$('#add-build-toggle-thumbnails').show();

        // Clear the file input if a default thumbnail is chosen
        $('#add-build-upload').val(null);

        // Update the button text to show selected thumbnail
        $('#add-build-thumbnail-preview').attr('src', $(this)[0].src);

        //Shrink the popup and change the text on the button to select
        $('#add-build-content').toggleClass('popup-expanded');
        toggleThumbnailButtonText($('#add-build-toggle-thumbnails'), $('#add-build-content'));
    });

    $('#add-build-upload').change(function (event) {
        var file = event.target.files[0];
        if (file) {
            var reader = new FileReader();
            reader.onload = function (e) {
                $('#add-build-thumbnail-preview').attr('src', e.target.result); // Show the uploaded image as preview
            };
            reader.readAsDataURL(file);
        }
    });
}

function populateBuildForms() {
    return new Promise((resolve, reject) => {
        $.ajax({
            type: 'POST',
            url: '/Build/GetClassesAndTags'
        })
            .done(function (result) {
                if (!result.success) {
                    console.log(result.errorMessage);
                    toastr.error('An error occurred, try again later.');
                    reject(new Error(result.errorMessage));
                    return;
                }
                var addBuildClassDropdown = $('#add-build-class');
                var editBuildClassDropdown = $('#edit-build-class');
                $.each(result.classes, function (index, classItem) {
                    var addOption = $('<option></option>')
                        .attr('value', classItem.classID)
                        .attr('data-game-id', classItem.gameID)
                        .text(classItem.className);

                    var editOption = addOption.clone(true);

                    addBuildClassDropdown.append(addOption);
                    editBuildClassDropdown.append(editOption);
                });
                resolve();
            })
            .fail(function (xhr, status, error) {
                console.log(error);
                toastr.error('An error occurred, try again later.');
                reject(error);
            });
    });
}

function initEditBuildForm() {
    initSlider('edit-build', null, updateClassDropdown, 'edit-build-class',);

    $('#edit-build-form').off('submit').on('submit', function (event) {
        event.preventDefault(); // Prevent form from submitting        

        var userToken = localStorage.getItem('token');
        if (!userToken) {
            window.location.href = '/';
            toastr.error(`Please log in again`);
            return;
        }

        var buildID = $('#edit-build-id').val();
        var name = $('#edit-build-name').val();
        var image = $('#edit-build-image')[0].files[0];
        var classID = $('#edit-build-class').val();
        var gameID = $('.edit-build-slider-option.active').data('value');
        var tags = $('#edit-build-tags').val();
        var isPublic = $('#edit-build-isPublic-checkbox').is(':checked');
        var formData = new FormData();

        formData.append('buildID', buildID);
        formData.append('buildName', name);
        formData.append('isPublic', isPublic);
        formData.append('classID', classID);
        formData.append('gameID', gameID);
        formData.append('tags', tags);


        // Check if a custom image is selected
        if (image) {
            if ((image.size / 1024 / 1024) > 10) { //size in MB
                toastr.error('Max file size is 10MB')
                $('#edit-build-image').val('');
                $('#edit-build-thumbnail-preview').attr('src', '/media/question-mark.png');
                return;
            }
            formData.append('image', image);

        } else if (selectedThumbnail) {
            // If a default thumbnail is selected, add it to form data
            formData.append('selectedThumbnail', selectedThumbnail);
        }

        disableForm($('#edit-build-form'));
        $.ajax({
            url: '/Build/EditBuild',
            headers: {
                Authorization: userToken
            },
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        })
            .done(function (result) {
                if (!result.success) {
                    toastr.error(result.errorMessage);
                    return;
                }
                hideEditBuildForm();
                populateBuilds();
            })
            .fail(function (xhr, status, error) {
                toastr.error('Failed to update build');
                console.log(`Error: ${error}`);
                $('#edit-build-content').find('button[type="submit"]').prop('disabled', false); // Re-enable button on error
            })
            .always(function () {
                enableForm($('#edit-build-form'));
            });
    });

    $('#delete-build-button').off('click').on('click', async function () {
        var userToken = localStorage.getItem('token');
        if (!userToken) {
            window.location.href = '/';
            toastr.error('Please log in again');
            return;
        }
        var buildID = $('#edit-build-id').val();
        const confirmDelete = await showConfirmModal(`Are you sure you want to delete this build?`)


        if (confirmDelete) {
            disableForm($('#edit-build-form'));
            $.ajax({
                url: '/Build/DeleteBuild',
                headers: {
                    Authorization: userToken
                },
                type: 'POST',
                data: { buildID: buildID }
            })
                .done(function (result) {
                    if (!result.success) {
                        toastr.error('Failed to delete build: ' + result.errorMessage);
                        return;
                    }
                    // Hide the form and refresh the page to show the updated build list
                    if (buildID == localStorage.buildID) {
                        localStorage.removeItem('buildID');
                    }
                    hideEditBuildForm();
                    populateBuilds();
                    populateNote();
                    populateReferenceImages();
                    populateBuildUrls();
                })
                .fail(function (xhr, status, error) {
                    // Show an error message if the request failed
                    toastr.error('Failed to delete build');
                    console.log(`Error: ${error}`);
                })
                .always(function () {
                    enableForm($('#edit-build-form'));
                });
        }
    });

    $('#edit-toggle-thumbnails').click(function () {
        $('#edit-build-thumbnail-grid').toggle();
        $('#edit-build-content').toggleClass('popup-expanded');
        toggleThumbnailButtonText($('#edit-toggle-thumbnails'), $('#edit-build-content'));
    });

    // Handle thumbnail selection
    $('#edit-build-thumbnail-grid img').click(function () {
        // Remove 'selected' class from any previously selected thumbnail
        $('#edit-build-thumbnail-grid img').removeClass('selected-thumbnail');

        // Add 'selected' class to the clicked thumbnail
        $(this).addClass('selected-thumbnail');

        // Store the selected thumbnail's filename
        selectedThumbnail = $(this)[0].dataset.filename;

        // Close the grid
        $('#edit-build-thumbnail-grid').hide();

        // Clear the file input if a default thumbnail is chosen
        $('#edit-build-thumbnail-preview').val(null);

        // Update the button text to show selected thumbnail
        $('#edit-build-thumbnail-preview').attr('src', $(this)[0].src);

        $('#edit-build-content').toggleClass('popup-expanded');
        toggleThumbnailButtonText($('#edit-toggle-thumbnails'), $('#edit-build-form-popup'));
    });

    $('#edit-build-image').change(function (event) {
        var file = event.target.files[0];
        if (file) {
            var reader = new FileReader();
            reader.onload = function (e) {
                $('#edit-build-thumbnail-preview').attr('src', e.target.result); // Show the uploaded image as preview
            };
            reader.readAsDataURL(file);
        }
    });
}

function initUploadReferenceImageForm() {
    $('#upload-reference-image-form').submit(function (event) {
        event.preventDefault();

        var userToken = localStorage.getItem('token');
        if (userToken == null) {
            window.location.href = '/';
            toastr.error(`Please log in again`);
            return;
        }
        var image = $('#reference-image-upload')[0].files[0];
        if (image == null) {
            toastr.error('Please select an image to upload');
            return;
        }
        if ((image.size / 1024 / 1024) > 10) { //size in MB
            toastr.error('Max file size is 10MB')
            $('#reference-image-upload').val('');
            $('#upload-reference-image-thumbnail-preview').attr('src', '/media/question-mark.png');
            return;
        }

        var formData = new FormData();
        formData.append('image', image);
        formData.append('buildID', localStorage.buildID);

        disableForm('#upload-reference-image-form');
        $.ajax({
            url: '/BuildData/UploadReferenceImage',
            headers: {
                Authorization: userToken
            },
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        })
            .done(function (result) {
                if (!result.success) {
                    toastr.error(result.errorMessage);
                    return;
                }
                // Hide the form and refresh the page to show the new build
                hideUploadReferenceImageForm();
                populateReferenceImages();
            })
            .fail(function (xhr, status, error) {
                // Show an error message if the request failed
                toastr.error('Failed to upload image');
                console.log(`Error: ${error}`);
            })
            .always(function () {
                enableForm('#upload-reference-image-form');
            });
    });

    $('#reference-image-upload').change(function (event) {
        var file = event.target.files[0];
        if (file) {
            var reader = new FileReader();
            reader.onload = function (e) {
                const img = $('#upload-reference-image-thumbnail-preview');
                img.attr('src', e.target.result); // Show the uploaded image as a preview
            };
            reader.readAsDataURL(file);
        }
    });
}

function initAddUrlForm() {
    // Handle form submission
    $('#add-url-form').submit(function (event) {
        event.preventDefault(); // prevent form from submitting

        var userToken = localStorage.getItem('token');
        if (userToken == null) {
            window.location.href = '/';
            toastr.error(`Please log in again`);
            return;
        }

        var buildUrlName = $('#add-url-name').val();
        var buildUrl = $('#add-url-address').val();
        var buildID = localStorage.getItem('buildID');
        var userToken = localStorage.getItem('token');

        var formData = new FormData();

        formData.append('buildUrlID', null);
        formData.append('buildUrlName', buildUrlName);
        formData.append('buildUrl', buildUrl);
        formData.append('buildID', buildID);

        disableForm('#add-url-form');
        $.ajax({
            url: '/BuildData/CreateBuildUrl',
            headers: {
                Authorization: userToken
            },
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        })
            .done(function (result) {
                // Hide the form and refresh the page to show the new build
                if (!result.success) {
                    toastr.error(result.errorMessage);
                    return;
                }
                returnBuildUrl = result.buildUrl
                hideAddUrlForm();
                saveSelectedUrlForBuild(returnBuildUrl.buildID, returnBuildUrl.buildUrlID)
                populateBuildUrls();
            })
            .fail(function (xhr, status, error) {
                toastr.error('Failed to add URL');
                console.log(`Error: ${error}`);

            })
            .always(function () {
                enableForm('#add-url-form');
            });
    });
}

function initEditUrlForm() {
    $('#edit-url-form').submit(function (event) {
        event.preventDefault(); // prevent form from submitting

        var userToken = localStorage.getItem('token');
        if (userToken == null) {
            window.location.href = '/';
            toastr.error(`Please log in again`);
            return;
        }

        var buildUrlID = $('#hidden-build-url-id').val();
        var buildUrlName = $('#edit-url-name').val();
        var buildUrl = $('#edit-url-address').val();
        var buildID = localStorage.getItem('buildID');
        var userToken = localStorage.getItem('token');

        var formData = new FormData();

        formData.append('buildUrlID', buildUrlID);
        formData.append('buildUrlName', buildUrlName);
        formData.append('buildUrl', buildUrl);
        formData.append('buildID', buildID);

        disableForm('#edit-url-form');
        $.ajax({
            url: '/BuildData/UpdateBuildUrl',
            headers: {
                Authorization: userToken
            },
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        })
            .done(function (result) {
                // Hide the form and refresh the page to show the new build
                if (!result.success) {
                    toastr.error(result.errorMessage);
                    return;
                }
                returnBuildUrl = result.buildUrl
                hideEditUrlForm();
                saveSelectedUrlForBuild(returnBuildUrl.buildID, returnBuildUrl.buildUrlID)
                populateBuildUrls();
            })
            .fail(function (xhr, status, error) {
                toastr.error('Failed to edit URL');
                console.log(`Error: ${error}`);

            })
            .always(function () {
                enableForm('#edit-url-form');
            });
    });

    $('#delete-url-button').off('click').on('click', async function () {

        var userToken = localStorage.getItem('token');
        if (!userToken) {
            window.location.href = '/';
            toastr.error('Please log in again');
            return;
        }

        var buildUrlID = $('#hidden-build-url-id').val();
        const confirmDelete = await showConfirmModal(`Are you sure you want to delete this URL?`)

        if (confirmDelete) {
            disableForm('#edit-url-form');
            $.ajax({
                url: '/BuildData/DeleteBuildUrl',
                headers: {
                    Authorization: userToken
                },
                type: 'POST',
                data: { buildUrlID: buildUrlID }
            })
                .done(function (result) {
                    if (!result.success) {
                        toastr.error('Failed to delete URL: ' + result.errorMessage);
                        return;
                    }
                    hideEditUrlForm();
                    populateBuildUrls();
                })
                .fail(function (xhr, status, error) {
                    // Show an error message if the request failed
                    toastr.error('Failed to delete URL');
                    console.log(`Error: ${error}`);
                })
                .always(function () {
                    enableForm('#edit-url-form');
                });
        }
    });
}

function initNotesButtons() {

    // Get the buttons and build info section
    var editButton = $('#notes-edit-button');
    var cancelButton = $('#notes-cancel-button');
    var saveButton = $('#notes-save-button');
    var notes = $('#notes');
    var noteContent;

    // Edit button click event
    editButton.on('click', function () {
        // Hide the edit button, show the cancel and save buttons
        editButton.hide();
        cancelButton.show();
        saveButton.show();

        noteContent = notes[0].innerText;
        // Enable editing of the notes section
        notes.attr('contenteditable', 'true');
        notes.focus();
    });

    // Cancel button click event
    cancelButton.on('click', function () {
        // Show the edit button, hide the cancel and save buttons
        editButton.show();
        cancelButton.hide();
        saveButton.hide();

        // Disable editing of the notes section and reset its content
        notes.attr('contenteditable', 'false');
        notes[0].innerText = noteContent;
    });

    // Save button click event
    saveButton.on('click', function () {
        saveButton.prop('disabled', true);

        var userToken = localStorage.getItem('token');
        if (userToken == null) {
            window.location.href = '/';
            return;
        }

        $.ajax({
            type: 'POST',
            url: '/BuildData/SetNote',
            headers: {
                Authorization: userToken
            },
            data: {
                buildID: localStorage.buildID,
                noteContent: notes[0].innerText //.text() doesnt preserve new line chars .html() throws security error
            }
        })
            .done(function (result) {
                if (!result.success) {
                    toastr.error(result.errorMessage);
                    return;
                }
                editButton.show();
                cancelButton.hide();
                saveButton.hide();
                notes.attr('contenteditable', 'false');
            })
            .fail(function (xhr, status, error) {
                toastr.error('An error occurred while saving the note');
                console.log(`Error: ${error}`);
            })
            .always(function () {
                saveButton.prop('disabled', false);
            });
    });
}

function toggleThumbnailButtonText(thumbnailButton, popup) {
    if ($(popup).hasClass('popup-expanded')) {
        $(thumbnailButton).text('Hide');
    }
    else {
        $(thumbnailButton).text('Select');
    }
}

function showAddBuildForm() {
    $('body').addClass('noscroll');
    $('#header-menu').hide();
    resetAddBuildForm();
    $('#add-build-popup').show();
    $('#add-build-name').focus();
}

function hideAddBuildForm() {
    $('#add-build-popup').hide();
    $('body').removeClass('noscroll');
}

function resetAddBuildForm() {
    selectedThumbnail = null;
    $('#add-build-form')[0].reset();
    $('#add-build-toggle-thumbnails').text('Select');
    $('#add-build-thumbnail-grid').hide();
    $('#add-build-content').removeClass('popup-expanded');
    $('#add-build-thumbnail-preview').attr('src', '/media/question-mark.png');
}

async function showEditBuildForm(buildID, imageID, filePath, buildName, isPublic, gameID, classID, tags) {
    $('body').addClass('noscroll');
    $('#header-menu').hide();
    resetEditBuildForm();
    populateImageCache([{ imageID: imageID, filePath: filePath }])
    let cachedFilePath = await getCachedImageUrl(filePath) || '/media/question-mark.png';
    $('#edit-build-thumbnail-preview').attr('src', cachedFilePath);
    $('#edit-build-id').val(buildID);
    $('#edit-build-name').val(buildName);
    var sliderSelection = $('.edit-build-slider-option.active').data('value')
    if (gameID != sliderSelection)
        $('#edit-build-slider').click();
    $('#edit-build-class').val(classID);
    $('#edit-build-tags').val(tags);
    $('#edit-build-isPublic-checkbox').prop('checked', isPublic);    
    $('#edit-build-popup').show();
    $('#edit-build-name').focus();
}

function hideEditBuildForm() {
    $('#edit-build-popup').hide();
    $('body').removeClass('noscroll');
}

function resetEditBuildForm() {
    selectedThumbnail = null;
    $('#edit-build-form')[0].reset();
    $('#edit-build-thumbnail-grid').hide();
    $('#edit-toggle-thumbnails').text('Select');
    $('#edit-build-thumbnail-grid').hide();
    $('#edit-build-content').removeClass('popup-expanded');
    $('#edit-build-content').find('button[type="submit"]').prop('disabled', false)
}

function showAddUrlForm() {
    $('#add-url-form')[0].reset();
    $('body').addClass('noscroll');
    $('#add-url-popup').show();
    $('#add-url-name').focus();
}

function hideAddUrlForm() {
    $('#add-url-popup').hide();
    $('body').removeClass('noscroll');
}

function showEditUrlForm(urlID, urlName, urlAddress) {
    $('#edit-url-form')[0].reset();
    $('body').addClass('noscroll');
    $('#hidden-build-url-id').val(urlID);
    $('#edit-url-name').val(urlName);
    $('#edit-url-address').val(urlAddress);
    $('#edit-url-popup').show();
    $('#edit-url-name').focus();
}

function hideEditUrlForm() {
    $('#edit-url-popup').hide();
    $('body').removeClass('noscroll');
}

function showUploadReferenceImageForm() {
    $('#reference-image-upload').val('');
    $('body').addClass('noscroll');
    resetUploadReferenceImageForm();
    $('#upload-reference-image-popup').show();
}

function hideUploadReferenceImageForm() {
    $('#upload-reference-image-popup').hide();
    $('body').removeClass('noscroll');
}

function resetUploadReferenceImageForm() {
    $('#upload-reference-image-form')[0].reset();
    $('#upload-reference-image-thumbnail-preview').attr('src', '/media/question-mark.png');
}

function saveImageOrder(newOrder) {
    var userToken = localStorage.getItem('token');

    $.ajax({
        type: 'POST',
        url: '/BuildData/SaveImageOrder',
        headers: {
            Authorization: userToken
        },
        contentType: 'application/json',
        data: JSON.stringify({
            newOrder: newOrder,
            buildID: localStorage.buildID
        })
    })
        .fail(function (xhr, status, error) {
            console.log(`Error: ${error}`);
        });
}

function deleteReferenceImage(imageID) {
    var userToken = localStorage.getItem('token');
    $.ajax({
        type: 'POST',
        url: '/BuildData/DeleteReferenceImage', // Adjust the URL as per your actual route
        headers: {
            Authorization: userToken // Include the user token if needed
        },
        data: {
            imageID: imageID
        }
    })
        .done(function (result) {
            if (!result.success) {
                toastr.error(result.errorMessage);
            }
            populateReferenceImages();
        })
        .fail(function (xhr, status, error) {
            toastr.error('An error occurred while deleting the image');
            console.log(`Error: ${error}`);
        });
}

async function checkEditing() {
    if ($('#notes').attr('contenteditable') == 'true' || $('#txt-url-url').prop('readonly') == false) {
        if (await showConfirmModal(`You have unsaved changes, are you sure you wish to continue?`)) {
            $('#url-cancel-button').trigger('click');
            $('#notes-cancel-button').trigger('click');
            return true;
        }
        return false;
    }
    return true;
}
// Show the modal and return a Promise that resolves on confirmation
function showConfirmModal(message) {
    return new Promise((resolve, reject) => {
        const modal = $('#confirm-modal');
        const messageElement = $('#confirm-modal-message');
        const confirmButton = $('#confirm-modal-confirm');
        const cancelButton = $('#confirm-modal-cancel');

        messageElement.text(message);
        modal.css('display', 'flex');

        confirmButton.on('click', function () {
            modal.css('display', 'none');
            resolve(true); // Resolve promise with true
        });

        // Cancel action
        cancelButton.on('click', function () {
            modal.css('display', 'none');
            resolve(false); // Resolve promise with false
        });
    });
}
function submitActivePopup() {
    if ($('#add-build-popup').is(':visible')) {
        $('#add-build-submit').trigger('click');
    }
    else if ($('#edit-build-popup').is(':visible')) {
        $('#edit-build-submit').trigger('click');
    }
    else if ($('#add-url-popup').is(':visible')) {
        $('#add-url-submit').trigger('click');
    }
    else if ($('#edit-url-popup').is(':visible')) {
        $('#edit-url-submit').trigger('click');
    }
    else if ($('#upload-reference-image-popup').is(':visible')) {
        $('#upload-reference-image-submit').trigger('click');
    }
}
function closeActivePopup() {
    if ($('#confirmModal').is(':visible')) {
        $('#confirmModal-cancel').trigger('click');
    }
    else if ($('#add-build-popup').is(':visible')) {
        $('#add-build-cancel').trigger('click');
    }
    else if ($('#edit-build-popup').is(':visible')) {
        $('#edit-build-cancel').trigger('click');
    }
    else if ($('#add-url-popup').is(':visible')) {
        $('#add-url-cancel').trigger('click');
    }
    else if ($('#edit-url-popup').is(':visible')) {
        $('#edit-url-cancel').trigger('click');
    }
    else if ($('#upload-reference-image-popup').is(':visible')) {
        $('#upload-reference-image-cancel').trigger('click');
    }
    else if ($('.imageModal').is(':visible')) {
        $('.imageModal').remove();
        $('body').removeClass('noscroll');
    }
}

function initContextMenu() {
    let contextMenuBuildID = null;
    let contextIsPublic = false;

    $(document).on('contextmenu', '.build-item', function (e) {
        if (!$(this).data('buildID')) {
            // If no buildID exists, it's a utility button, so don't show the context menu
            return;
        }

        e.preventDefault();  // Prevent the default right-click menu

        contextMenuBuildID = $(this).data('buildID');
        contextIsPublic = $(this).data('isPublic');

        // Show or hide "Copy Public URL" based on build's public status
        if (contextIsPublic) {
            $('#context-copy-public-url').show();
        } else {
            $('#context-copy-public-url').hide();
        }

        // Position and show the context menu
        $('#build-context-menu').css({
            top: e.pageY + 'px',
            left: e.pageX + 'px'
        }).show();
    });
    // Hide context menu on click elsewhere
    $(document).on('click', function () {
        $('#build-context-menu').hide();
    });

    // Prevent context menu inside the custom menu
    $('#build-context-menu').on('contextmenu', function (e) {
        e.preventDefault();
    });

    // Edit Build option
    $('#context-edit-build').on('click', function () {
        $('#build-context-menu').hide();
        $(`.build-item`).filter(function () {
            return $(this).data('buildID') === contextMenuBuildID;
        }).find('.editImage').trigger('click');
    });

    // Copy Editable URL option
    $('#context-copy-build-url').on('click', function () {
        $('#build-context-menu').hide();
        const buildUrl = `${window.location.origin}/Builds/${contextMenuBuildID}`;
        navigator.clipboard.writeText(buildUrl);
    });

    // Copy Public URL option
    $('#context-copy-public-url').on('click', function () {
        $('#build-context-menu').hide();
        const publicUrl = `${window.location.origin}/Public/${localStorage.username}/${contextMenuBuildID}`;
        navigator.clipboard.writeText(publicUrl);
    });

}

//function positionAutocompleteBox(tagInput, autocompleteBox) {

//    tagInput = tagInput[0]; // Get the DOM element from jQuery object
//    autocompleteBox = autocompleteBox[0];

//    autocompleteBox.style.position = "fixed";
//    autocompleteBox.style.top = tagInput.offsetTop + tagInput.offsetHeight + "px";
//    autocompleteBox.style.left = tagInput.offsetLeft + "px";
//    autocompleteBox.style.width = tagInput.clientWidth + "px";
//}

async function verifyToken() {
    var loggedIn = await validateUserToken();
    if (!loggedIn) {
        window.location.href = '/';
        toastr.error(`Please log in again`);
    }
    document.title = `${localStorage.username}'s Builds`;
}


$(document).ready(async function () {
    verifyToken();
    checkMobile();
    await populateBuildForms();
    initAddBuildForm();
    initEditBuildForm();
    initUploadReferenceImageForm();
    initNotesButtons();
    initAddUrlForm();
    initEditUrlForm();
    initSidebarToggle();
    initFilter();
    populateBuilds();
    initDropdownAdjust();
    initContextMenu();

});


