$(function() {

$.nav = {};

$.nav.search = $("div#searchResults:first");

$.nav.items = $("div#navItems:first");

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

		$.nav.search.css('-webkit-filter', 'blur(1px)');

		return;
	}


	$navItem = $.input.cloneModel($.input.models.master, $this.attr('address'));

	$navItem.off('click');
	
	$.nav.items.append($navItem);

	$.context.create($navItem, 'nav', $("#searchInput")[0]);

	$navItem[0].context.start($navItem.attr('address'));

	//

	//$navItem = $this.clone(true);

	

	$.nav.search.css('-webkit-filter', 'blur(1px)');


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


$.nav.parentsItemClick = function(e)
{
	e.stopPropagation();

	var $this = $(this);

	var text = $this.attr("address");// $(".thumb_text:first", $this).text();

//	$.nav.search.empty();
	
	$.nav.items.empty();

	$.searchInput[0].context.start(text);

	//$.input.search($.searchInput[0], text);
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