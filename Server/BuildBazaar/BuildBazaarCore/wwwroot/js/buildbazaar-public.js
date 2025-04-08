async function populatePublicBuilds() {
    var userToken = localStorage.getItem('token');
    userName = window.location.pathname.split('/')[2];  // Get the username from the URL path

    $.ajax({
        type: 'POST',
        url: '/Build/GetPublicBuilds',
        headers: {
            Authorization: userToken
        },
        data: { userName: userName }
    })
        .done(async function (result) {
            if (!result.success) {
                toastr.error(result.errorMessage);
                return;
            }
            userName = result.userName;
            document.title = `${userName}'s Builds`;

            var list = $('#sidebar-build-list');
            var headerMenuList = $('#header-menu-list');
            var foundBuild = false;
            var currentBuildID = window.location.pathname.split('/')[3] || localStorage.buildID;

            list.empty();
            headerMenuList.find('li.build-item').remove();

            var loggedIn = await validateUserToken();
            if (loggedIn) {
                let li = $('<li>').addClass('build-item');
                let copyOnClick = async function () {
                    //Create Copy of build function
                    var copyButton = $(this);
                    copyButton.off('click');
                    copyButton.find('h4').text('Copying...');

                    $.ajax({
                        type: 'POST',
                        url: '/Build/CopyBuild',
                        headers: {
                            Authorization: userToken
                        },
                        data: { originalBuildID: localStorage.buildID }
                    })
                        .done(function (result) {
                            // Hide the form and refresh the page to show the new build
                            if (!result.success) {
                                toastr.error(result.errorMessage);
                                return;
                            }
                            toastr.success('A copy of this build has been added to your collection');
                        })
                        .fail(function (xhr, status, error) {
                            toastr.error('Failed to copy build');
                            console.log(`Error: ${error}`);

                        })
                        .always(function () {
                            copyButton.on('click', copyOnClick);
                            copyButton.find('h4').text('Copy Build');
                        });
                };
                li.on('click', copyOnClick);
                let buildImg = $('<img>').attr('src', '/media/add-thumbnail.png').addClass('thumbnail');
                let buildName = $('<h4>').text('Copy Build');
                li.append(buildImg).append(buildName);
                list.append(li);
                let headerLi = li.clone(true);
                headerMenuList.append(headerLi);
            }

            await populateImageCache(result.builds);

            // Iterate through the rows returned by the database
            for (const record of result.builds) {
                let li = $('<li>').addClass('build-item');
                li.data('buildID', record.buildID);
                if (currentBuildID == record.buildID) {
                    foundBuild = true;
                    li.addClass('selected-build');
                    localStorage.setItem('buildID', currentBuildID);
                    history.replaceState(null, '', `/Public/${result.userName}/${currentBuildID}`);
                }
                li.on('click', async function () {
                    var buildID = $(this).data('buildID');
                    localStorage.setItem('buildID', buildID);
                    history.replaceState(null, '', `/Public/${result.userName}/${buildID}`);
                    $('#sidebar-build-list li').removeClass('selected-build');
                    $(this).addClass('selected-build');
                    populateNote(true);
                    populateReferenceImages(true);
                    populateBuildUrls(true);
                });

                let cachedFilePath = await getCachedImageUrl(record.filePath) || '/media/question-mark.png';
                let buildImg = $('<img>').attr('src', cachedFilePath).addClass('thumbnail');
                let buildName = $('<h4>').text(record.buildName);
                let editImg = $('<img>').attr('src', '/media/edit.png').addClass('editImage').on('click', async function (e) {
                    e.stopPropagation();
                    if (await checkEditing()) {
                        showEditBuildForm(record.buildID, record.filePath, record.buildName, record.isPublic)
                    }
                });

                li.append(buildImg).append(buildName);
                list.append(li);
                let headerLi = li.clone(true);
                headerMenuList.append(headerLi);
            };

            if (result.builds.length < 1 && !foundBuild) {
                localStorage.removeItem('buildID');
                $('#main-row').addClass('hidden-children')
                $('#sidebar').show();
            } else if (!foundBuild) {
                var firstBuild = 0;
                if (loggedIn) {
                    firstBuild = 1;
                }
                var secondLi = $('#sidebar li').eq(firstBuild);
                var buildID = secondLi.data('buildID');
                localStorage.setItem('buildID', buildID);
                history.replaceState(null, '', `/Public/${result.userName}/${buildID}`);
                secondLi.addClass('selected-build');
            }
            else {
                $('#main-row').removeClass('hidden-children');
            }
            populateNote(true);
            populateReferenceImages(true);
            populateBuildUrls(true);
        })
        .fail(function (xhr, status, error) {
            toastr.error('Failed fetching builds');
            console.log(`Error: ${error}`);
        });
}

function submitActivePopup() {
    //placeholder
}

function closeActivePopup() {
    if ($('#confirmModal').is(':visible')) {
        $('#confirmModal-cancel').trigger('click');
    }
    else if ($('.imageModal').is(':visible')) {
        $('.imageModal').remove();
        $('body').removeClass('noscroll');
    }
}

function initContextMenu() {
    let contextMenuBuildID = null;

    $(document).on('contextmenu', '.build-item', function (e) {
        if (!$(this).data('buildID')) {
            // If no buildID exists, it's a utility button, so don't show the context menu
            return;
        }

        e.preventDefault();  // Prevent the default right-click menu

        contextMenuBuildID = $(this).data('buildID');

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

    // Copy Public URL option
    $('#context-copy-public-url').on('click', function () {
        $('#build-context-menu').hide();
        const publicUrl = `${window.location.origin}/Public/${userName}/${contextMenuBuildID}`;
        navigator.clipboard.writeText(publicUrl);
    });
    return
}

let userName = "";

$(document).ready(function () {
    initToastrSettings();
    checkMobile();
    initSidebarToggle();
    initHeaderSlider();
    initFilter();
    initContextMenu();
    populatePublicBuilds();
    initDropdownAdjust();

});