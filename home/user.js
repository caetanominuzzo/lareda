$(function() {

$.user = {};

$.user.load = function()
{
	var user = $.utils.cookies.read("user");

	if(user)
		$.user.userAddress = user;

	$.user.createUserAvatarOrSingup();

	$("div#selectUserButton").click(function()
	{
		$.user.selectUser();
	});

	$("div#confirmSelectUserButton").click(function()
	{
		$.user.confirmCreateUser();
	});	

	$("div#loginButton").click($.user.login);

	$("div#logoutButton").click($.user.logout);
}

$.user.createUserAvatarOrSingup = function()
{
	if($("div#right img").length > 1)
	{
		$("div#right img:first").remove();
	}

	$("#loginInput").text("");

	var $img;

	if($.user.userAddress)
	{
		var picture = $.utils.cookies.read("picture");

		var $img = $("<img />", 
			{
				src: picture,
				class: "userPic",
				results : "userImgResult"
			});

		$("div#right").prepend($img);

		//$.context.create($img);

		$("div#userImgResult").html("");

		//$img[0].context.start($.user.userAddress);

		//$.input.search($("div#login input").val(), $img.attr("contextid"));

		

		$("div#loginButton").hide();

		$("div#logoutButton").show();
	}
	else
	{
		$("div#loginButton").show();

		$("div#logoutButton").hide();
	}
}

$.user.logout = function()
{
	$.user.userAddress = null;

	$("#loginInput").removeAttr("address");

	$.utils.cookies.erase("picture");

	$.utils.cookies.erase("user");

	$.user.createUserAvatarOrSingup();
}

$.user.login = function()
{
	$("div#login").show();
}

$.user.selectUser = function()
{
	$("div#login").hide();

	var address = $("#loginInput").attr("address");

	if(address)
	{
		$.user.userAddress = address;

		$.utils.cookies.create("user", address);

		$.user.createUserAvatarOrSingup();
	}
	else
	{
		$("span#newUserusername").text($("#loginInput").text());

		$("div#newUser").show();
	}
}

$.user.confirmCreateUser = function()
{
	$("div#newUser").hide();

	$.ajax({
		url: "createUserAvatar/" + $("#loginInput").text(),
		success: function(data)
		{
			var address = data;

			$.utils.cookies.create("user", address);

			$.user.userAddress = address;

			$.user.createUserAvatarOrSingup();
		},
		dataType: "json"});
}

$.user.getWelcomeKey = function()
{
	$.get("friendKey", null, function(data)
	{
		$("div#welcomeKeyText").text(data);

		$("div#welcomeKey").show();
	})
}

$.user.lookingForImage = function($results, $this)
{
	var escape = $this.attr("lookingfor").replace(/([\-=\/\(])/g, "\\$1"); 

	var $img = $results.find("div[address=" + escape + "]").parentsUntil($results).find("img");

	if($img.length > 0)
	{
		$this.attr("src", $img.attr("src"));

		$this.removeAttr("active");
	}
}


})

