function populateNote() {
    if (!localStorage.buildID) {
        return;
    }

    var userToken = localStorage.getItem("token");

    $.ajax({
        type: 'POST',
        url: '/BuildData/GetNote',
        headers: {
            Authorization: userToken
        },
        data: {
            buildID: localStorage.buildID
        },
    })
        .done(function (result) {
            if (!result.success || !result.noteFileUrl.success) {
                console.error(`Error fetching note: ${result.errorMessage}`);
                return;
            }
            var noteFileUrl = result.noteFileUrl.url;
            // Fetch the note contents using the pre-signed URL
            $.ajax({
                type: 'GET',
                url: noteFileUrl,
                success: function (fileContents) {
                    document.getElementById("notes").innerText = fileContents; 
                    
                },
                error: function (xhr, status, error) {
                    toastr.error('Error fetching note contents');
                    console.log(`Error: ${error}`);
                }
            });
        })
        .fail(function (xhr, status, error) {
            toastr.error('Error fetching note');
            console.log(`Error: ${error}`);
        });
}

function populateBuildUrls() {
    if (!localStorage.buildID) {
        return;
    }

    var userToken = localStorage.getItem("token");

    $.ajax({
        type: 'POST',
        url: '/BuildData/GetBuildUrls',
        headers: {
            Authorization: userToken
        },
        data: {
            buildID: localStorage.buildID
        }
    })
        .done(function (result) {
            if (!result.success) {
                toastr.error(result.errorMessage);
                return;
            }

            var editable = !window.location.pathname.includes('/Public');
            var dropdownContent = $('.dropdown-content');
            dropdownContent.empty(); // Clear any existing options

            // Add the "Create New" option at the top
            if (editable) {
                var createNewOption = $('<div>')
                    .addClass('dropdown-item create-new')
                    .text('Create New URL')
                    .data({
                        'url': '',
                        'name': '',
                        'id': null
                    })
                    .on('click', function () {
                        showAddUrlForm();
                        if (checkMobile()) {
                            $('.dropdown-content').hide();
                            $('#dropdown-toggle').css("background-color", "black");
                        }
                    });
                dropdownContent.append(createNewOption);
            }

            //get the previously selected URL
            var selectedUrl = getSelectedUrlForBuild(localStorage.buildID);
            var foundUrl = false;
            setUrlDropdown(null, 'Select URL', '');
            // Populate the custom dropdown items
            result.buildUrls.forEach(function (buildUrl) {
                var option = $('<div>')
                    .addClass('dropdown-item')
                    .data({
                        'url': buildUrl.buildUrl,
                        'name': buildUrl.buildUrlName,
                        'id': buildUrl.buildUrlID
                    })
                    .on('click', function () {
                        setUrlDropdown(buildUrl.buildUrlID, buildUrl.buildUrlName, buildUrl.buildUrl)
                        if (checkMobile()) {
                            $('.dropdown-content').hide();
                            $('#dropdown-toggle').css("background-color", "black");
                        }
                    });

                var textContainer = $('<span>').addClass('dropdown-text').text(buildUrl.buildUrlName);
                var iconsContainer = $('<span>').addClass('dropdown-icon-container');

                // Add edit and delete icons within the dropdown item
                var openIcon = $('<img>')
                    .addClass('dropdown-icon')
                    .attr('src', '/media/open.png')
                    .attr('alt', 'Open')
                    .on('click', function (e) {
                        e.stopPropagation(); // Prevents triggering other click events, if any
                        window.open(buildUrl.buildUrl, '_blank');
                    });

                if (editable) {
                    var editIcon = $('<img>')
                        .addClass('dropdown-icon')
                        .attr('src', '/media/edit.png')
                        .attr('alt', 'Edit')
                        .on('click', function (e) {
                            e.stopPropagation(); // Prevents triggering other click events, if any
                            showEditUrlForm(buildUrl.buildUrlID, buildUrl.buildUrlName, buildUrl.buildUrl);
                            if (checkMobile()) {
                                $('.dropdown-content').hide();
                                $('#dropdown-toggle').css("background-color", "black");
                            }
                        });

                    iconsContainer.append(editIcon, openIcon);
                }
                else {
                    iconsContainer.append(openIcon);
                }
                option.append(textContainer, iconsContainer)
                dropdownContent.append(option);

                if (!foundUrl && selectedUrl == buildUrl.buildUrlID) {
                    setUrlDropdown(buildUrl.buildUrlID, buildUrl.buildUrlName, buildUrl.buildUrl);
                    foundUrl = true;
                }
            });

        })
        .fail(function (xhr, status, error) {
            toastr.error('Error fetching build URLs');
            console.log(`Error: ${error}`);
        });
}

