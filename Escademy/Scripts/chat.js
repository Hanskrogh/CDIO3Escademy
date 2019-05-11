/**
 * VORES BAGGRUNDS UPDATER CLASS.
 * BRUGER LIGE PT. AJAX TIL AT KALDE getUnreadMessages I UserController.cs
 * TIL AT OPDATERER VORES CHAT SYSTEM I BAGGRUNDEN.
 */
var background_msg_updater;
var to_user_id = 0;
var current_user_image = '';
var from_user_image = '';
var second_user_Fristname = '';
/**
 * NÅR JQUERY ER LOADED.
 */
var chatHub = $.connection.chatHub;
$(document).ready(function () {
    // Declare a proxy to reference the hub. 
    var chatHub = $.connection.chatHub;
    // Start Hub
    $.connection.hub.start().done(function () {
        //--Get Current Logged-In User Detail---
        $.get("/api/user/GetLoggedInUserDetail", null, function (dataUser) {
            if (dataUser.auth == "OK") {

                //--Hide the Message-type-box and Username(Header Text) during pageload--
                $("#txtMessageField").hide();
                $("#txtUsernameA").hide();
                //--Append the welcome message during the page load--
                $("#msgHistory").html('');
                $("#msgHistory").append('<div style="text-align:center;margin-top:200px;"><h2>Welcome ' + dataUser.UserDetail.FirstName + ' ' + dataUser.UserDetail.LastName + '</h2></div><div style="text-align: center;font-size: 22px;">Start Conversation</div>');

                //--Store Current Logged-In UserId and Username in the hidden fields--
                $("[id$=hdnCurrentUserID]").val(dataUser.UserDetail.Id);
                $("[id$=hdnCurrentUserName]").val(dataUser.UserDetail.FirstName + ' ' + dataUser.UserDetail.LastName);

                if (dataUser.UserDetail.Picture.length > 0) {
                    current_user_image = "data:image/png;base64," + dataUser.UserDetail.Picture;
                } else {
                    current_user_image = "https://ptetutorials.com/images/user-profile.png";
                }
                //--Connect the chathub event--
                registerEvents(chatHub);

                //-- Load All Users List except Current LoggedIn User--
                $.get("/api/user/GetAllContacts",null,
                function (data) {
                    if (data.auth == "OK") {
                        $.each(data.conversations, function (index, obj) {
                            var isPrimary = false;
                            var source_Pic = "";
                            if (obj.Profile.length > 0) {
                                source_Pic = "data:image/png;base64," + obj.Profile;
                            } else {
                                source_Pic = "https://ptetutorials.com/images/user-profile.png"; 
                            }
                            //--Create/Append Chat Element (Users Lint)
                            createChatElement(isPrimary, source_Pic, obj.FirstName + " " + obj.LastName, obj.Id, obj.TotalUnreadMessagesCount);
                        });

                            //.. Click Event execute when user will click on specific User from Users List ..
                        $(".chat_list").click(function () {

                            //--make Active of the selected user-box--
                                $(".chat_list").removeClass("active_chat");
                                $(this).addClass("active_chat");

                            //--Store the selected user-Id into the global variable--
                                to_user_id = $(this).attr("data-sender");

                                //--Get User Detail of selected User by Id--
                                $.get("/api/user/GetUserDetialById", { UserId: to_user_id }, function (dataToUser) {

                                    if (dataToUser.UserDetail.length > 0) {

                                        //--Show the Message-type-box--
                                        $("#txtMessageField").show();
                                        //--Show the selected user-name on the top Header--
                                        $("#txtUsernameA").html(dataToUser.UserDetail[0].FirstName + ' ' + dataToUser.UserDetail[0].LastName);
                                        $("#txtUsernameA").show();
                                        //--Store the selected User-name in the global variable--
                                        second_user_Fristname = dataToUser.UserDetail[0].FirstName;
                                        //--Store the selected User-Image in the global variable--
                                        if (dataToUser.UserDetail[0].ProfilePicture.length > 0) {
                                            from_user_image = "data:image/png;base64," + dataToUser.UserDetail[0].ProfilePicture;
                                        } else {
                                            from_user_image = "https://ptetutorials.com/images/user-profile.png";
                                        }
                                    }
                                    //--Get All previous messages of the selected User--
                                    chatHub.server.requestLastMessage($("[id$=hdnCurrentUserID]").val(), to_user_id);
                                });
                            });
                    }
                });
            }
         });
    });
    $("#txtMessageField").on('keyup', function (e) {
        //--Request the typing indicator--
        chatHub.server.sendUserTypingRequest(to_user_id);
        if (e.keyCode == 13) {//ENTER
            //--Send message after enter press to the selected user--
            chatHub.server.sendPrivateMessage(to_user_id, $("#txtMessageField").val());
            //--Hide the message-text-box--
            $("#txtMessageField").val('');
            //--Hide typing text--
            $("#dv_TypingIndicator").hide();
        }
    });
});

chatHub.client.ReceiveTypingRequest = function (userId) {
    //--Check if selected user is typing --
    if (userId == to_user_id) {
        $("#dv_TypingIndicator").html(second_user_Fristname + " is typing...");
        $("#dv_TypingIndicator").show();
        //--Hide typing text after 3 seconds--
        $("#dv_TypingIndicator").delay(3000).fadeOut("slow");
    }
}

function registerEvents(chatHub) {
    var UserName = $("[id$=hdnCurrentUserName]").val();
    var UserID = parseInt($("[id$=hdnCurrentUserID]").val());
    //--Connet with the chathub--
    chatHub.server.connect(UserName, UserID);
}

