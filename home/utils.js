$(function() {

$.utils = {};

$.utils.generateAddress = function()
{
	var result = [];

	for(var i = 0; i < 32; i++)
		result.push( String.fromCharCode( Math.floor(Math.random() * 256)) )

	return btoa(result.join("")).replace(/\//g, '_').replace(/\+/g, '-').replace(/\=/g, '=');
}

$.decodeHtmlDiv = document.createElement('div');

$.utils.decodeHtml = function(str)
{
    if(str && typeof str === 'string') {
      str = str.replace(/<\/?\w(?:[^"'>]|"[^"]*"|'[^']*')*>/gmi, '');
      $.decodeHtmlDiv.innerHTML = str;
      str = $.decodeHtmlDiv.textContent;
      $.decodeHtmlDiv.textContent = '';

    return str;
  }

}

$.utils.addressToColorBytes = function(b64Data) {
    var sliceSize = 3;

    var byteChars = atob(b64Data.replace(/_/g, '/').replace(/\-/g, '+').replace(/\=/g, '='));

	var slice = byteChars.slice(0, sliceSize);

	var byteNumbers = new Array(slice.length);

	for (var i = 0; i < slice.length; i++) {
		byteNumbers[i] = slice.charCodeAt(i);
	}

	var byteArray = new Uint8Array(byteNumbers);

	
	
	return byteArray;
}

var rxBase64 = /^(?:[A-Za-z0-9\(\-]{4})*(?:[A-Za-z0-9\(\-]{2}==|[A-Za-z0-9\(\-]{3}=)?$/;

$.utils.isBase64Address = function(base64)
{
    return base64 != null && base64.length == 44 && rxBase64.test(base64);
}

$.utils.cookies = 
{
    create : function(name, value, days)
    {
        var expires;

        if (days)
        {
            var date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = "; expires=" + date.toGMTString();
        } else 
            expires = "";

        document.cookie = escape(name) + "=" + escape(value) + expires + "; path=/";
    },

    read : function(name)
    {
        var nameEQ = escape(name) + "=";
        var ca = document.cookie.split(';');
        for (var i = 0; i < ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0) === ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) === 0) return unescape(c.substring(nameEQ.length, c.length));
        }
        return null;
    },


    erase : function(name)
    {
        $.utils.cookies.create(name, "", -1);
    }
}

})