$(function() {

$.filetypes = 
{
	image :
	{						 
		selector: "div[value=uT\\-fzw9qpMWIhR4cynegdEiPt6IuDxmWXwGjFbMhyUw\\=]",
		tag: "img",
		append: "<img src='#ADDRESS#' />",
		view: "<a href='#ADDRESS#?type=image' target='_blank' width=300 height=120 ><div class='view' /></a>"
	},

	download :
	{
		selector: "div[value=mEniHQkMD3CfpumfpiZRSHj5KobLo3gobNwWOJEjTkM\\=]",
		append: "<a href='javascript:$.get(\"#ADDRESS#?type=download&name=#NAME#\")' target='_blank' ><div class='download' /></a>",
	},

	video:
	{
		selector:  "div[value=syOawLi\\-4MeGohZbhzpeCuxxAH0TMe3PU55MhIcfIlM\\=]",
		append: "<a href='#ADDRESS#?type=video/mp4' target='_blank' ><div class='play' /></a>",
	},	

	web :
	{
		selector: "div[value=au6qAF7iS9Kl5UZYzT0eOHYBjJAffV9zGwsugktlQeU\\=]",
		append: "<a href='#ADDRESS#/?type=web' target='_blank' ><div class='web' /></a>",
	},


	process : function($results)
	{
		var $items = $results.find($.filetypes.image.selector).parent();

		$items.each(function(i, p)
		{
			var $p = $(p);

			if($p.children($.filetypes.download.selector).length > 0)
				return;

			if($p.children($.filetypes.image.tag).length > 0)
				return;

			$p.prepend($($.filetypes.image.append.replace("#ADDRESS#", $p.attr("address"))));
		});


/*
		//downloads sem imagem
		$items = $results.find($.filetypes.download.selector).parent();

		$items.each(function(i, p)
		{
			var $p = $(p);

			if($p.children($.filetypes.image.tag).length == 0 && $p.parents(":first")[0].tagnName != "A")
				$p.prepend($($.filetypes.image.append.replace("#ADDRESS#", "")));
		});


*/

		$items = $results.find($.filetypes.video.selector).parent();

		$items.each(function(i, p)
		{

			var $p = $(p);

			var escape = $p.attr("address").replace(/([\-=\/\(])/g, "\\$1"); 

			$p.find("img").each(function(j, q)
			{
				var $q = $(q);

				if($q.siblings("a[href*=" + escape + "\\?type\\=video\\/mp4]").length == 0)
				{
					$q.siblings(":last").before($.filetypes.video.append.replace("#ADDRESS#", $p.attr("address")));			

					//	$q.after("<a href='" + $p.attr("address") + "' target='_blank' >play</a>");					
				}
			});
		});	



		$items = $results.find($.filetypes.image.selector).parent();

		$items.each(function(i, p)
		{
			var $p = $(p);

			var escape = $p.attr("address").replace(/([\-=\/\(])/g, "\\$1"); 

			$p.find("img").each(function(j, q)
			{
				var $q = $(q);

				if( $p.attr("address") != $q.parent().attr("address") && $q.siblings("a[href*=" + escape + "]").length == 0)
				{
					$q.siblings(":last").before($.filetypes.image.view.replace("#ADDRESS#", $p.attr("address")));			

					//	$q.after("<a href='" + $p.attr("address") + "' target='_blank' >play</a>");					
				}
			});
		});	



		$items = $results.find($.filetypes.web.selector).parent();

		$items.each(function(i, p)
		{
			var $p = $(p);

			var escape = $p.attr("address").replace(/([\-=\/\(])/g, "\\$1"); 

			$p.find("img").each(function(j, q)
			{
				var $q = $(q);

				if($q.siblings("a[href*=" + escape + "]").length == 0)
				{

					$q.siblings(":last").before($.filetypes.web.append.replace("#ADDRESS#", $p.attr("address")));			

					//	$q.after("<a href='" + $p.attr("address") + "' target='_blank' >play</a>");					
				}
			});
		});	



		$items = $results.find($.filetypes.download.selector).parent();

		$items.each(function(i, p)
		{
			var $p = $(p);

			var escape = $p.attr("address").replace(/([\-=\/\(])/g, "\\$1"); 


			var name = $p.children().children("div[value=qfOqLd3DmtK\\-GWzR4Tpvr8f2dS\\(IdVlJOgyPYV7aBUs\\=]").parent().children(":first").text();

			var extension = $p.children().children("div[value=BVqfSE6j3SkhIsG\\-qAIyls9XxqRDWCQcxSdNxvu0IU8\\=]").parent().children(":first").text();


			$p.find("img").each(function(j, q)
			{
				var $q = $(q);

				if($q.siblings("a[href*=" + escape + "]").length == 0)
				{

					$q.siblings(":last").before($.filetypes.download.append.replace("#ADDRESS#", $p.attr("address")).replace("#NAME#", name + extension) );			

					//	$q.after("<a href='" + $p.attr("address") + "' target='_blank' >play</a>");					
				}
			});
		});	

		


		$results.find("img").each(function(i, p)
		{
			$(p).click(function()
			{
				if(this.nextSibling.href)
					window.open(this.nextSibling.href);	
			})

			$(p).hover(function()
			{
				$(this.nextSibling).addClass("selected");
			},
			function()
			{
				$(this.nextSibling).removeClass("selected");
			});
		});

	},


}

})



	