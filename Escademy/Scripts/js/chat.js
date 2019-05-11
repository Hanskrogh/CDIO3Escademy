/**
 * VORES BAGGRUNDS UPDATER CLASS.
 * BRUGER LIGE PT. AJAX TIL AT KALDE getUnreadMessages I UserController.cs
 * TIL AT OPDATERER VORES CHAT SYSTEM I BAGGRUNDEN.
 */
var background_msg_updater;

/**
 * NÅR JQUERY ER LOADED.
 */
$(document).ready(function () {
    hookSendChatEvents();

    // Create new message parameter
    var messageTo = parseInt(findGetParameter("msgto"));
    var isNewContact = true;

    // Load conversations
    $.ajax({
        url: "/api/user/GetConversations"
    }).done(function (data) {
        if (data.auth == "OK") {
            $.each(data.conversations, function (index, obj) {
                var isPrimary = false;
                if (!messageTo) {
                    isPrimary = index == 0;
                } else if (parseInt(obj.Sender_id) == messageTo) {
                    isPrimary = true;
                    isNewContact = false;
                    console.log("NOT PRIMARY!");
                }

                var source_Pic = "";
                if (obj.Profile.length > 0) {
                    source_Pic = "data:image/png;base64," + obj.Profile;
                } else {
                    source_Pic = "https://ptetutorials.com/images/user-profile.png";
                }

                var chatElement = createChatElement(isPrimary, source_Pic, obj.FirstName + " " + obj.LastName, new Date(obj.Created_at).toLocaleDateString(), obj.Message, obj.Sender_id);
                $(".inbox_chat").append(chatElement);
            });

            if (data.conversations.length > 0) {
                if (!messageTo) {
                    loadFullConversation(data.conversations[0].Sender_id);
                }

                //.. convo event hooking ..
                $(".chat_list").click(function () {
                    if (!$(this).hasClass("active_chat")) {
                        $(".active_chat").removeClass("active_chat");
                        $(this).addClass("active_chat");

                        loadFullConversation(parseInt($(this).attr("data-sender")));

                    }
                });
            }

            // Skriv ny besked..
            if (messageTo) {
                if (isNewContact) {
                    var chatElement = createChatElement(true, "https://ptetutorials.com/images/user-profile.png", "Ny Besked", new Date().toLocaleDateString(), "Sender en ny besked", messageTo);
                    $(".inbox_chat").prepend(chatElement);
                }

                loadFullConversation(messageTo);
            }
        }
    });
});

/** 
 * RETUNERER $_GET PARAMETERNE
 */
function findGetParameter(parameterName) {
    var result = null,
        tmp = [];
    var items = location.search.substr(1).split("&");
    for (var index = 0; index < items.length; index++) {
        tmp = items[index].split("=");
        if (tmp[0] === parameterName) result = decodeURIComponent(tmp[1]);
    }
    return result;
}

/**
 * KALDES KUN 1 GANG. 
 * BRUGES TIL AT KÆDE sendMessageTo SAMMEN MED ENTER OG SEND KNAPPEN!
 */
function hookSendChatEvents() {
    function sendContents() {
        var txtField = $("#txtMessageField");

        var receiver_id = parseInt(txtField.attr("data-receiver-id"));
        var text = txtField.val();

        sendMessageTo(receiver_id, text);
        txtField.val("");

        // Bottom Scroll
        var historyDiv = document.getElementById("msgHistory");
        historyDiv.scrollTop = historyDiv.scrollHeight;
    }

    //.. event hooking ..
    $("#btnMessageField").click(sendContents);
    $("#txtMessageField").on('keyup', function (e) {
        if (e.keyCode == 13) {//ENTER
            sendContents();
        }
    });
}

/**
 * AJAX CHAT
 */
