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

$(window).resize(function()
{
	$.nav.items.detach();

	$.nav.addNav($.last_selected, false);
})

$.nav.addNav = function(t, hover)
{
	$this = $(t).closest('.layout');

		var sib = $this;

		var next = $this.next();

		var end = next.length == 0;

		while(!end)
		{
			next = sib;

			sib = sib.next();

			end = sib.length == 0 || sib.offset().top != $this.offset().top;
		} 

		if(next.length > 0)
			sib = next;

	
		if((sib[0] != $.nav.items.prev()[0] || !$.nav.items.is(":visible")))
		{
			if(hover)
				return false;

			$.nav.items.css('height', '0px');

			sib.after($.nav.items);

			$.nav.items.animate({height : $('body').height() - $this.height() - 88}, 100,
				function() {  
				});

			$.nav.items.fadeIn(1000);	


			$('html, body').animate({
				scrollTop: $this.offset().top - 71
			}, 100);

			return true;
		}

		return false;
}

$.nav.parentsItemClick = function(e, t, hover)
{
	e.stopPropagation();

	if(!t)
		t = this;

	var $this = $(t);

	if($this.parents(".navItems").length > 0)
		return;

	if(t.className.indexOf('parents') == -1)
		return;

	var text = $this.attr("address");

	if(true)
	{
		if($.last_selected != t)
			$.nav.items.children().fadeOut(500, function() { $(this).remove();  });

		$.searchInput[0].context.start(text, 'nav');

		$.last_selected = t;
	}
}

$.nav.parentsItemFocus = function(e)
{
	e.stopPropagation();

	if($.nav.items.is(":visible"))
	{
		$.nav.parentsItemClick(e, this, true);
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