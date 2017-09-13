$(function() {

$.nav = {};

$.nav.search = $("div#searchResults:first");

$.nav.items = $("div.navItems:first");

$.nav.searchItemClick = function(e)
{
	e.stopPropagation();

	var $this = $(this);

	var $navItem =  $.nav.items.children('div[address="' + $this.attr('address') + '"]');

	if($navItem.length == 1)
	{
		$navItem[0].context.start($navItem.attr('address'));
		
		$navItem.
			css('top', $this.offset().top).
			css('width', $('div.first').width()).
			css('min-height', $this.height()).
			css('z-index', $.nav.top++).		
			show();

		var $input = $(".searchInput", $navItem);

		$input.focus();

		return;
	}


	$navItem = $.input.cloneModel($.input.models.master_stream, $this.attr('address'));

	$navItem.off('click');
	
	$.nav.items.append($navItem);

	$.context.create($navItem, 'nav', $("#searchInput")[0]);

	$navItem[0].context.start($navItem.attr('address'));


	$navItem.
		css('position', 'absolute').
		css('top', $this.offset().top).
		css('width', $('div.first').width()).
		css('min-height', $this.height()).
		css('z-index', $.nav.top++).		
		removeClass('anchor');

	if($this.offset().left > $('body').width()  / 2)
		$navItem.css('right', ($('body').width() / 18) - 1);
	else
		$navItem.css('left', $this.offset().left);

	


	$navItem.show();

	

	

	var $input = $(".searchInput", $navItem);

	//$.input.appendInput($input, null, "list");

	$input.keypress($.input.onKeyPress);

	$input.focus();

	$input.css('height','80px');

	return;	

	var $t = $("div.menu:first", this);

	$t.css('width', parseInt($t.parent().children('.firstpic').css('width')) - 12);

	$t.css('height', (parseInt($t.parent().children('.firstpic').css('height')) - 10) / 2);

	//$t.css('margin-top', (parseInt($t.parent().children('.firstpic').css('height'))  /2) - 20);

	var $sub = $("div.menu_sub:first", this);

	$sub.css('margin-bottom', (parseInt($t.parent().children('.firstpic').css('height')) /10));


	var $main = $("div.menu_main:first", this);

	$main.css('margin-left', (parseInt($t.parent().children('.firstpic').css('width'))-55) / 2);

	$t.css('visibility', 'visible');	
}

$.nav.listItemClick = function(e)
{
	e.stopPropagation();

	var $this = $(this);

	var text = $(".text:first", $this).text();

	var $input = $('.searchInput:first' , $this.parents('.subinput'));

	var $marker = $(".marker", $input);

	$marker.empty();

	$marker.append($this.children().not("img").clone());

	$marker.addClass("marker_ready");

	$marker.attr("address", $this.attr("address"));

	$input.append("&nbsp;");

	$input.focus();	
}


$.nav.parentsItemFocus = function(e)
{
	e.stopPropagation();

	var $layout = $(this).parents('.layout:first');

	var $parentResults = $($layout.parents()[0]);

	if($parentResults.is('.navItems'))
		return;

	

	var $sibs = $parentResults.find('>.layout').not($layout).css('z-index', 1);

	var layout_middle = parseInt(($layout.position().top + $layout[0].getBoundingClientRect().height / 2)/20);

	var layout_x = $layout.position().left + $layout.width();

	if(layout_x > $parentResults.parent().width())
	{
		$.nav.scroll({
			currentTarget : $layout.parent()[0],
			originalEvent : {
				wheelDelta : -1

			}
		})
		return;
	}

	$sibs.each(function (i)
	{
		var $this = $(this);

        var this_middle = parseInt(($this.position().top + $this[0].getBoundingClientRect().height / 2)/20);

        var origin_x = parseInt(parseInt($layout.css('transform-origin'))/20);

		if(this_middle == layout_middle)
		{
			console.log(i + ' top ' + $this.position().top);

			if($this.position().left < $layout.position().left)
			{
				$this.addClass('layout_left');

				if(origin_x == 0 || origin_x == parseInt($this.width()/20))
					$this.addClass('double_layout_left');
			}
			else 
			{
				if(origin_x != parseInt($this.width()/20))
					$this.addClass('layout_right');

				if(origin_x == 0)
					$this.addClass('double_layout_right');
			}
		}

	});

	$layout.addClass('layout_scale')

	$layout.css('z-index', 30000);
}

$.nav.parentsItemLeave = function(e)
{
	e.stopPropagation();

	var $layout = $(this).parents('.layout:first');

	var $parentResults = $($layout.parents()[0]);

	var $sibs = $parentResults.find('>.layout').not($layout);

	$sibs.removeClass('layout_left').removeClass('layout_right').removeClass('double_layout_left').removeClass('double_layout_right');


	$layout.removeClass('layout_scale');
}

$.nav.parentsItemClick = function(e, t, hover)
{
	e.stopPropagation();

	if($(e.currentTarget).is('.large_play'))
	{
	
		//return;	
	}

	if(!t)
		t = this;

	var $this = $(t).parents('.layout:first');

	if($this.parents(".navItems").length > 0)
		return;

	var text = $this.attr("address");

	$('.card_title .top').css('background-image', 'initial');

	$('.card_title .top', $this).css('background-image', 'url(images/ic_navigate_down_white_48dp_2x.png');

	$('.clip').css('outline', '0px');

	$('.clip', $this).css('outline', '2px solid #ccc');


	if(true)
	{
		if($.last_selected != t)
			$.nav.items.children().fadeOut(500, function() { $(this).remove();  });

		$.searchInput[0].context.start(text, 'nav', function(dat, target)
		{
			if($(e.currentTarget).is('.large_play'))
			{
				var $origin = $("div[address='" + target.text + "'] ~ .navItems:first", $.nav.search);

				var  $large_play = $('.large_play', $origin);
				
				$.menu.play(e, $large_play[0]);
			}
		});

		$.last_selected = t;
	}
}

$.nav.mouseenter = function(e)
{
	var $this = $(this);


	e.stopPropagation();

}

$.nav.mouseleave = function(e)
{
	var $this = $(this);

}

$.nav.sliding = false;

$.nav.scroll = function(e)
{
	if($.nav.sliding)
		return;

	$.nav.sliding = true;		

	var $this = $(e.currentTarget);

	if($this.is('body'))
		$this = $this.find('#body>#searchResults');

	var $sibs = $this.find('>.layout');

	if($sibs.length < 2)
	{
		$.nav.sliding = false;

		return;
	}

	$sibs.removeClass('layout_left').removeClass('layout_right').removeClass('double_layout_left').removeClass('double_layout_right').removeClass('layout_scale');

	if(e.originalEvent.wheelDelta < 0)
	{
		var $first = $($sibs[0]);

		var $nav = $first.next();
		
		var $last = $($sibs[$sibs.length-1]).next();

		var height = $first.height();

		$first[0].style.height = height;

		$first.animate({width:'0px', opacity: 0}, 400, 'easeInOutCubic', function(){

			$first.detach();

			$nav.detach();

			$last.after($first);

			$first.after($nav);

			$first[0].style.width = null;

			$first[0].style.height = null;

			$first[0].style.opacity = null;

			$.nav.sliding = false;		
		});
	}
	else
	{
		var $last = $($sibs[$sibs.length-1]);

		var $nav = $last.next();
		
		var $first = $($sibs[0]);

		var width = $last.width();

		var height = $last.height();

		$last.detach();

		$nav.detach();

		$last[0].style.width = "0px";

		$last[0].style.height = height;

		$last[0].style.opacity = "0";

		$first.before($last);		

		$last.after($nav);				

		$last.animate({width:width, opacity: 1}, 400, 'easeInOutCubic', function(){
			$last[0].style.width = null;

			$last[0].style.height = null;

			$.nav.sliding = false;		
		});
	}

	$.nav.scroll_x += (e.originalEvent.wheelDelta /1);

	//var s = 'translate3d('+ $.nav.scroll_x +'px, 0px,  0px)';

	//$sibs.css({'transform' : s});



}

$.nav.post = function(e)
{
	var $this = $(this);

	var $parent = $this.parents("div[address]");

	var $input = $(".searchInput", $parent);

	var $refs = $(".marker_parent > .marker > .cards > .clip > .content > *", $input).not(".text");

	$refs.remove();

	$.input.post($input.text(), $parent.attr("address"));

	var $markers = $(".marker_parent > .marker", $input);

	$markers.each(function(i, t)
	{
		$.input.post($(t).attr('address'), $parent.attr("address"));
	});

	$input.empty();
}


});