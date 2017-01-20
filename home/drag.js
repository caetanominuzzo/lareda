$(function () {

    $.drag = {};

    $.drag.start = function () {
        var dragging = $(window);

        dragging.bind("dragover", $.drag.onDragOver);

        dragging.bind("dragleave", $.drag.onDragOver);
    }

    $.drag.onDragOver = function (e) {
        e.stopPropagation();

        e.preventDefault();

        $.drag.showDraggers();

        setTimeout(function() {
            $.get(e.type + "/" + $.utils.generateAddress() + "/" + ($.user.userAddress || ""));
        }, 100);

    }

    $.drag.showDraggers = function () {
        $("div.field")
    }
})
