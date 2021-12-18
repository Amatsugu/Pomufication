$(() => {

	var channelTemplate = $("#template");
	var addBtn = $(".addBtn");

	var addChannel = $("#addChannel");

	var channelSearch = addChannel.find("input");
	var results = addChannel.find(".channelResult");
	var searchReq;

	var timeout = -1;

	channelSearch.on("input", () => {
		if (timeout != -1) {
			clearTimeout(timeout);
			timeout = -1;
		}

		timeout = setTimeout(PerformSearch, 500);
	});

	function PerformSearch() {
		console.log("Searching...");
		if (searchReq)
			searchReq.abort();
		if (!channelSearch.val()) {
			results.html(`<div class="status">Enter a channel name</div>`);
			return;
		}
		results.html(`<div class="status">Loading...</div>`);

		searchReq = $.ajax({
			url: `/api/config/channel/search?query=${encodeURIComponent(channelSearch.val())}`
		}).done(res => {
			results.html("");
			for (var i = 0; i < res.items.length; i++) {
				let channel = res.items[i];
				var elem = $(channelTemplate.html());
				elem.find(".icon").css("background-image", `url(${channel.thumb})`);
				elem.find(".name a").text(channel.name);

				elem.on("click", () => {
					$.ajax({
						url: `/api/config/channel/${channel.id}/add`,
						method: "POST"
					}).done(() => {
						window.location = `/config/channel/${channel.id}`;
					}).fail(err => {
						//TODO: Handle Error
					});
				});
				results.append(elem);
			}
			if (res.items.length == 0) {
				results.html(`<div class="status">No Results Found</div>`);
			}
		});
	}

	addBtn.on("click", () => {
		addChannel.slideToggle();
	});

});