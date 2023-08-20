$(() => {
	var filterTemplate = $("#filterTemplate");
	var filterValueTemplate = $("#filterValueTemplate");

	var channelId = $(".channel").data("id");

	var filters = $(".filterList .filterEntry");
	var filterList = $(".filterList");

	filters.each(i => {
		InitFilter(filters.eq(i));
	});

	$(".deleteBtn").on("click", () => {
		$.ajax({
			url: `/api/config/channel/${channelId}`,
			method: "DELETE"
		}).done(() => {
			window.location = "/config";
		});
	});

	$("#addFilter").on("click", () => {
		var elem = $(filterTemplate.html()).hide();
		filterList.append(elem);
		elem.slideDown(250);
		InitFilter(elem);
	});

	function InitFilter(filter) {
		var addBtn = filter.find(".addBtn");
		addBtn.on("click", () => {
			var elem = $(filterValueTemplate.html()).hide();
			addBtn.before(elem);
			elem.find(".removeBtn").on("click", () => {
				elem.slideUp(250, () => elem.remove());
			});
			elem.slideDown(250);
		});

		var values = filter.find(".filterValue");
		values.each(i => {
			var elem = values.eq(i);
			elem.find(".removeBtn").on("click", () => {
				elem.slideUp(250, () => elem.remove());
			});
		});

		filter.find(".removeFilter").on("click", () => {
			filter.slideUp(250, () => {
				filter.remove();
			});
		});
	}

	var form = $("form");

	form.on("submit", f => {
		f.preventDefault();
		var data = [];

		var activeFilters = filterList.find(".filterEntry");

		activeFilters.each(i => {
			var curFilterEntry = activeFilters.eq(i);
			var elem = {
				Enabled: curFilterEntry.find("input[name=enable]").is(":checked"),
				Type: curFilterEntry.find("select[name=type]").val(),
				Filters: []
			};
			var fValues = curFilterEntry.find(".filterValue");
			fValues.each(j => {
				var fElem = fValues.eq(j);
				elem.Filters.push(fElem.find("input").val());
			});
			data.push(elem);
		});

		$.ajax({
			url: `/api/config/channel/${channelId}`,
			method: "PUT",
			data: {
				config: data
			}
		}).done(res => {
		});

	});

});