function loadFullConversation(sender_id) {
    $.ajax({
        url: "/api/user/GetFullConversation?sender_id=" + sender_id
    }).done(function (data) {
        clearChatPane();

        var heading = $(".active_chat .chat_ib h5").text();
        var dateStr = $(".active_chat .chat_ib h5 .chat_date").text();

        $("#txtUsernameA").text(heading.substring(0, heading.length - dateStr.length));

        if (data.auth == "OK") {
            $.each(data.messages, function (index, obj) {
                var messageObject = createChatMessage(sender_id != obj.Sender_id, obj.Message, obj.Created_at, obj.Id);
                $(".msg_history").append(messageObject);
            });

            // Sæt sender ID som vi bruger til sende beskeder med. Data-sender-id er receiver id af den man vil chatte med
            $("#txtMessageField").attr("data-receiver-id", sender_id);

            // Bottom Scroll
            var historyDiv = document.getElementById("msgHistory");
            historyDiv.scrollTop = historyDiv.scrollHeight;

            // deklarerer vores msg_updater class og starter den.
            if (background_msg_updater) {
                background_msg_updater.stopListening();
            }
            else {
                background_msg_updater = new backgroundChatUpdater(500);
            }

            background_msg_updater.listenTo(sender_id);
            //background_msg_updater.stopListening();//TODO: Remove
        }
    });
}
function backgroundChatUpdater(refreshRate) {
    var dynamic_conv_handle;
    var dynamic_chat_handle;
    var dynamic_chat_id;
    
    //---------------------------------------------------------------------------------------
    // TODO: Kun hvis vi har tid, så replace ajax pings med websockets for chat performance -
    //---------------------------------------------------------------------------------------
    this.listenTo = function (conv_id) {
        this.stopListening();
        dynamic_chat_id = conv_id;

        dynamic_chat_handle = setInterval(function () {
            $.ajax({
                url: "/api/user/GetUnreadMessages?sender_id="+dynamic_chat_id
            }).done(function (data) {
                if (data.size > 0) {
                    $.each(data.messages, function (index, message) {
                        if (!checkIfMessageIsDuplicate(message.Id)) {
                            var messageObject = createChatMessage(false, message.Message, message.Created_at, message.Id);
                            $(".msg_history").append(messageObject);
                            $($(".chat_list.active_chat .chat_ib p")[0]).text(message.Message);

                            var historyDiv = document.getElementById("msgHistory");
                            historyDiv.scrollTop = historyDiv.scrollHeight;
                        }
                    });
                }
            });
            
        }, refreshRate);

        dynamic_conv_handle = setInterval(this.updateConversations, refreshRate*10);
    };

    this.updateConversations = function () {
        $.ajax({
            url: "/api/user/GetConversations"
        }).done(function (data) {
            if (data.auth == "OK") {
                $.each(data.conversations, function (index, obj) {
                    var sender = parseInt(obj.Sender_id);
                    var sender_found = false;

                    // Is this sender already in conversations?
                    $(".chat_list").each(function (convoElemIndex, convoElem) {
                        if (parseInt($(convoElem).attr("data-sender")) == sender) {
                            sender_found = true;

                            if (obj.Profile.length > 0) {
                                $(this).find(".chat_img img").attr("src", "data:image/png;base64," + obj.Profile);
                            }

                            $(this).find(".chat_ib h5").text(obj.FirstName + " " + obj.LastName);

                            var chat_ib_date = $("<span></span>")
                                .addClass("chat_date")
                                .text(new Date(obj.Created_at).toLocaleDateString());

                            $(this).find(".chat_ib h5").append(chat_ib_date);
                            $(this).find(".chat_ib p").text(obj.Message);
                        }
                    });

                    // Fuck it
                    var heading = $(".active_chat .chat_ib h5").text();
                    var dateStr = $(".active_chat .chat_ib h5 .chat_date").text();
                    $("#txtUsernameA").text(heading.substring(0, heading.length - dateStr.length));

                    // New conversation! Create it
                    if (!sender_found) {
                        var source_Pic = "";
                        console.log(obj);
                        if (obj.Profile.length > 0) {
                            source_Pic = "data:image/png;base64," + obj.Profile;
                        } else {
                            source_Pic = "https://ptetutorials.com/images/user-profile.png";
                        }

                        var chatElement = createChatElement(false, source_Pic, obj.FirstName + " " + obj.LastName, new Date(obj.Created_at).toLocaleDateString(), obj.Message, obj.Sender_id);
                        $(chatElement).click(function () {
                            if (!$(this).hasClass("active_chat")) {
                                $(".active_chat").removeClass("active_chat");
                                $(this).addClass("active_chat");
                                loadFullConversation(parseInt($(this).attr("data-sender")));
                            }
                        });

                        $(".inbox_chat").prepend(chatElement);
                    }
                });

            }
        });

    };

    this.stopListening = function () {
        clearInterval(dynamic_chat_handle);
        clearInterval(dynamic_conv_handle);
    };

    var checkIfMessageIsDuplicate = function (messageId) {
        var foundDuplicate = false;
        $(".msg_history").children().each(function (index, element) {

            if (parseInt($(element).attr("data-message-id")) == parseInt(messageId)) {
                foundDuplicate = true;
            }
        });
        return foundDuplicate;
    };
}

