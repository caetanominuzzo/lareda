$(function()
{


	$("div.dialog").bind('mouseleave', function()
	{
		$(this).hide();
	})




	$.drag.start();

	$.input.start();

	$.searchInput = $("#searchInput");	

	

	$.input.appendInput($.searchInput);

	$.context.create($.searchInput, "main");

	$.searchInput.focus();

	$.user.load();


	$.nav.items.mouseleave(function(t)
		{
			$.nav.items.children().hide();


			$.nav.top = 100;
		});

	var count  = 0;

	var i = 0;

	//$.ajax({
	//url: "createUserAvatar/" + "aaa" + (i == 0 ? "" : i),
	//success: function(data)
	//{

		var address = null;//data;

				var f = function() { 
			
				$.input.post ("Filme" + count, address, function(id)
				{
					//$.input.post("home", id);

						if(count++ < max)
							f();
						else
						{

								{
									var count2  = 0;

									var f2 = function() { 
										{
										
										});

									};

									f2();


								});
						}
				});

			};
			
			f();




	//},
	//dataType: "text"});


			

	

	if(false)
	{
		$.ajax({
			url: "createUserAvatar/" + "aaa" + (i == 0 ? "" : i),
			success: function(data)
			{
				var address = data;

				$.utils.cookies.create("user", address);

				$.user.userAddress = address;

				$.user.createUserAvatarOrSingup();

				$.input.post ("bbb" + i, null, function(id)
				{
					$.input.post("ccc" + i, id);
				});

				$.input.post ("ddd" + i, null);

				$.input.post ("eee" + i, null);

			},
			dataType: "text"});
	
	}

	$.mouse = { x: -1, y: -1 };

    $(document).mousemove(function(event) {
        $.mouse.x = event.pageX;
        $.mouse.y = event.pageY;
    });

    String.prototype.endsWith = function(suffix) {
	    return this.indexOf(suffix, this.length - suffix.length) !== -1;
	};

});

if (window.jQuery) {
  (function($) {
    $.fn.AudioJS = function(options) {
      this.each(function() {
        AudioJS.setup(this, options);
      });
      return this;
    };
    $.fn.player = function() {
      return this[0].player;
    };
  })(jQuery);
}