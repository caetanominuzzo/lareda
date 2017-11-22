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
	master_tile : $("#render_master_tile"),
	children : $("#render_children"),
	text	: $("#render_text"),
	parents	: $("#render_parents"),
	list	: $("#render_list"),
	select	: $("#render_select"),
}

$.input.models.master_stream.find('>.cards').click($.nav.parentsItemClick);

$.input.models.master_stream.find('>.cards').mouseenter($.nav.parentsItemFocus);

$.input.models.master_stream.find('>.cards').mouseleave($.nav.parentsItemLeave);



$.input.models.master_post.click($.nav.parentsItemClick);

$.input.models.master_tile.click($.nav.parentsItemClick);

$.input.models.children.click($.nav.parentsItemClick);

$.input.models.list.click($.nav.listItemClick);

$.input.models.parents.click($.nav.parentsItemClick);

$.input.models.text.click($.nav.parentsItemClick);

$.input.models.select.click($.nav.parentsItemClick);

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
		if(model == 'master_stream' || model == 'master_post' || model == 'master_tile')
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
	//data = '[{"root": "post", "collapsed": "4.00", "average": "2.50", "thumb_text": "ccc10", "address": "wA08ZlhXpGhS0qO3Xx7XgdyPgZjBWQSdiKsez4_hmY4=", "index": "0", "weight": "4.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "727"}, {"root": "post", "collapsed": "4.00", "average": "2.50", "thumb_text": "ccc10", "address": "-vulMifhA5fMzdx5AT3G_coJ4ime2yZvZkcrKXIa1cM=", "index": "0", "weight": "4.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "734"}, {"root": "post", "collapsed": "4.00", "average": "2.50", "thumb_text": "ccc10", "address": "uhZqrmTdHoo9mGMiwfBGB2egEP_eNjBpQGjT1rcKGQY=", "index": "0", "weight": "4.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "758"}, {"root": "post", "collapsed": "4.00", "average": "2.50", "thumb_text": "ccc10", "address": "dSMRDLDF1CZb9GPYC1UiP7BIHyBWP8wsVd3siMeI1XA=", "index": "0", "weight": "4.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "768"}, {"root": "post", "collapsed": "4.00", "average": "2.50", "thumb_text": "ccc10", "address": "9Hh_qJk2V4kmmzZxjen0yxnYEAjSC5oRJ4j14bsKQB4=", "index": "0", "weight": "4.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "783"}, {"root": "post", "collapsed": "4.00", "average": "2.50", "thumb_text": "ccc10", "address": "UUkLuHRf81IEejvelG0Izv0lJFy2ItL9ALv7-ALCPUo=", "index": "0", "weight": "4.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "793"}, {"root": "post", "collapsed": "4.00", "average": "2.50", "thumb_text": "ccc10", "address": "tdNHWGhh2Dr2Wp-7S0E6xZsQ1nLn1efTuCN_vYQCc9U=", "index": "0", "weight": "4.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "802"}, {"root": "post", "collapsed": "4.00", "average": "2.50", "thumb_text": "ccc10", "address": "bLiay5t4uItTcPuEmM9Xw7z06dyrzUYIn3VugRBANys=", "index": "0", "weight": "4.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "809"}, {"root": "post", "collapsed": "4.00", "average": "2.50", "thumb_text": "ccc10", "address": "IHPPNTGWuo0SDe-1pP666r-I0SxYaWFWerl7OLyCUZc=", "index": "0", "weight": "4.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "816"}, {"root": "post", "collapsed": "4.00", "average": "2.50", "thumb_text": "ccc10", "address": "BX-NkbLDXccmVic6x5y7xkw1KHzaslaL5Q1pi18H2K0=", "index": "0", "weight": "4.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "826"}, {"root": "post", "collapsed": "3.00", "average": "2.67", "thumb_text": "ddd10", "address": "iIG1801PafGy_IJ5er7tPQ-_JtoGh4h7ScpqlQDtzHM=", "index": "0", "weight": "3.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "741"}, {"root": "post", "collapsed": "3.00", "average": "2.67", "thumb_text": "eee10", "address": "G6tE_mp-m7BRWhtl8dfql0E93_K-te4uQyh7RE2vr7A=", "index": "0", "weight": "3.00", "date": "Sunday, April 9, 2017", "text": "", "pic": "7w-hwlmw4LNtkY2WedxKJJmQGhFE25KJnhjnlOGsQ5M=", "simple": "748"}]';

	var items = data;

	var parent = $.nav.search;

	var resultId = target.getAttribute("results");
	
	if(target.context.mode == 'list')
	{
		parent = $(target).parents('.subinput').children(".searchResults:first");
	}
	else if(target.context.mode == 'nav')
	{
		var $origin = $("div[address='" + target.text + "'] ~ .navItems:first", $.nav.search);

		parent = $origin;
	}
	
	if(items.length > 0)
	{
		$.input.updateItems(parent, items, target.context.mode, 0);
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
}

