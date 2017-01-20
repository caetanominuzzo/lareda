$(function () {

    $.input.post = function (text, target, callback) {
        $.ajax({
            url: "post/", // + encodeURI(text) + "/" + (target || "null") + "/" + ($.user.userAddress || ""),
            data:
            {
                post: text,
                target: target,
                user: $.user.userAddress
            },
            success: callback,
            dataType: "text",
            method: "POST"
        });
    }
})