function setUrlDropdown(buildUrlID, buildUrlName, buildUrl) {
    var svgArrow = '<svg class="svg-arrow" viewBox="0 0 10 5" xmlns="http://www.w3.org/2000/svg">' +
        '<path d="M0 0 L5 5 L10 0 Z" fill="white" />';
    $('#dropdown-toggle').html(buildUrlName + svgArrow);
    //$('#dropdown-toggle').text(buildUrlName);
    setTimeout(() => {
        $('#iframe-window').attr('src', buildUrl);
    }, 50);
    saveSelectedUrlForBuild(localStorage.buildID, buildUrlID)
}

// Save selected URL for a build
function saveSelectedUrlForBuild(buildID, buildUrlID) {
    // Get existing selections from local storage or create a new object
    var buildSelections = JSON.parse(localStorage.getItem("buildSelections")) || {};

    // Update the selected URL for the specified build ID
    buildSelections[buildID] = buildUrlID;

    // Save back to local storage
    localStorage.setItem("buildSelections", JSON.stringify(buildSelections));
}

// Load selected URL for a build
function getSelectedUrlForBuild(buildID) {
    var buildSelections = JSON.parse(localStorage.getItem("buildSelections")) || {};
    return buildSelections[buildID]; // returns undefined if no selection
}

function populateReferenceImages() {
    if (localStorage.buildID == null) {
        return;
    }

    var userToken = localStorage.getItem("token");

    $.ajax({
        type: 'POST',
        url: '/BuildData/GetReferenceImages',
        headers: {
            Authorization: userToken
        },
        data: {
            buildID: localStorage.buildID,
        },
    })
        .done(async function (result) {
            if (!result.success) {
                toastr.error(result.errorMessage);
                return;
            }
            // Get the list container element
            var list = $('#reference-image-list');
            list.empty();

            // Sort the images by their order
            result.images.sort(function (a, b) {
                return a.imageOrder - b.imageOrder;
            });

            await populateImageCache(result.images);

            // Iterate through the rows returned by the database
            for (const record of result.images) {
                var li = $('<li>').addClass('build-item')
                    .data('data-imageID', record.imageID)
                    .data('data-imageOrder', record.imageOrder);

                // Create a wrapper div for the image
                var wrapperDiv = $('<div>').addClass('image-wrapper');

                // Create an <img> element for the image
                let cachedFilePath = await getCachedImageUrl(record.filePath) || '/media/question-mark.png';
                var img = $('<img>').attr('src', cachedFilePath).addClass('referenceImage');
                var expandImage = $('<img>').addClass('expand-button').data('imageElement', img);

                wrapperDiv.append(img);
                if (window.location.pathname.includes('/Public')) {
                    expandImage.addClass('public');
                }
                else {
                    var deleteImage = $('<img>').addClass('delete-button');
                    // Add click event for the delete button
                    deleteImage.on('click', async function (event) {
                        event.stopPropagation();
                        const confirmDelete = await showConfirmModal(`Are you sure you want to delete this image?`);
                        if (confirmDelete) {
                            var imageID = record.imageID;
                            deleteReferenceImage(imageID);
                        }
                    });
                    wrapperDiv.append(deleteImage);
                }

                // Add click event for the fullscreen button
                expandImage.on('click', function (event) {
                    event.stopPropagation();
                    showImageModal(record.imageID, record.filePath);
                });

                img.on('dblclick', function (event) {
                    event.stopPropagation();
                    showImageModal(record.imageID, record.filePath);
                    //showImageModal($(this).attr('src'));
                });

                wrapperDiv.append(expandImage);
                li.append(wrapperDiv);
                list.append(li);
            };

            // Make the images sortable if not mobile
            if (!checkMobile()) {
                list.sortable({
                    update: function (event, ui) {
                        // Get the new order of the images
                        var newOrder = list.children().map(function (index, element) {
                            return {
                                imageID: $(element).data('data-imageID'),
                                imageOrder: index
                            };
                        }).get();

                        // Save the new order
                        saveImageOrder(newOrder);
                    }
                });
            }
        })
        .fail(function (xhr, status, error) {
            toastr.error('An error occurred while fetching reference images');
            console.log(`Error: ${error}`);
        });
}

