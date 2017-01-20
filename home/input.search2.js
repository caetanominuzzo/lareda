$(function() {

$.input.search = function(text, target)
{
	$.ajax({
		url: "search/" + target.context.id + "/" + text.replace(/\+/g, "%2B").replace(/#/g, "%23").replace(/\?/g, "%3f"),
		dataType: "json"
	});
}

$.input.searchCallback = function(data, target)
{
	alert(data);

	var results;

	var resultId = target.getAttribute("results");

	var $results = $("div#" + resultId);

	data.forEach(function(valueHit)
	{
		$.input.AddRootResult(valueHit, $results);
	});

	//$("div.non-display-root-reference").removeClass("non-display-root-reference");

	//$.input.resolveHits($results);
	
	//$.input.sortHits($results);

	$("div.input").each(function(i, p)
	{
		p.parentNode.appendChild(p);
	})

	//$.input.removeRootElementsWhenPresentInAnotherRoot($results);

	//$.input.resolveGroups($results);

	//txt.focus();

	//$.filetypes.process($results);

	

	if(target.tagName == "IMG")
	{
		var $img = $results.find("img:first");

		if($img.length > 0)
		{
			target.setAttribute("src", $img.attr("src"));

			target.context.stop();
		}
		else
		{
			target.context.start($.user.userAddress)
		}
	}
	else
	{
		$results.show();
	}

}


$.input.resolveGroups = function($results)
{

	return;

	var groupCount = [];

	$results.children("div[address]").each(function(i, p)
	{
		var escape = p.getAttribute("address").replace(/([\-=\/\(])/g, "\\$1"); 

		var elements = $results.find("div[address=" + escape + "]");

		var count = elements.length * p.getAttribute("hits");

		$results.find("div[value=" + escape + "]").each(function(j, q)
		{
			count += parseInt(q.getAttribute("hits"));
		});

		groupCount.push({
			element: p,
			escape: escape,
			count: count,
			elements: elements
		});
	});


	groupCount.sort(function(a,b)
	{
		return b.count - a.count;
	});


	var $result = $("<div />").prependTo($results);

	if(groupCount.length > 0 && groupCount[0].elements.length == groupCount.length)
		return;

	if(groupCount == 0)
		return;

//	var item = groupCount[0];

	groupCount.forEach(function(item)
	{	


		if(item.element.getAttribute("grouped"))
			return;

		var $groupElement = $(item.element.cloneNode(true));

		$groupElement.css("color", "red");

		item.element.setAttribute("grouped", true); 

		
		$result.before($groupElement)

		//item.element.style.color = "red";

		item.elements.each(function(i, p)
		{
			if(p.getAttribute("grouped"))
				return;

			var $p = $(p);

			var parents = $.makeArray($p.parents());

			var moving = parents[parents.indexOf($results[0]) - 1];

			if(moving == null || moving.getAttribute("grouped"))
				return;
			
			var groupItem = moving.cloneNode(true);

			moving.setAttribute("grouped", true);

			var escape = p.getAttribute("address").replace(/([\-=\/\(])/g, "\\$1"); 

			$result.append($(groupItem).remove("div[address=" + escape + "]"));

		});
	});	

	$.input.resolveGroups($result);
}
				




$.input.sortHits = function($results)
{
	var $hits = $results.children("div[total-hits]");

	$hits.sort(function(a,b)
	{
		return b.getAttribute("total-hits") - a.getAttribute("total-hits");
	});


	$hits.each(function(i, p)
	{
		var $parent = $(p.parentNode);

		var $input = $parent.children("div.value");

		if($input.length == 1)
		{
			$(p).insertAfter($input);
		}
		else
			p.parentNode.appendChild(p);


		var hits = p.getAttribute("total-hits");

		var parentHits = p.parentNode.getAttribute("total-hits");

//return;

		if(hits < (parentHits / p.parentNode.childNodes.length))
			p.style.display = "none";
		else
			p.style.display = "";

	});
}

$.input.resolveHits = function($results)
{
	var $children = $results.children("div[hits]");

	if($children.length == 0)
	{
		$results.attr("total-hits", $results.attr("hits"));

		return parseInt($results.attr("hits"));
	}
	
	var total = 0;

	$children.each(function(i, p)
	{
		total += $.input.resolveHits($(p));
	});

	total = parseInt($results.attr("hits")) + total;

	$results.attr("total-hits", total);

	return total;
}

$.input.removeRootElementsWhenPresentInAnotherRoot = function($results)
{
	

	$results.children("div[address]").each(function(i, p)
	{
		var targetEscape = p.getAttribute("address").replace(/([\-=\/\(])/g, "\\$1"); 

		var res = $results.find("div[address=" + targetEscape + "]:not(.non-display-root-reference)");

		if(res.length > 1)
			$(p).addClass("non-display-root-reference");
	});

}


$.input.parents = function(e)
{
	var i = 0;
	var parent = e.parentNode;
	while(parent != document.body)
	{
		i++;
		parent = parent.parentNode;
	}

	return i;

}

$.input.prependContent = function($element, content)
{
	if(content == null)
		return;

	if($element.children("span.value").length == 0)
	{
		$("<span />",
		{
			text: $.utils.decodeHtml(content),
			class: "value"
		}).
		prependTo($element);
	}
}

$.input.appendTarget = function($result, address, targetAddress, $results)
{
	var targetEscape = targetAddress.replace(/([\-=\/\(])/g, "\\$1"); 

	var $targets =  $results.find("div.field[address=" + targetEscape + "]");

	if($targets.length == 0)
	{
		var $targets = $("<div />",
		{
			address: targetAddress,
			class: "field",
			hits: 0
		}).
		appendTo($results);
	}

	var escape = address.replace(/([\-=\/\(])/g, "\\$1"); 

	$targets.each(function(i, p){

		var $p = $(p);

		if($p.children("div.field[address=" + escape + "]").length > 0)
			return;

		var $r = $result.clone().show();

		$p.append($r[0]);

	});
}

$.input.AddRootResult = function(post, $results)
{
	var escape = post.A.replace(/([\-=\/\(])/g, "\\$1"); 

	var $result = $results.find("div.field[address=" + escape + "]");

	if($result.length == 0)	
	{
		$result = $("<div />",
		{
			address: post.A,
			value: $.utils.isBase64Address(post.C) ? post.C : null,
			target: post.B,
			class: "field"
		});

		if(post.B)
			$.input.appendTarget($result, post.A, post.B, $results)

		else
			$results.append($result);
	}
	else
		if(post.B)
			$.input.appendTarget($result, post.A, post.B, $results)

	$result = $results.find("div.field[address=" + escape + "]");



	if($.utils.isBase64Address(post.C))
	{
		var valueEscape =  post.C.replace(/([\-=\/\(])/g, "\\$1"); 

		$valueResult = $result.find("div.field[address=" + valueEscape + "]");

		if($valueResult.length == 0)
		{
			$valueResult = $results.find("div.field[address=" + valueEscape + "]:first");

			if($valueResult.length > 0)
			{
				$valueResult = $valueResult.clone().show();

				//$valueResult.attr("hits", parseInt($valueResult.attr("hits")) + post.Hits);

				$valueResult.appendTo($result);
			}
			else
			{
				$valueResult = $("<div />",
				{
					address: post.C,
					class: "field",
				//	hits: post.Hits
				}).
				appendTo($result);
			}
		}
		//else
		//	$valueResult.attr("hits", parseInt($valueResult.attr("hits")) + post.Hits);

	}
	else
		$.input.prependContent($result, post.C);






	$result = $results.find("div.field[address=" + escape + "]");

	$result.attr("hits", (parseInt($result.attr("hits")) || 0) + post.Hits);




	var $values = $results.find("div.field[value=" + escape + "]");

	$values.each(function(i, p)
	{
		if($(this).children("div.field[address=" + escape + "]").length == 0)
		{
			var r = $result.clone().show()[0];

			//r.setAttribute("hits", parseInt(r.setAttribute("hits")) + parseInt(p.getAttribute("hits")));

			p.appendChild(r);
		}
	});
	


	if(!$.utils.isBase64Address(post.C))
	{
		//$.input.appendInput($result);

		//$result[0].addEventListener("click", $.input.search.onClick);
	}
	
	return $result;
}

$.fn.setHits = function(hits)
{
	return this.each(function()
	{
		this.setAttribute("hits", (parseInt(this.getAttribute("hits")) || 0) + (hits || 1));

		var t = this.parentNode;

		if(t == null)
			return;

		while(t != document.body)
		{
			if(t.hasAttribute("hits"))
				t.setAttribute("hits", (parseInt(t.getAttribute("hits")) || 0) + (hits || 1));	

			t = t.parentNode;				
		}	
	});
}

$.input.search.onClick = function(e)
{
	e.cancelBubble = true;

	if(e.srcElement.tagName == "INPUT")
		return;

	var $e = $(e.srcElement);

	$result = $e.parents("div .input");

	var $selected = $(e.path).filter(".results > .field");

	var $value =  $selected.find(".value:first")

	var $input = $("input", $result);

	$input[0].setAttribute("preventTriggers", true);

	$input.focus();

	var pos = $input[0].selectionStart;

	var hashBegin = $.input.search.hashBegin($input.val(), pos);

	var hashEnd = $.input.search.hashEnd($input.val(), pos);

	var text = $input.val();

	text = text.slice(0, hashBegin) + "#" + $value.text() + text.slice(hashEnd, text.length) + " ";

	$input.val(text);

	$input[0].setAttribute("last_value", text);

	$input[0].setAttribute("address", $selected.attr("address"));

	//$input[0].ids.push([{ value: $value.text(), address: }]);

	$input[0].setAttribute("prevent_triggers", false);

	$input[0].parentNode.lastChild.innerHTML = "";

	$input[0].parentNode.lastChild.style.display = "none";
}

$.input.search.hashBegin = function(text, start)
{
	for(var i = start; i > 0; i--)
	{
		if(text[i] == "#")
			return i;
	}

	return 0;
}


$.input.search.hashEnd = function(text, start)
{
	for(var i = start; i < text.length; i++)
	{
		if(text[i] == " ")
			return i;
	}

	return text.length;
}



})
