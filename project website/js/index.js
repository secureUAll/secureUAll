$(document).ready(function() {

    // Toggle video
    $(".changeVideo").click(function() {
        console.log($(this));
        $("#video").attr("src", $(this).data("video"));
        $(".changeVideo").removeClass("active");
        $(this).addClass("active");
    })

});