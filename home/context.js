$(function () {

$.context = {};

$.context.minInterval = 50; //50

$.context.maxInterval = 20000; //bound to the time of the search results cache

$.context.create = function ($target, mode, parent)
{
    var target = $target[0];

    target.context =
    {
        interval: $.context.minInterval,

        id: $.utils.generateAddress(),

        mode: mode,

        parent: parent,

        process: function () {
            
            target.context.interval = Math.min(target.context.interval * 2, $.context.maxInterval);

            clearTimeout(target.context.timeoutObject);

            if ($(target).is(":visible"))
                $.ajax({
                    url: "context/",
                    data:
                        {
                            context: target.context.id,
                        },
                    success: target.context.callback,
                    error: target.context.callbackError,
                    crossDomain: false,
                    dataType: "json",
                    method: "POST"
                });
        },

        callbackError: function(data)
        {
            //alert('error');
            if(data.length > 0)
            {
                $("body").text(data.responseText);


                debugger;
                target.context.timeoutObject = setTimeout(target.context.process, target.context.interval);
            }
        },

        callback: function (data)
        {
           
            var anyChange = (data.length > 0);

            if(anyChange)
            {
                target.context.interval = $.context.minInterval;

                target.context.process(); //todo: alternar com a linha de baixo
                //target.context.timeoutObject = setTimeout(target.context.process, target.context.interval);            

                $.input.searchCallback(data, target);

                if(typeof target.context.pos_callback != 'undefined')
                    target.context.pos_callback(data, target);
            }
            else
                target.context.timeoutObject = setTimeout(target.context.process, target.context.interval);            

            
        },

        stop: function () {
            clearTimeout(target.context.timeoutObject);
        },

        start: function (term, mode, pos_callback) {
            target.context.stop();

            target.context.interval = $.context.minInterval;

            target.text = term;

            target.context.pos_callback = pos_callback;

            if(typeof mode != 'undefined')
                target.context.mode = mode;

            $.input.search(target);

            setTimeout(target.context.process, target.context.minInterval);

            if(target.parent)
            {
                target.parent.context.start(target.parent.context.term);
            }
            
        }

    };

    target.context.timeoutObject = setTimeout(target.context.process, target.context.interval);

    target.setAttribute("contextid", target.context.id);
}

})
