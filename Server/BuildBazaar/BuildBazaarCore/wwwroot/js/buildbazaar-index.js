
function initPasswordResetForm() {
	$('#password-reset-popup').submit(function(event) {
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
			.done(function(result) {
				if (!result.success) {
					toastr.error(`Error: ${result.errorMessage}`);
					return;
				};
				toastr.success(result.message);
				hidePasswordResetForm();
			})
			.fail(function(xhr, status, error) {
				console.log(error);
				toastr.error('An error occurred, try again later.');
			})
			.always(function() {
				enableForm($('#password-reset-popup'));
			});
	});
}

function initNewPasswordForm() {
	$('#new-password-popup').submit(function(event) {
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
			.done(function(result) {
				if (!result.success) {
					toastr.error(`Error: ${result.errorMessage}`);
					return;
				};
				toastr.success(result.message);
				hideNewPasswordForm();
			})
			.fail(function(xhr, status, error) {
				console.log(error);
				toastr.error('An error occurred, try again later.');
			})
			.always(function() {
				enableForm($('#new-password-popup'));
			});
	});
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
	$('#search-buildname').focus();
}

async function initSearchForm() {
	$("#search-form").on("submit", function(e) {
		e.preventDefault();
		fetchSearchResults(true);
	});

	$("#search-reset").on("click", function() {
		$("#search-form")[0].reset();
		//fetchSearchResults(true);
	});

	// Sort dropdown
	$("#search-sort").on("change", function() {
		sortBy = $(this).val() === "0" ? "newest" : "oldest";
		//fetchSearchResults(true);
	});

	await populateSearchForm();
	initSlider('search', 'searchSliderPosition', updateClassDropdown, 'search-class');

	// Infinite scroll
	$(".body-content").on("scroll", function() {

		if ($(this).scrollTop() + $(this).height() > $(this)[0].scrollHeight - 100) {
			fetchSearchResults(false);
		}
	});
}

function populateSearchForm() {
	return new Promise((resolve, reject) => {
		$.ajax({
			type: 'POST',
			url: '/Build/GetClassesAndTags',
			data: { lastTagID: 10, lastClassID: 10 }
		})
			.done(function(result) {
				if (!result.success) {
					console.log(result.errorMessage); // Fixed to use result.errorMessage
					toastr.error('An error occurred, try again later.');
					reject(new Error(result.errorMessage));
					return;
				}
				var classDropdown = $('#search-class');
				$.each(result.classes, function(index, classItem) {
					classDropdown.append($('<option></option>')
						.attr('value', classItem.classID)
						.attr('data-game-id', classItem.gameID)
						.text(classItem.className));
				});
				resolve();
			})
			.fail(function(xhr, status, error) {
				console.log(error);
				toastr.error('An error occurred, try again later.');
				reject(error);
			});
	});
}

function fetchSearchResults(reset = false) {
	if (isLoading)
		return;

	isLoading = true;

	if (reset) {
		page = 1;
		$("#search-results").empty();
	}

	var selectedGame = localStorage.getItem('searchSliderPosition');
	selectedGame = selectedGame ? parseInt(selectedGame) + 1 : $(".search-slider-option.active").data("value");

	const formData = new FormData();
	formData.append("page", page);
	formData.append("sortBy", sortBy);
	formData.append("buildName", $("#search-buildname").val());
	formData.append("author", $("#search-author").val());
	formData.append("classId", $("#search-class").val());
	formData.append("gameId", selectedGame);
	formData.append("tags", $("#search-tags").val());

	$.ajax({
		type: "POST",
		url: "/Build/SearchBuilds",
		data: formData,
		processData: false,
		contentType: false
	})
		.done(async function(data) {
			if (!data.success) {
				toastr.error(data.errorMessage || "Failed to load builds.");
				return;
			}

			if (data.builds.length === 0 && page === 1) {
				$("#search-results").html("<div class='no-results'>No builds found matching your criteria.</div>");
				return;
			}

			await populateImageCache(data.builds);

			data.builds.forEach(build => {
				getCachedImageUrl(build.filePath).then(cachedFilePath => {
					if (!cachedFilePath) {
						cachedFilePath = '/media/question-mark.png';
					}

					var img = $('<img>').attr('src', cachedFilePath).addClass('result-thumbnail');

					let div = $(`
                        <div class="result-item" data-build-id="${build.buildID}">
                            <div>${img.prop('outerHTML')}</div>
                            <div class="hide-overflow">${build.buildName}</div>
                            <div class="hide-overflow">${build.userName}</div>
                            <div class="hide-overflow">${build.className || "N/A"}</div>
                            <div class="hide-overflow">${build.tags || "No Tags"}</div>
                        </div>
                    `);
					//<div>${build.gameName || "N/A"}</div>

					// Add click event to navigate to build details
					div.on("click", function() {
						window.location.href = `/Public/${build.userName}/${build.buildID}`;
					});

					$("#search-results").append(div);
				});
			});

			if (data.builds.length > 0) {
				page++;
			}
		})
		.fail(function(xhr, status, error) {
			toastr.error("An error occurred while fetching builds: " + error);
		})
		.always(function() {
			isLoading = false;
		});
}
$(".column-header").click(function() {
	sortBy = $(this).data("sort");
	fetchSearchResults(true);
});

$(window).scroll(function() {
	if ($(window).scrollTop() + $(window).height() >= $(document).height() - 50) {
		fetchSearchResults();
	}
});

let sortBy = "newest";
let page = 1;
let isLoading = false;

$(document).ready(function() {
	parseUrl();
	initPasswordResetForm();
	initNewPasswordForm();
	initSearchForm();
	fetchSearchResults();
});

