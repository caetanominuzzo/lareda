function UploadAdd(data)
{
	onPostEnter();

	var post_items = $("#post_items");

	var post_item = $('<div/>',
	{
	    class: 'post_item',
	    value: data.id,
	    text: 'post'
	}).appendTo(post_items);

	$.each(data.files, function(i, p)
	{
		var post_file = $("<div />", 
		{
			class: 'post_file',
			text: 'file'
		}).appendTo(post_item);

		for(var i = 0; i< p.length; i++)
		{
			$("<div/>",
			{
				text: p[i].Key + ": " + p[i].Value
			}).appendTo(post_file);
		}
	});
}