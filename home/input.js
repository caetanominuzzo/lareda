$(function() {

$.input = {};

$.input.start = function()
{
	$.input.monitorChanges();
}

$.input.monitorChanges = function()
{
	setInterval(function() 
	{
		$("div.searchInput").each(function(i, txt)
		{
			if(typeof txt.context == 'undefined')
				return;

			var last_value = txt.getAttribute("last_value");

			if(last_value == txt.innerText)
				return;

			txt.setAttribute("last_value", txt.innerText);

			var results;

			if(txt.context.mode == "main")
				results = $.nav.search;
			else
				results = $(txt).parents('.subinput').children(".searchResults:first");

			var contains = txt.innerText.indexOf(last_value) > -1;

			if(!contains)
			{
				results.empty();

				

				$.nav.items.empty();

				if(txt.context.mode != "main")
					results.css('display', 'none');

				if(results.hasClass('isotope'))
					results.isotope('destroy');
			}

			if(txt.innerText.length > 0)
			{
				txt.context.start(txt.innerText, 'main');
			}
		})
	}, 100);
}

$.input.onKeyPress = function(e)
{
	if(String.fromCharCode(e.which) == "#")
	{
		var $marker_parent = $("<span class='marker_parent' contentEditable='false' />");	

		var $marker = $("<span class='marker' />");

		$marker_parent.append($marker);

		var $search  = $("<div class='searchInput inlineSearchInput' contentEditable='true' />");

		$subinput = $("<div class='subinput'><div class='subSearch searchResults' /></div>");

		$subinput.prepend($search);

		$marker.append($subinput);

		$(this).append($marker_parent);

		$.input.appendInput($search);

		$.context.create($search, "list");

		$search.focus();

		e.preventDefault();

		return false;
	}

	return;


	if (e.keyCode != 13)
		return;

	var parent = $(e.target.parentNode);

	var input = $(".searchInput:first", parent);

	var t = $(e.target);

	var address = t.attr("address");

	var value = t.text();

	if(address)
	  value = address; //.replace(/\+/g, "%2B").replace(/#/g, "%23").replace(/\?/g, "%3f");

	$.input.post(value, t.parents("div[address]").attr("address"));

	//e.target.context.start(e.target.value);

	t.empty();

	results = t.parent().children(".searchResults:first");

	results.hide();

	return false;
	
}


$.input.appendInput = function($target)
{
	/*$target.after($("<div />",
	{
		class : "innerButton searchButton",
	}));*/

	$target.
		attr("last_value", "").
		keypress($.input.onKeyPress).
		focus($.input.onFocus).
		blur($.input.onBlur);	

	return $target;
}

$.input.onFocus = function(e)
{
	
}

$.input.onBlur = function(e)
{
	
}

})