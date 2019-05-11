$(document).ready(function () {
    $.ajax({
        url: "/api/transaction/GetBalance",
        success: function(data) {
            if (data.Auth == "OK") {
                $("#user_account_balance").text("$" + data.Balance.toFixed(2));
            }
        }
    });
});