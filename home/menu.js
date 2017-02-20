$(function() {

$.menu = {};

$.menu.MIME_TYPE_TEXT_STREAM = 'a3GOXtEDByHlwKI7aPuaAJwAM_wZq3bkQWB2H7QaL0E=';

$.menu.bind = function($item, item, video)
{
	var $menu = $('.menu:first', $item);

	var $current_time = $('.current_time', $menu);

	var $end_time = $('.end_time', $menu);

	var $video = $(video);

	if($video.is('.bound'))
	{
		return;
	}

	$video.addClass('bound');

	$video.bind('durationchange', function(e)
	{
		$end_time.text($.menu.secToTime(video.duration));
	});

	var $progress = $('.progress_bar', $menu);

	var $played = $('.progress_bar_played', $progress);

	var $toplay = $('.progress_bar_toplay', $progress);

	
	$video.bind('timeupdate', function(e)
	{
		$current_time.text($.menu.secToTime(video.currentTime));

		var perc = $video[0].currentTime / video.duration;

		$played.width(perc * 100 + "%");
	
		$toplay.width(100 - (perc * 100)  + "%");
	});

	$video.bind('ended', function(e)
	{
		var $play = $('.pause', $item);

		$play.removeClass('pause');

		$play.addClass('play');

		var $top =  $('.top', $item);

		$top.addClass('visible');
	});

	$progress.bind('mouseup', function(e)
	{
		var total = parseInt($progress.width());

		var offset = e.pageX - $progress.offset().left;

		video.currentTime = video.duration * (offset/ total);
	});

	

	var $more = $('.more', $menu);

	var $layout = $more.parents('.layout:first');

	$more.bind('click', function(e)
	{
		$('.rootitem > div', $layout).not(':empty').show();

		$('.subinput', $layout).show();

		$("#searchResults").isotope('layout');
	});

	var $full = $('.full', $menu);

	$full.bind('click', function(e)
	{
		var $elem = $('.firstpic_container');

		var $firstpic = $('.firstpic', $elem);

		var elem = $elem[0];

		var full = document.fullscreenElement ||    // alternative standard method
		  document.mozFullScreenElement || document.webkitFullscreenElement || document.msFullscreenElement;

		if(full)
		{
			$elem.css('max-width', '100%');
			$elem.height('auto');
			$elem.width('100%');
			$firstpic.width('100%');
			$firstpic.height('auto');

			elem.full = false;

			//document.webkitExitFullScreen();

			elem = document;

			if (elem.exitFullscreen) 
				elem.exitFullscreen();
			else if (elem.msExitFullscreen) 
				elem.msExitFullscreen();
			else if (elem.mozCancelFullScreen) 
				elem.mozCancelFullScreen();
			else if (elem.webkitExitFullscreen) 
				elem.webkitExitFullscreen();

		}
		else
		{
			if (elem.requestFullscreen) 
				elem.requestFullscreen();
			else if (elem.mozRequestFullScreen) 
				elem.mozRequestFullScreen();
			else if (elem.webkitRequestFullscreen) 
				elem.webkitRequestFullscreen();

			elem.full = true;

			$elem.css('max-width', '100%');

			$elem.width('100%');

			$firstpic.height('100%');

			setTimeout(function()
			{
				if($elem.height() > $(window).height())
				{
					$elem.css('width', '100%');
					$elem.css('height', '100%');

					$firstpic.width('100%');
					$firstpic.height('100%');
				}
			}, 100);
		}


		
			
	});

	if(typeof video == 'undefined')
		return;

	var $volume = $('.volume', $menu);

	var audio = video.nextElementSibling;

	var $audio = $(audio);

	$volume.bind('click', function(e)
	{
		audio.muted = !audio.muted;
	});

	var $volume_bar = $('.volume_bar', $menu);

	var $volume_left = $('.volume_bar_left', $volume_bar);

	var $volume_right = $('.volume_bar_right', $volume_bar);

	$audio.bind('volumechange', function(e)
	{
		$volume.removeClass (function (index, css) {
			return (css.match (/\bvolume_\S+/g) || []).join(' ');
		});

		if(audio.muted)
		{
			$volume.addClass('volume_0');

			$volume_left.css('background-color', "#999");

			return;
		}

		var t = Math.floor(Math.min((audio.volume * 3)+ 1, 3));

		$volume.addClass('volume_' + t );

		var perc = audio.volume / 1;

		$volume_left.width(perc * 100 + "%");
	
		$volume_right.width(100 - (perc * 100)  + "%");

		$volume_left.css('background-color', "red");
		
	});

	

	$volume_bar.bind('click', function(e)
	{
		var total = parseInt($volume_bar.width());

		var offset =  Math.min(e.pageX - $volume_bar.offset().left, total);

		audio.muted = false;

		audio.volume = offset/ total;
	});

	var $language = $('.language', $menu);

	$language.bind('click', function(e) 
	{
		e.stopPropagation();

		var $this = $(this).parents('.layout:first');

		var text = $this.attr("address");
		
		$.nav.items.empty();

		$.searchInput[0].context.start(text, $.menu.MIME_TYPE_TEXT_STREAM);
	})


}

$.menu.secToTime = function(sec)
{
	var t = new Date(1970,0,1);
		
	t.setSeconds(sec);

	return t.toTimeString().substr(0,8);
}

$.menu.play = function(e)
{
	e.stopPropagation();

	var $this = $(this);

	var $menu = $this.parents(".menu:first");

	var $top = $('.top', $menu);

	var $video = $menu.next();
	
	if($this.is('.play'))
	{
		$this.removeClass('play');

		$this.addClass('pause');

		$video[0].play();

		$top.removeClass('visible');
	}
	else
	{
		$this.removeClass('pause');

		$this.addClass('play');

		$video[0].pause();

		$top.addClass('visible');
	}
}


$.menu.playAudio = function()
{
	this.nextElementSibling.play();	
}

$.menu.pauseAudio = function()
{
	this.nextElementSibling.pause();	
}

$.menu.seekAudio = function()
{
	this.nextElementSibling.currentTime = this.currentTime;
}


$('.play', $.input.models.master).bind('click', $.menu.play);


$('video:first', $.input.models.master).bind('play', $.menu.playAudio);
$('video:first', $.input.models.master).bind('pause', $.menu.pauseAudio);
$('video:first', $.input.models.master).bind('seeked', $.menu.seekAudio);

});