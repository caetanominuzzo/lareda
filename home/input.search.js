$(function() {

$.input.search = function(target, term)
{
	$.ajax({
		url: "search/", //+ target.context.id + "/" + target.context.mode + "/" + text, 	//.replace(/\+/g, "%2B").replace(/#/g, "%23").replace(/\?/g, "%3f"),
		data:
            {
                term: term || target.text,
                context: target.context.id,
                mode: target.context.mode,
                parent: target.context.parent ? target.context.parent.context.id : null,
                filter: target.filter || null
            },
		dataType: "json",
		method: "POST"
	});
}

$.input.models = 
{
	master_stream : $("#render_master_stream"),
	master_post : $("#render_master_post"),
	children : $("#render_children"),
	text	: $("#render_text"),
	parents	: $("#render_parents"),
	list	: $("#render_list")
}


$.input.models.children.click($.nav.parentsItemClick);

$.input.models.list.click($.nav.listItemClick);

$.input.models.parents.click($.nav.parentsItemClick);

$.input.models.text.click($.nav.parentsItemClick);

/*
$.input.models.children.click($.nav.searchItemClick);

$.input.models.list.click($.nav.searchItemClick);

$.input.models.parents.click($.nav.searchItemClick);

$.input.models.text.click($.nav.searchItemClick);
*/

for (var model in $.input.models)
{
    if ( $.input.models.hasOwnProperty(model))
    {
		if(model == 'master_stream' || model == 'master_post')
			continue;

		var $thumb = $('.thumb_text:first', $.input.models[model]);

		$thumb.css('cursor', 'pointer');

    	$thumb.mouseenter($.nav.mouseenter);

    	$thumb.mouseleave($.nav.mouseleave);
    }
}

$('input[type=button]', $.input.models.master_stream).click($.nav.post);

$('input[type=button]', $.input.models.master_post).click($.nav.post);



$.input.searchCallback = function(data, target)
{
	var items = data;

	var parent = $.nav.search;

	var resultId = target.getAttribute("results");
	
	if(target.context.mode == 'list')
	{
		parent = $(target).parents('.subinput').children(".searchResults:first");
	}
	else if(target.context.mode == 'nav')
	{
		parent = $.nav.items;
	}
	

	if(items.length > 0)
	{
		if(parent == $.nav.search)
		{
			parent.empty(); //todo:temp

			if(parent.children('.layout-sizer').length == 0)
				parent.append('<div class="layout-sizer"></div>');

			parent.isotope({
				  itemSelector: '.layout',
				  percentPosition: true,
				  transitionDuration: 0,
				  masonry: {
					columnWidth: '.layout-sizer'
				  }
				});
		}

		
		items[0].root = 'stream';

		$.input.updateItems(parent, items, target.context.mode);

		

		//items[0].address = 'k5c0241K9ckEw3ruLh2ZTUppkFtTurVikZJTuMn8UX8_';

		//$.input.updateItems(parent, items, target.context.mode);
	}

	if(target.context.mode == 'list' )
	{
		if(parent.text() != '')
		{
			parent.css('max-height', ($('body').height() - parent.position().top));

			parent.css('width', 260);

			parent.css('display', 'block');
		}
		else
			parent.css('display', 'none');
	}

	$("#searchResults").isotope();

	return;

	$(".menu_item").click(function()
	{
		var t = $('video:first', $(this).parents('.cards'))[0];

		if(t.paused)
			t.play();
		else
			t.pause();
	});
}

$.input.updateItems = function($parent, items, mode)
{
	if(mode == 'post')
	{
		debugger;
	}

	$locals = $parent.children("div");
	
	for(var i = 0; i < items.length; i++)
	{
		var item = items[i];

		var	$item = $parent.children("div[address='" + item.address + "']");
	
		if(item.index != -1)
			item.index = i;
				
		if($item.length == 0)
		{
			$item = $.input.cloneModel(
						mode == 'list'? $.input.models.list : 
							mode == 'children'? $.input.models.children :
								mode == 'parents'? $.input.models.parents :
									mode == 'text'? $.input.models.text :
										item.root == 'post' ? $.input.models.master_post :
											$.input.models.master_stream,
						item.address);

			$parent.append($item)

			if($parent[0] == $.nav.search[0])
				$parent.isotope('addItems', $item );

			//$parent.isotope( 'addItems', $item );

		}
		else if(item.index == -1)
		{
			$item.remove();	
		}

		$.input.bindItem($item, item);

		if(item.children && item.children.length > 0)
		{
			var $subresults = $item.find("div.subitems:first");

			$.input.updateItems($subresults, item.children, 'children');
		}
	}

	$.nav.search.children(".layout").sortElements(function(a, b)
	{
		return $(a).attr('index') - $(b).attr('index');
	});
}

$.input.bindItem = function($item, item)
{
	var $content = $item.children('.cards').children('.clip').children('.content');

	if($item.is('.parents')) 
	{
		var parents_thumb_text =  $('.parents_thumb_text:first', $content);

		if(parents_thumb_text.text() != item.thumb_text)
			parents_thumb_text.text(item.thumb_text);

		return;
	}
		

	var author =   $('.firstauthor:first', $content);

	var text =  $('.text:first', $content);

	var thumb_text =  $('.thumb_text:first', $content);

	var date =   $('.firstdate:first', $content);

	var img =  $content.find("video:first")[0] ||  $content.find("img:first")[0];	

	var audio = $content.find("audio:first")[0];

	var subtitle = $content.find("track:first")[0];
	
	var parents = $('.parents:first', $content); 

	if(typeof item.superiorchildren != 'undefined' && item.superiorchildren.length > 0)
	{
		for(var i = 0; i < item.superiorchildren.length; i++)	
		{
			$.input.updateItems(parents, item.superiorchildren, 'parents');

		}
	}

	if($item.attr('address') != item.address)
		$item.attr('address', item.address);


	if(img && ((img.tagName == "VIDEO" && (typeof img.poster == 'undefined' || !img.poster.endsWith(item.pic)))
	|| (img.tagName == "IMG" && (typeof img.src == 'undefined' || !img.src.endsWith(item.pic)))))
	{
		if(img.tagName == "VIDEO")
			img.poster = item.pic;
		else
			img.src = item.pic;
	}

	if(img && (img.tagName == "VIDEO" && (typeof item.video != 'undefined')))
	{
		if(!img.src.endsWith(item.video))
		{
			img.setAttribute("src", item.video);			
		}

		if(typeof audio != 'undefined' &&  typeof item.audio != 'undefined')
		{
			if(!audio.src.endsWith(item.audio))
			{
				audio.setAttribute("src", item.audio);			
			}
		}


	}

	if(img && (img.tagName == "VIDEO" && (typeof item.video == 'undefined') && (typeof item.audio != 'undefined')))
	{
		if(!img.src.endsWith(item.audio))
		{
			img.setAttribute("src", item.audio);			
		}
	}

	if(img && img.tagName == "VIDEO" && (typeof item.subtitle != 'undefined'))
	{
		subtitle.setAttribute("src", item.subtitle);			
	}


	if(item.author)
		$.input.updateItems(author, [item.author], 'text');

	//if(author.text() != item.author)
	//	author.text(item.author);

	if(date.attr('title') != item.date)
	{
		date.attr('title', item.date);

		date.timeago();
	}		


	if(text.text() != item.text)
		text.text(item.text);

	if(thumb_text.text() != item.thumb_text)
		thumb_text.text(item.thumb_text);


	$.menu.bind($item, item, img);


	if($item.attr('index') != item.index)
		$item.attr("index", item.index);

	if(item.index == 0)
	{
		if(!$item.parent().hasClass('subitems'))
		{
			$('.first').removeClass('first');
			
			$item.addClass('first');
		}

		$item.removeClass('pure-u-md-1-4');
	}
}



$.input.cloneModel = function($model, address)
{
	if(typeof $.input.ids == 'undefined')
		$.input.ids  = 1;

	var $clone = $model.clone(true);

	$clone.attr('id', 'id' + $.input.ids++);

	$clone.attr('address', address);

	var colorBytes = $.utils.addressToColorBytes(address);

	
	$('.cards', $clone).css('background-color', 'rgba(' + colorBytes.join(', ') + ', .6)');

	$('.cards', $clone).css('box-shadow', '0px 0px 100px rgba(' + colorBytes.join(', ') + ', .3)');

	return $clone;
}

$.input.removeItems = function($locals, items)
{
	$locals.each(function(i, t)
	{
		var $t = $(t);

		var found = false;

		for(var j = 0; j<items.length; j++)
		{
			if(items[j].address == $t.attr('address'))
			{
				found = true;

				break;
			}
		}

		if(!found)
			$.input.removeItem($t);
	});
}

$.input.removeItem = function($item)
{
	$.nav.items.children("div[address='" + $item.attr('address') + "']").remove();

	$item.remove();
}

})

