$(document).ready(function () {
    var url = @Url.Action("/Home/GetComments")
    $.getJSON(url, null, function (data) {
        console.log(data)


    });


});