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

	$.ajax({
		url: "createUserAvatar/" + "aaa",
		success: function(data)
		{
			var address = data;

			$.utils.cookies.create("user", address);

			$.user.userAddress = address;

			$.user.createUserAvatarOrSingup();


/*
			$.input.post ("bbb ccc", null, function(id)
			{
				$.input.post("ddd", id);
			});

			$.input.post ("eee", null);
			$.input.post ("eee", null);
*/
		},
		dataType: "text"});



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