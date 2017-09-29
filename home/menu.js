$(function() {

$.menu = {};

$.menu.MIME_TYPE_TEXT_STREAM = 'a3GOXtEDByHlwKI7aPuaAJwAM_wZq3bkQWB2H7QaL0E=';

$.menu.bind = function($item, item, video)
{
	var $menu = $('.menu:first', $item);

	var $lasting_time = $('.lasting_time', $menu);

	var $video = $(video);

	if($video.is('.bound'))
	{
		return;
	}

	$video.addClass('bound');

	$video.bind('durationchange', function(e)
	{
		$lasting_time.text($.menu.secToTime(video.duration));
	});

	var $progress = $('.progress_bar', $menu);

	var $played = $('.progress_bar_played', $progress);

	var $toplay = $('.progress_bar_toplay', $progress);

	var $loaded = $('.progress_bar_loaded', $progress);

	
	$video.bind('timeupdate', function(e)
	{
		/*
		$.ajax({
            url: "keepAlive/",
            data:
                {
                    files: [video.currentSrc, audio.currentSrc],
                },
            crossDomain: false,
            dataType: "json",
            method: "GET"
        });
*/

		if(!isFinite(video.duration))
			return;	

		var lasting = video.duration - video.currentTime;

		$lasting_time.text($.menu.secToTime(lasting));

		var perc = video.currentTime / video.duration;

		var currentBuffer = 0;

		var start = video.buffered.start(currentBuffer);

		var end = video.buffered.end(currentBuffer);

		var loaded = (video.buffered.end(0) - video.buffered.start(0)) / video.duration;

		while(video.currentTime < start || video.currentTime > end)
		{
			currentBuffer++;
			
			start = video.buffered.start(currentBuffer);

			end = video.buffered.end(currentBuffer);
		}

		var loaded = end / video.duration;

		loaded -= perc;

		$played.width(perc * 100 + "%");

		$loaded.width(loaded * 100 + "%");

		hideLoading();
	});

	var hideLoading = function(e)
	{
		var $top = $('.top', $menu);

		var $bottom = $('.bottom', $menu);

		var $video = $menu.next();

		var $play = $('.play', $menu);

		var $large_play = $('.large_loading', $menu);

		$large_play.hide();

		$large_play.removeClass('large_loading');

		$large_play.addClass('large_play');

	};
	

	$video.bind('canplay', hideLoading);



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
		var $top = $('.top', $menu);

		var $bottom = $('.bottom', $menu);

		var $video = $menu.next();

		var $play = $('.play', $menu);

		var $large_play = $('.large_play', $menu);

		$large_play.show();

		$large_play.removeClass('large_play');

		$large_play.addClass('large_loading');

		var setTime = function()
		{
			if(!isFinite(video.duration))
			{
				setTimeout(setTime, 100);

				return;
			}
			var total = parseInt($progress.width());

			var offset = e.pageX - $progress.offset().left;

			video.currentTime = video.duration * (offset/ total);

		}

setTime();
		

		
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

		var $language_options = $('.language_options', $language);

		var $subtitle_options = $('.language_options .subtitle_options', $language);

		var $audio_options = $('.language_options .audio_options', $language);

		$subtitle_options.empty();

		$audio_options.empty();

		var language_itens = [];

		item.subtitles.forEach(function(t){

			var $tt = $("<div class='language_options_item'>"+t.thumb_text+"</div>");

			$tt.bind('click', function(ttt)
			{
					var $parentPicContainer = $(ttt.target).closest(".firstpic_container");

					var $video = $('video', $parentPicContainer);

					$('track', $video).attr('mode', 'hidden');
					var ttrakk = $('track', $video)[0];

					var player = $video[0];


					let tracks = player.textTracks;

					for (let i = 0; i < tracks.length; i++) {
					  let track = tracks[i];

					  // find the captions track that's in english
					  if (track.label === t.thumb_text) {
					    track.mode = 'showing';
					  }
					  else
					  	track.mode = 'disabled';
					}

			});

			$subtitle_options.append($tt);

		});

		item.audios.forEach(function(t){

			var $ttt = $("<div class='language_options_item'>"+t.thumb_text+"</div>");

			$ttt.bind('click', function(ttt)
			{
				var $parentPicContainer = $(ttt.target).closest(".firstpic_container");

				$("audio:first", $parentPicContainer)[0].setAttribute('src', t.address);

				$("audio:first", $parentPicContainer)[0].load();				

				$("audio:first", $parentPicContainer)[0].currentTime = video.currentTime;


				if($('.play', $.input.models.master).is('.play'))
					$("audio:first", $parentPicContainer)[0].play();				

			});

			$audio_options.append($ttt);

		});

		$language_options.toggle();

		$(".timeline", $menu).toggle();


	});


}

$.menu.secToTime = function(sec)
{
	var t = new Date(1970,0,1);
		
	t.setSeconds(sec);

	return t.toTimeString().substr(0,8);
}

$.menu.play = function(e, t)
{
	e.stopPropagation();

	var $this = $(this);

	if(t)
		$this = $(t);
	
	if(typeof t == 'undefined' && $this.parents('.navItems').length == 0 && $this.is('.large_play'))
	{
		$.nav.parentsItemClick(e, $this.parents('.layout:first')[0]);

		return;		
	}

	

	var $menu = $this.parents(".menu:first");

	var $top = $('.top', $menu);

	var $bottom = $('.bottom', $menu);

	var $video = $menu.next();

	var $play = $('.play', $menu);

	var $large_play = $('.large_play', $menu);
	
	if($this.is('.play') || $this.is('.large_play'))
	{
		$menu.parent().find('>.firstpic_overdraw').fadeOut();
		
		$large_play.removeClass('large_play');

		$large_play.addClass('large_loading');

		$play.removeClass('play');

		$play.addClass('pause');

		var playPromise = $video[0].play();

		$top.removeClass('visible');

		$bottom.addClass('visible');

		//$large_play.hide();
	}
	else
	{
		$play.removeClass('pause');

		$play.addClass('play');

		$video[0].pause();

		$top.addClass('visible');

		$large_play.show();

		$bottom.removeClass('visible');
	}
}


$.menu.playAudio = function()
{
	this.nextElementSibling.load();
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