$.input.updateItems = function($parent_subresults, items, mode, deepness, parent, $parent_subparentresults, parent_index)
{
	if(typeof deepness == 'undefined') deepness = 0;

	$parent_subresults.show();

	$locals = $parent_subresults.children("div");
	
	for(var i = 0; i < items.length; i++)
	{
		var item = items[i];
		
		if(item.index != -1)
			item.index = i;
		
		var	$item = $parent_subresults.children("div[address='" + item.address + "']");
	
		var old_index = $item.attr("index");

		var new_item = false;

		if($item.length == 0)
		{
			new_item = true;
			
			$item = $.input.cloneModel(
						mode == 'select'? $.input.models.select : 
							mode == 'list'? $.input.models.list : 
								mode == 'children'? $.input.models.children :
									mode == 'parents'? $.input.models.parents :
										mode == 'text'? $.input.models.text : //(nav or main)
											item.root == 'post666' ? $.input.models.master_post :
												$.input.models.master_stream,
						item.address);

			var show = false;

			if(mode == 'nav')
			{
				if($parent_subresults.children().length > 0)
				{
					$parent_subresults.children().fadeOut('fast', function() { $(this).remove() });
				
					show = true;
				}

				//var colorBytes = $.utils.addressToColorBytes(item.address);

				//var colorBytesShadow = [colorBytes[0]-50, colorBytes[1]-50, colorBytes[2]-50];

				//$parent_subresults.css('background', 'rgba(' + colorBytes.join(', ') + ', 1)');
			}

			$parent_subresults.append($item);


			var $playButton = $('.large_play', $item);

			$playButton.on('click', $.menu.play);

			$card = $('>.cards', $item);

			$card.on('mouseenter', $.nav.mouseenter).on('mouseleave', $.nav.mouseleave);


		}
		else if(item.index == -1)
		{
			$item.remove();	
		}


		//if(!item.children || item.children[0].root == 'sequence' || mode == 'main' || typeof parent == 'undefined')
			$.input.bindItem($item, item, new_item);

		if(typeof parent_index != 'undefined')
			$item.attr('parent_index', parent_index);

		if(item.children && item.children.length > 0 && mode != 'main')
		{
			var $subresults_buttons_placeholder = $item.find("div.subitems_buttons_placeholder:first");

			var $subparentresults_buttons_placeholder = $item.find("div.subparentitems_buttons_placeholder:first");

			var $prev = $('.subresults_buttons.slick-prev', $item);

			var $next = $('.subresults_buttons.slick-next', $item);

			var $parentprev = $('.subparentresults_buttons.slick-prev', $item);

			var $parentnext = $('.subparentresults_buttons.slick-next', $item);

			var $subresults = $item.find("div.subitems:first");

			var $subparentresults = $item.find(".subparentitems:first");

			if(item.children[0].root == 'sequence')
			{
				$.input.updateItems($subparentresults, item.children, 'select', deepness+1, item, $subresults, i);
			}
			else if(item.root == 'sequence')
			{
				var child_index = $('>div', $parent_subparentresults).length;

				$.input.updateItems($parent_subparentresults, item.children, 'post', deepness+1, item, $subresults);

				$item.attr('child_index', child_index);
			}
			else
			{
				$.input.updateItems($subresults, item.children, 'post', deepness+1, item, $subparentresults);
			}


			if(deepness < 1)
			{
				
				/*$subresults.slick({
					//slidesToShow: 1,
					variableWidth: true,
					prevArrow: $prev,
					nextArrow: $next,
				});
				*/

				$subresults.children().after("<div class='navItems searchResults'>");

				$subresults.on('mousewheel', $.nav.scroll);

				$subresults.on('beforeChange', function(slick, currentSlide, nextSlide)
				{
				  	var selectedSlide = $(currentSlide.$slides[nextSlide]).attr('child_index');

					//todo: sync select
					//$subparentresults.slick('slickGoTo', selectedSlide);

				});

				$subresults.on('mouseenter', function()
				{
					$prev.attr('style', 'display: block !important');
					$next.attr('style', 'display: block !important');
				});
				
				$subresults.on('mouseleaeve', function()
				{
					$prev.removeAttr('style');
					$next.removeAttr('style');
				});

				$subparentresults.show();

				$subresults.show();

				$subparentresults.show();

			}


		}


	}


	if(mode == 'main')
	{
		/*
		$parent_subresults.slick({
					//slidesToShow: 1,
					variableWidth: true,
					prevArrow: $prev,
					nextArrow: $next,
				});
				*/
				
		$(".navItems", $parent_subresults).remove();
	
		$parent_subresults.children().after("<div class='navItems searchResults'>");
	}


	if(mode == 'nav' && new_item)
	{
		$item.hide().fadeIn('fast');

		var $card_title = $('.card_title', $item);

		$card_title.hide().css('left', 100);

		$card_title.fadeIn('fast').animate({left: 0}, 'fast');
	}




/*
	$.nav.search.children(".layout").sortElements(function(a, b)
	{
		return $(a).attr('index') - $(b).attr('index');
	});
	*/
}