async function showImageModal(imageID, filePath) {
    // Create modal elements
    var modal = $('<div>').addClass('imageModal');
    populateImageCache([{ imageID: imageID, filePath: filePath }])
    let cachedFilePath = await getCachedImageUrl(filePath) || '/media/question-mark.png';
    var modalContent = $('<img>').attr('src', cachedFilePath).addClass('imageModal-content');
    var closeModal = $('<span>').addClass('close-imageModal').html('&times;');

    $('body').addClass('noscroll');

    // Append elements to the modal
    modal.append(modalContent, closeModal);

    // Append modal to body
    $('body').append(modal);

    // Close modal when clicking on the close button
    closeModal.on('click', function () {
        modal.remove();
        $('body').removeClass('noscroll');
    });

    // Close modal when clicking outside the modal content
    $(window).on('click', function (event) {
        if ($(event.target).is(modal)) {
            modal.remove();
            $('body').removeClass('noscroll');
        }
    });
}

function adjustDropdownPosition() {
    const dropdownToggle = $('#dropdown-toggle');
    const dropdownContent = $('.dropdown-content');

    dropdownContent.css({
        width: `${dropdownToggle.outerWidth()}px`,
        //left: `${dropdownToggle.offset().left}px`
    });
}

function initDropdownAdjust() {
    const dropdownToggle = $('#dropdown-toggle');
    const dropdownContent = $('.dropdown-content');

    if (checkMobile()) {
        $('#dropdown-toggle').on('click', function (event) {
            var dropdownContent = $('.dropdown-content');
            if (dropdownContent.is(':visible')) {
                dropdownContent.hide();
                $('#dropdown-toggle').css("background-color", "black");
            } else {
                dropdownContent.show();
            }
            event.stopPropagation(); // Prevent event from bubbling up
        });
    }

    function adjustDropdownPosition() {
        const toggleRect = dropdownToggle[0].getBoundingClientRect();

        dropdownContent.css({
            position: 'absolute',  // Keeps it positioned relative to the viewport
            width: `${toggleRect.width}px`,
            //top: `${toggleRect.bottom}px`,
            //left: `${toggleRect.left}px`,
            zIndex: 1000  // Ensure it's above other elements
        });
    }

    // Adjust position on load, window resize, and scroll
    adjustDropdownPosition();
    $(window).on('resize scroll', adjustDropdownPosition);
}

function initSidebarToggle() {
    $('#sidebar-toggle').click(function () {
        const currentlyCollapsed = $('#sidebar').hasClass('collapsed');

        $('#sidebar').toggleClass('collapsed', !currentlyCollapsed);
        $('#content').toggleClass('collapsed', !currentlyCollapsed);
        $('#notes-column').toggleClass('collapsed', !currentlyCollapsed);
        $('#build-filter').toggleClass('hidden', !currentlyCollapsed);
        $('.build-item').toggleClass('collapsed', !currentlyCollapsed);
        $('.sidebar-content').toggleClass('collapsed', !currentlyCollapsed);    

        localStorage.setItem('sidebarCollapsed', !isCollapsed);
        adjustDropdownPosition();
    });

    const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true'; // Default is expanded
    if (isCollapsed) {
        $('#sidebar').addClass('collapsed');
        $('#content').addClass('collapsed');
        $('#notes-column').addClass('collapsed');
        $('#build-filter').addClass('hidden');
        $('.build-item').addClass('collapsed');
        $('.sidebar-content').addClass('collapsed');
    }
    $('#sidebar').removeClass('hidden');
}



function initFilter() {

    $('#filter-builds-li').css('display', 'flex');
    $('#build-filter, #header-menu-build-filter').on('input', function () {
        filterBuilds($(this).val());
    });
}

function filterBuilds(filterText) {
    $('#build-filter, #header-menu-build-filter').val(filterText);

    var filterTextLower = filterText.toLowerCase();  

    const applyFilter = function () {
        const listItem = $(this);
        const buildId = listItem.data('buildID');

        // Skip items without buildID (like create/copy buttons)
        if (buildId === undefined) {
            return;
        }

        // Show/hide based on text content
        listItem.toggle(listItem.text().toLowerCase().includes(filterTextLower));
    };

    $('#sidebar-build-list li').each(applyFilter);
    $('#header-menu-list li.build-item').each(applyFilter);
}

