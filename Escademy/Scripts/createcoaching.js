$(document).ready(function () {
    var faqFactory = new FAQFactory($("#accordion1"));

    // Adds a new untilted F.A.Q
    $("#addFaq").click(function (e) {
        e.preventDefault();
        faqFactory.create("Untitled");
    });

    // Update's exisitng F.A.Q
    $("#faqAddExisting").click(function (e) {
        e.preventDefault();
        faqFactory.create($("#faqTitle").val(), $("#faqAnswer").val());

        $("#faqTitle").val("");
        $("#faqAnswer").val("");
    });
});