$.input.bindItem = function($item, item, new_item)
{
	var $cards = $item.children('.cards');

	var $content = $cards.children('.clip').children('.content');

	if($item.is('.parents')) 
	{
		var parents_thumb_text =  $('.parents_thumb_text:first', $content);

		if(parents_thumb_text.text() != item.thumb_text)
			parents_thumb_text.text(item.thumb_text);

		return;
	}
	
	if($item.is('.option')) 
	{
		
		var parents_thumb_text =  $('.thumb_text:first', $item);

		if($item[0].children.length == 0)
		{
			$item.append("<div w3-include-html='"+item.thumb_text+"'></div>");
		}
		else
		{
			var $item_div = $($item[0].children[0]);

			if($item_div.attr('w3-include-html') != item.thumb_text)
				$item_div.attr('w3-include-html', item.thumb_text);

		}
/*
		if($item.text() != item.thumb_text)
			$item.text(item.thumb_text);
*/
		$item.attr('value', item.address);

		return;
	}
		

	var author =   $('.firstauthor:first', $content);

	var text =  $('.text:first', $content);

	var thumb_text =  $('.thumb_text:first', $cards);

	var date =   $('.firstdate:first', $cards);

	var img =  $content.find("video:first")[0] ||  $content.find("img:first")[0];	

	var audio = $content.find("audio:first")[0];

	var video_src = null;

	if(img.tagName == "VIDEO")
		video_src = img.children[0];
	
	var parents = $('.parents:first', $content); 

	if(typeof item.superiorchildren != 'undefined' && item.superiorchildren.length > 0)
		for(var i = 0; i < item.superiorchildren.length; i++)	
			$.input.updateItems(parents, item.superiorchildren, 'parents', 0);

	if($item.attr('address') != item.address)
		$item.attr('address', item.address);

	if(img &&
		((img.tagName == "VIDEO" && (typeof img.poster == 'undefined' || !img.poster.endsWith(item.pic))) ||
		 (img.tagName == "IMG" && (typeof img.src == 'undefined' || !img.src.endsWith(item.pic)))))
	{
		if(img.tagName == "VIDEO")
			img.poster = item.pic;
		else
			img.src = item.pic;
	}


	if(img && (img.tagName == "VIDEO" && (typeof item.video != 'undefined')))
	{
		if(!img.src.endsWith(item.video) && (!img.temp || !img.temp.endsWith(item.video)))
		{
			img.addEventListener('error', function() {
		       video_src.setAttribute("src", item.video);
		       img.load();
		       console.log('video error');
		    });

			img.temp = item.video;

			img.retry = 0;
			
			video_src.setAttribute("src", item.video);

			img.load();

			var setVideoSrc = function()
			{ 
				if(!isFinite(img.duration))
				{
					if(img.retry++ % 5 == 0)
					{
						//video_src.setAttribute("src", '');

						//video_src.setAttribute("src", item.video);

						//img.load();
					}

					setTimeout(setVideoSrc, 1000);
/*
					$.ajax({
						url: "keepAlive/",
						data:
							{
								files: JSON.stringify([item.video, item.audio]),
							},
						crossDomain: false,
						dataType: "json",
						method: "GET"
					});
*/
					return;						
				}

				if(typeof audio != 'undefined' &&  typeof item.audio != 'undefined')
				{
					if(!audio.src.endsWith(item.audio))
					{
						audio.setAttribute("src", item.audio);			
					}
				}

				if(img && img.tagName == "VIDEO" && (typeof item.subtitle != 'undefined'))
				{

					var count = 0;

					item.subtitles.forEach(function(t){

						var $subtitle = $('<track kind="subtitles" src="'+t.address+'" srclang="en" label="'+t.thumb_text+'" '+(count==0?'mode="showing"':'')+'>');

						count++;

						$(img).append($subtitle);
					});


				}


				if((typeof audio != 'undefined' &&  typeof item.audio != 'undefined') &&
					(img && img.tagName == "VIDEO" && typeof item.video != 'undefined') &&
					img.duration > 0 && audio.duration > 0)
				{
					img.id = "aa1";

					audio.id = "aa2";

					$.synchronizeVideos(0, "aa1", "aa2");
				}

			};
		}

		setTimeout(setVideoSrc, 1000);
	}

	if(img && (img.tagName == "VIDEO" && (typeof item.video == 'undefined') && (typeof item.audio != 'undefined')))
	{
		if(!img.src.endsWith(item.audio))
		{
			img.setAttribute("src", item.audio);			
		}
	}

	if(item.author)
		$.input.updateItems(author, [item.author], 'text', 0);

	//if(author.text() != item.author)
	//	author.text(item.author);

	if(date.attr('title') != item.date)
	{
		date.attr('title', item.date);

		date.timeago();
	}		



/*	if(text.text() != item.text)
		text.text(item.text);

	if(thumb_text.text() != item.thumb_text)
		thumb_text.text(item.thumb_text);
*/


	if(text.attr('w3-include-html') != item.text)
		text.attr('w3-include-html', item.text);

	if(thumb_text.attr('w3-include-html') != item.thumb_text)
		thumb_text.attr('w3-include-html', item.thumb_text);

	w3.includeHTML();


/*
	if(text.attr('data') != item.text)
		text.attr('data', item.text);

	if(thumb_text.attr('data') != item.thumb_text)
		thumb_text.attr('data', item.thumb_text);

*/

	$.menu.bind($item, item, img);


	if($item.attr('index') != item.index)
	{
		$item.attr("index", item.index);

		$item.attr("data-index", item.index);
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

	var colorBytesShadow = [colorBytes[0]-50, colorBytes[1]-50, colorBytes[2]-50];
	
	//$('.cards', $clone).css('background-color', 'rgba(' + colorBytes.join(', ') + ', .6)');
	//$('.cards', $clone).css('background-color', 'white');

//	$('.card_triangle', $clone).css('background-color', 'rgba(' + colorBytesShadow.join(', ') + ', 1)');

//	$('.card_title .top', $clone).css('border-left', '1px solid rgba(' + colorBytes.join(', ') + ', 1)');

	//$('.card_title .top', $clone).css('border-right', '1px solid rgba(' + colorBytes.join(', ') + ', .6)');

//	$('.card_title .top', $clone).css('border-bottom', '1px solid rgba(' + colorBytes.join(', ') + ', 1)');

//	$('.card_title .top', $clone).css('background-color', 'rgba(' + colorBytes.join(', ') + ', 1)');

//	$('.cards', $clone).css('box-shadow', '0px 0px 100px rgba(' + colorBytes.join(', ') + ', .3)');

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