function sendMessageTo(receiverId, text) {
    var messageObject = createChatMessage(true, text, new Date(), -1);
    $(".msg_history").append(messageObject);

    $.ajax({
        url: "/api/user/SendMessage",
        type: "get",
        data: {
            Receiver_id: receiverId,
            message: text
        }
    }).done(function () {
        if ($(".active_chat .chat_ib p").text() == "Sender en ny besked") {
            background_msg_updater.updateConversations();
        }
    });
}

/**
 * HJÆLPER FUNKTIONER
 */
function formatAMPM(date) {
    var hours = date.getHours();
    var minutes = date.getMinutes();
    var ampm = hours >= 12 ? 'pm' : 'am';
    hours = hours % 12;
    hours = hours ? hours : 12; // the hour '0' should be '12'
    minutes = minutes < 10 ? '0' + minutes : minutes;
    var strTime = hours + ':' + minutes + ' ' + ampm;
    return strTime;
}

/**
 * HTML CRUD FUNKTIONER
 */
function createChatElement(isActive, profileImg, name, date, message, sender_id) {
    var listDiv = $("<div></div>")
        .addClass("chat_list")
        .attr("data-sender", sender_id);

    if (isActive) listDiv.addClass("active_chat");

    var chatPeople = $("<div></div>").addClass("chat_people");
    var chat_img = $("<div></div>").addClass("chat_img");
    var chat_img_content = $("<img></img>")
        .attr("src", profileImg)
        .attr("alt", name);
    chat_img.append(chat_img_content);

    var chat_ib = $("<div></div>")
        .addClass("chat_ib");

    var chat_ib_header = $("<h5></h5>")
        .text(name);
    var chat_ib_date = $("<span></span>")
        .addClass("chat_date")
        .text(date);
    chat_ib_header.append(chat_ib_date);

    var chat_ib_content = $("<p></p>")
        .text(message);

    chat_ib.append(chat_ib_header);
    chat_ib.append(chat_ib_content);

    chatPeople.append(chat_img);
    chatPeople.append(chat_ib);

    listDiv.append(chatPeople);
    return listDiv;
}
function createChatMessage(isOutgoing, message, date, messageId) {
    var parsedDate = new Date(date);
    const monthNames = ["January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    var msgContainer = $("<div></div>")
        .addClass(isOutgoing ? "outgoing_msg" : "incoming_msg")
        .attr("data-message-id", messageId);

    var msg_timestamp = $("<span></span>")
        .addClass("time_date")
        .text(" " + formatAMPM(parsedDate) + "    |    " + monthNames[parsedDate.getMonth()] + " " + parsedDate.getDate());

    var msg_paragraph = $("<p></p>")
        .text(message);

    if (!isOutgoing) { // incoming_msg
        var incoming_msg_img = $("<div></div>")
            .addClass("incoming_msg_img");
        var incoming_msg_img_content = $("<img></img>")
            .attr("src", $(".active_chat").find("img").attr("src")) /*"https://ptetutorials.com/images/user-profile.png"*/
            .attr("alt", "Sunil");
        incoming_msg_img.append(incoming_msg_img_content);

        var received_msg_container = $("<div></div>")
            .addClass("received_msg");

        var received_msg = $("<div></div>")
            .addClass("received_withd_msg");

        received_msg.append(msg_paragraph);
        received_msg.append(msg_timestamp);

        received_msg_container.append(received_msg);

        msgContainer.append(incoming_msg_img);
        msgContainer.append(received_msg_container);
    } else {
        var sent_msg = $("<div></div>")
            .addClass("sent_msg");

        sent_msg.append(msg_paragraph);
        sent_msg.append(msg_timestamp);

        msgContainer.append(sent_msg);
    }

    return msgContainer;
}
function clearChatPane() {
    $(".msg_history").html("");
}
