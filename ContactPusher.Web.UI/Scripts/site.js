var cp = cp || {};

cp.common = (function () {
	var ajaxTimers = {};
	var ajaxRequests = {};

	function initialize() {
		$("#AjaxStatus").bind("ajaxSend", function () {
			$(this).text('Saving...');
			$(this).show();
		}).bind("ajaxComplete", function () {
			$(this).hide();
		});
	}

	function makeAjaxRequest(name, delay, func) {
		if (ajaxRequests[name]) {
			ajaxRequests[name].abort();
			ajaxRequests[name] = null;
		}

		clearTimeout(ajaxTimers[name]);

		ajaxTimers[name] = setTimeout(function () {
			ajaxRequests[name] = func();
		}, delay); // delay, tweak for faster/slower
	}

	return {
		initialize: initialize,
		ajaxTimers: ajaxTimers,
		ajaxRequests: ajaxRequests,
		makeAjaxRequest: makeAjaxRequest
	};
}());