$(function() {

$.sort_by_hits = function (a, b)
{
    return a.getAttribute("hits") < b.getAttribute("hits");
}

$.indent = function()
{
	var list = Array.prototype.slice.call(document.getElementsByTagName("div"));

	list.sort($.sort_by_hits);

	var cache = []

	var target;

	for (var i = 0; i < list.length; i++)
	{
		if(!list[i].hasAttributes("hits"))
			continue;
		//target = list[i].getAttribute("target");

/*
		if(typeof(target) == "undefined")
			continue;

		if(typeof(cache[target]) == "undefined")
		{
			cache[target] = $("#" + target)[0];

			if(typeof(cache[target]) == "undefined")
				continue;			
		}
*/
		list[i].parentNode.appendChild(list[i]);
	}

}



})