$(() => {
	var form = $("form");
	form.on("submit", f => {
		f.preventDefault();
		$.ajax({
			url: "/api/config/system",
			method: "PUT",
			enctype: "application/x-www-form-urlencoded",
			data: form.serialize()
		});
	});
});