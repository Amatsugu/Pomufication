$(() => {

	var addBtn = $(".addBtn");

	var addChannel = $("#addChannel");

	var channelInput = addChannel.find("input");

	var form = $("form");
	form.on("submit", f =>
	{
		f.preventDefault();
		$.ajax({
			url: `/api/config/channel/add?url=${encodeURIComponent(channelInput.val())}`,
			method:"POST"
		}).done(() =>
		{
			window.location.reload();
		}).fail(err =>
		{
			console.log(err);
		});
	});

	

	addBtn.on("click", () => {
		addChannel.slideToggle();
	});

});