chatHub.client.sendPrivateMessage = function (ToUserId, from_userId, message, MessageSentDate, MessageId) {
   
    var outgoing_status = true;
    var user_image = current_user_image;
    //--Check if current logged-in user get the message--
    if (ToUserId == $("[id$=hdnCurrentUserID]").val()) {
        outgoing_status = false;
        user_image = from_user_image;
    }
    //--Check if new message from another user (not selected user)--
    if (from_userId != $("[id$=hdnCurrentUserID]").val() && from_userId != to_user_id) {
        //--Bind the total unread messages counter--
        var message_count = parseInt($("#chat_message_counter_" + from_userId).attr("data-Counter")) + 1;
        $("#chat_message_counter_" + from_userId).attr("data-Counter", message_count);
        $("#chat_message_counter_" + from_userId).html("<b>(" + message_count + ")</b>");
        $("#chat_list_" + from_userId).prependTo(".inbox_chat");
        $("#chat_message_counter_" + from_userId).show();
    }
    else {
        //--Bind the new message in the message-area (of selected user)--
        var messageObject = createChatMessage(outgoing_status, message, MessageSentDate, MessageId, user_image);
        $(".msg_history").append(messageObject);
        
        //--Check if message-from/sender is not current user--
        if (from_userId != $("[id$=hdnCurrentUserID]").val())
        {
            //--Update Message-Seen Status to 1 (means message has been seen by the receiver)--
            $.get("/api/user/UpdateMessageSeenStatus", { MessageId: MessageId }, function (dataMessageStatus) {
            });
        }
    }
    // Bottom Scroll
    var historyDiv = document.getElementById("msgHistory");
    historyDiv.scrollTop = historyDiv.scrollHeight;
}

chatHub.client.GetLastMessages = function (TouserID, CurrentChatMessages) {
    
    $("#msgHistory").html('');
    var outgoing_status = true;
    var user_image = '';

    $.each(CurrentChatMessages, function (index, obj) {
        if (obj.ToUserID == $("[id$=hdnCurrentUserID]").val()) {
            outgoing_status = false;
            user_image = from_user_image;
        }
        else {
            outgoing_status = true;
            user_image = current_user_image;
        }
        var messageObject = createChatMessage(outgoing_status, obj.Message, obj.SendDate , obj.Id , user_image);
        $(".msg_history").append(messageObject);
       
    });
    // Bottom Scroll
    var historyDiv = document.getElementById("msgHistory");
    historyDiv.scrollTop = historyDiv.scrollHeight;
   //--Empty the message-type-box--
    $("#txtMessageField").val();
    //--Set all unread messages seen of the selected user--
    $.get("/api/user/UpdateAllMessageSeenStatus", { ToUserid: $("[id$=hdnCurrentUserID]").val(), FromUserId: TouserID }, function (dataMessageSeen) {
        $("#chat_message_counter_" + TouserID).attr("data-Counter", "0");
        $("#chat_message_counter_" + TouserID).html('');
        $("#chat_message_counter_" + TouserID).hide();
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
function createChatElement(isActive, profileImg, name, sender_id, TotalUnreadMessages) {
    var arrNewMessageFromSender = [];
    var listDiv = $("<div></div>")
        .addClass("chat_list")
        .attr({ "data-sender": sender_id, "id": "chat_list_"+sender_id });

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

    if (TotalUnreadMessages == 0) {

        var chat_ib_date = $("<span></span>")
       .addClass("chat_message_counter")
   .attr({ "id": "chat_message_counter_" + sender_id, "data-Counter": TotalUnreadMessages });
        chat_ib_header.append(chat_ib_date);
    }
    else {
        arrNewMessageFromSender.push(sender_id);

        var chat_ib_date = $("<span></span>")
       .addClass("chat_message_counter")
            .html("<b>(" + TotalUnreadMessages + ")</b>")
   .attr({ "id": "chat_message_counter_" + sender_id, "data-Counter": TotalUnreadMessages, "display": "block" });
        chat_ib_header.append(chat_ib_date);
    }
  
    chat_ib.append(chat_ib_header);
 
    chatPeople.append(chat_img);
    chatPeople.append(chat_ib);

    listDiv.append(chatPeople);

    $(".inbox_chat").append(listDiv);

    //--Show new messages sender on top--
    if (arrNewMessageFromSender.length > 0) {
        $("#chat_list_" + arrNewMessageFromSender[0]).prependTo(".inbox_chat");
    }
    //return listDiv;
}
function createChatMessage(isOutgoing, message, date, messageId, fromUserImage) {
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
            .attr("src", fromUserImage) /*"https://ptetutorials.com/images/user-profile.png"*/
            .attr("alt", "");
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
        var sent_msg_img = $("<div></div>")
            .addClass("sent_msg_img");

        var sent_msg_img_content = $("<img></img>")
           .attr("src", fromUserImage) /*"https://ptetutorials.com/images/user-profile.png"*/
           .attr("alt", "");
        sent_msg_img.append(sent_msg_img_content);

        var sent_msg = $("<div></div>")
            .addClass("sent_msg");

        sent_msg.append(msg_paragraph);
        sent_msg.append(msg_timestamp);

        msgContainer.append(sent_msg_img);
        msgContainer.append(sent_msg);
    }

    return msgContainer;
}
function clearChatPane() {
    $(".msg_history").html("");
}