/**
 * jQuery.fn.sortElements
 * --------------
 * @param Function comparator:
 *   Exactly the same behaviour as [1,2,3].sort(comparator)
 *   
 * @param Function getSortable
 *   A function that should return the element that is
 *   to be sorted. The comparator will run on the
 *   current collection, but you may want the actual
 *   resulting sort to occur on a parent or another
 *   associated element.
 *   
 *   E.g. $('td').sortElements(comparator, function(){
 *      return this.parentNode; 
 *   })
 *   
 *   The <td>'s parent (<tr>) will be sorted instead
 *   of the <td> itself.
 */
jQuery.fn.sortElements = (function(){
 
    var sort = [].sort;
 
    return function(comparator, getSortable) {
 
        getSortable = getSortable || function(){return this;};
 
        var placements = this.map(function(){
 
            var sortElement = getSortable.call(this),
                parentNode = sortElement.parentNode,
 
                // Since the element itself will change position, we have
                // to have some way of storing its original position in
                // the DOM. The easiest way is to have a 'flag' node:
                nextSibling = parentNode.insertBefore(
                    document.createTextNode(''),
                    sortElement.nextSibling
                );
 
            return function() {
 
                if (parentNode === this) {
                    throw new Error(
                        "You can't sort elements if any one is a descendant of another."
                    );
                }
 
                // Insert before flag:
                parentNode.insertBefore(this, nextSibling);
                // Remove flag:
                parentNode.removeChild(nextSibling);
 
            };
 
        });
 
        return sort.call(this, comparator).each(function(i){
            placements[i].call(getSortable.call(this));
        });
 
    };
 
})();
