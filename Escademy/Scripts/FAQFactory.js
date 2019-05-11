function FAQFactory(target) {
    var unique_identifier = 0;

    this.create = function (title, content, editable = true) {
        if (editable) {
            target.append(createFaqElementWithTitle(getIdentifier(unique_identifier++), title, title == "Untilted" ? "" : title, content));
        } else {
            target.append(createUneditableFaqElement(getIdentifier(unique_identifier++), title, content));
        }
    };

    this.get = function () {
        var faqs = [];
        target.children().each(function (i, e) {
            var title = $(this).find(".accordion-heading .accordion-toggle").text();
            var content = $(this).find("textarea").val();

            faqs.push({ Title: title, Description: content });
        });

        console.log(faqs);
        return faqs;
    }

    function createFaqElementWithTitle(id, title, inputTitle, inputContent) {
        /* Sample Output FAQ Element */
        //<div class="accordion-group">
        //    <div class="accordion-heading"><a class="accordion-toggle" data-toggle="collapse" data-parent="#accordion1" href="#@collapseId"><i class="fa fa-bars" aria-hidden="true"></i> English Coaching</a> </div>
        //    <div id="@collapseId" class="accordion-body collapse">
        //        <div class="accordion-inner">
        //            <div>
        //                <div class="form-group">
        //                    <input type="text" class="form-control" placeholder="English Coaching" />
        //                </div>
        //                <div class="form-group">
        //                    <textarea class="form-control" rows="4" placeholder="English Coaching"></textarea>
        //                    <div class="pull-left"><small class="form-text text-muted"><a href="#"><i class="fa fa-trash"></i>&nbsp; Delete</a></small></div>
        //                    <div class="pull-right"><small class="form-text text-muted">16 / 300 Characters</small></div>
        //                    <div class="flclear"></div>
        //                </div>
        //                <div class="text-right">
        //                    <button class="btn btn-white-grad btnsmall mr-3">Cancel</button>
        //                    <button class="btn btn-lrg-standard btnsmall">Update</button>
        //                </div>
        //            </div>
        //        </div>
        //    </div>
        //</div>
        /*****************************/

        //<div class="accordion-group">
        var accordionGroup = $("<div></div>")
            .addClass("accordion-group");

        //    <div class="accordion-heading">
        var accordionHeading = $("<div></div>")
            .addClass("accordion-heading");


        //        <a class="accordion-toggle" data-toggle="collapse" data-parent="#accordion1" href="#@collapseId">
        var accordionToggle = $("<a></a>")
            .addClass("accordion-toggle")
            .attr("data-toggle", "collapse")
            .attr("data-parent", "#accordion1")
            .attr("href", "#" + id);

        //            <i class="fa fa-bars" aria-hidden="true"></i> English Coaching
        var faBars = $("<i></i>")
            .addClass("fa")
            .addClass("fa-bars")
            .attr("aria-hidden", "true");

        //        </a> <!-- accordion-toggle -->
        accordionToggle.text(" " + title);
        accordionToggle.prepend(faBars);


        //    </div> <!-- accordion-heading -->
        accordionHeading.append(accordionToggle);

        //    <div id="@collapseId" class="accordion-body collapse">
        var accordionBody = $("<div></div>")
            .addClass("accordion-body")
            .addClass("collapse")
            .attr("id", id);

        //        <div class="accordion-inner">
        var accordionInner = $("<div></div>")
            .addClass("accordion-inner");

        //            <div>
        var accordionContainer = $("<div></div>");

        //                <div class="form-group">
        var formGroup1 = $("<div></div>")
            .addClass("form-group");

        //                    <input type="text" class="form-control" placeholder="English Coaching" />
        var formControl1 = $("<input></input>")
            .addClass("form-control")
            .addClass("faq-title")
            .attr("placeholder", "F.A.Q Title")
            .val(inputTitle);

        //                </div> <!-- form-group -->
        formGroup1.append(formControl1);

        //                <div class="form-group">
        var formGroup2 = $("<div></div>");

        //                    <textarea class="form-control" rows="4" placeholder="English Coaching"></textarea>
        var formControl2 = $("<textarea></textarea>")
            .addClass("form-control")
            .attr("placeholder", "F.A.Q Answer")
            .attr("rows", "4")
            .text(inputContent);

        //                    <div class="pull-left"><small class="form-text text-muted"><a href="#"><i class="fa fa-trash"></i>&nbsp; Delete</a></small></div>
        var pullLeft = $("<div></div>")
            .addClass("pull-left");
        var pullLeftSmall = $("<small></small>")
            .addClass("form-text")
            .addClass("text-muted");

        var pullLeftA = $("<a></a>")
            .attr("href", "#")
            .attr("data-delete-id", id)
            .click(deleteHandler);

        var pullLeftIFaTrash = $("<i></i>")
            .addClass("fa")
            .addClass("fa-trash");
        pullLeftA.html("&nbsp; Delete");
        pullLeftA.prepend(pullLeftIFaTrash);
        pullLeftSmall.append(pullLeftA);
        pullLeft.append(pullLeftSmall);

        //                    <div class="pull-right"><small class="form-text text-muted">16 / 300 Characters</small></div>
        var pullRight = $("<div></div>")
            .addClass("pull-right");
        var pullRightSmall = $("<small></small>")
            .addClass("form-text")
            .addClass("text-muted")
            .text("16 / 300 Characters");
        pullRight.append(pullRightSmall);

        //                    <div class="flclear"></div>
        var flClear = $("<div></div>")
            .addClass("flclear");

        //                </div> <!-- form-group -->
        formGroup2.append(formControl2);
        formGroup2.append(pullLeft);
        formGroup2.append(pullRight);
        formGroup2.append(flClear);


        //                <div class="text-right">
        var textRight = $("<div></div>")
            .addClass("text-right");

        //                    <button class="btn btn-white-grad btnsmall mr-3">Cancel</button>
        var btnCancel = $("<button></button>")
            .addClass("btn")
            .addClass("btn-white-grad")
            .addClass("btnsmall")
            .addClass("mr-3")
            .text("Cancel");

        //                    <button class="btn btn-lrg-standard btnsmall">Update</button>
        var btnUpdate = $("<button></button>")
            .addClass("btn")
            .addClass("btn-lrg-standard")
            .addClass("btnsmall")
            .text("Update")
            .click(updateHandler);

        //                </div> <!-- text-right -->
        //textRight.append(btnCancel);
        textRight.append(btnUpdate);

        //            </div> <!-- accordion-container -->
        accordionContainer.append(formGroup1);
        accordionContainer.append(formGroup2);
        accordionContainer.append(textRight);

        //        </div> <!-- accordion-inner -->
        accordionInner.append(accordionContainer);

        //    </div> <!-- accordion-body -->
        accordionBody.append(accordionInner);

        //</div> <!-- accordion-group -->
        accordionGroup.append(accordionHeading);
        accordionGroup.append(accordionBody);


        return accordionGroup;
    }

    function createUneditableFaqElement(id, title, content) {
        /* Sample Output FAQ Element */
        //<div class="accordion-group">
        //    <div class="accordion-heading"><a class="accordion-toggle" data-toggle="collapse" data-parent="#accordion1" href="#collapseOne">Nulla dignissim nibh at ipsum pharetra gravida.?</a> </div>
        //    <div id="collapseOne" class="accordion-body collapse">
        //        <div class="accordion-inner">
        //            All the Lorem Ipsum generators on the Internet tend to repeat predefined chunks as necessary, making this the first true generator on the Internet.
        //      </div>
        //    </div>
        //</div>
        /*****************************/

        //<div class="accordion-group">
        var accordionGroup = $("<div></div>")
            .addClass("accordion-group");

        //    <div class="accordion-heading"><a class="accordion-toggle" data-toggle="collapse" data-parent="#accordion1" href="#collapseOne">Nulla dignissim nibh at ipsum pharetra gravida.?</a> </div>
        var accordionHeading = $("<div></div>")
            .addClass("accordion-heading");
        var accordionToggle = $("<a></a>")
            .addClass("accordion-toggle")
            .attr("data-toggle", "collapse")
            .attr("data-parent", "#accordion1")
            .attr("href", "#" + id)
            .text(title);
        accordionHeading.append(accordionToggle);

        //    <div id="collapseOne" class="accordion-body collapse">
        var accordionBody = $("<div></div>")
            .addClass("accordion-body")
            .addClass("collapse")
            .attr("id", id);

        //        <div class="accordion-inner">
        var accordionInner = $("<div></div>")
            .addClass("accordion-inner")
            .text(content);
        
        //      </div> <!-- accordion-inner -->
        //    </div> <!-- accordion-body -->
        accordionBody.append(accordionInner);

        //</div> <!-- accordion-group -->
        accordionGroup.append(accordionHeading);
        accordionGroup.append(accordionBody);


        return accordionGroup;
    }

    var deleteHandler = function (e) {
        e.preventDefault();

        var deleteId = $(this).attr("data-delete-id");
        var toRemoveElement = $("#" + deleteId)
            .parent();

        toRemoveElement.remove();
    }
    var updateHandler = function (e) {
        e.preventDefault();

        var top_element = $(this)
            .parent()
            .parent()
            .parent()
            .parent()
            .parent(); // 5th parent


        var top_title = top_element.find(".accordion-heading a");
        var top_i = top_title.find("i");

        top_title.text(top_element.find(".faq-title").val());
        top_title.prepend(top_i);

    }

    function getIdentifier(UID) {
        return "faq_element_" + UID;
    }
}
