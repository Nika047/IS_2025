
//Проверка на уникальность имени учетной записи
function checkAccountExists(name, oid) {
    var d = $.Deferred();

    $.ajax({
        url: "/Account/CheckExists",
        type: "POST",
        data: {
            userName: name,
            oid: oid
        },
        success: function (result) {
            if (result) {
                d.resolve();
            }
            else {
                d.reject("Значение должно быть уникальным");
            }
        },
        error: function (err) {
            d.reject(err);
        }
    });

    return d.promise();
}

function showUnreadMessagesCount(uri) {
    getUnreadMessagesCount(uri).then(data => {
        if (data === null)
            $('#msg_unread_counter').html('');
        else
            $('#msg_unread_counter').html(data.TotalUnreadCount !== 0 ? data.TotalUnreadCount : '');
    });
}

async function getUnreadMessagesCount(uri) {
    let response = await fetch(uri, { headers: { "Accept": "application/json", "Content-Type": "application/json" } })
        .catch(function (error) {
            return null;
        });

    let data = await response.json();
    return data;
}

function ToJavaScriptDate(value) {
    //alert(value);
    var dt = new Date(value);
    //alert(dt);
    //datetime: "full"
    //Globalize.formatDate(ToJavaScriptDate(Date), { raw: "yyyy/MM/dd" })
    return dt;
    /*
    var pattern = /Date\(([^)]+)\)/;
    var results = pattern.exec(value);
    var dt = new Date(parseFloat(results[1]));
    return (dt.getMonth() + 1) + "/" + dt.getDate() + "/" + dt.getFullYear();*/
}

function updateQueryParameter(uri, key, value) {
    var re = new RegExp("([?&])" + key + "=.*?(&|$)", "i");
    var separator = uri.indexOf('?') !== -1 ? "&" : "?";
    if (uri.match(re)) {
        return uri.replace(re, '$1' + key + "=" + value + '$2');
    }
    else {
        return uri + separator + key + "=" + value;
    }
}


async function saveSettings(url, key, state) {
    console.debug("save state", state);
    const response = await fetch(url, {
        method: "POST",
        headers: { "Accept": "application/json", "Content-Type": "application/json" },
        body: JSON.stringify({ key: key, data: JSON.stringify(state) })
    })
        .catch(error => {
            console.log("Ошибка записи настроек", error);
        });
}

function loadSettings(url, key) {
    console.log("loadSettings");
    var deferred = new $.Deferred;
    var storageRequestSettings = {
        url: url + '/GetUserSettings/' + key,
        //headers: {
        //    "Accept": "text/html",
        //    "Content-Type": "text/html"
        //},
        method: "GET",
        dataType: "json",
        success: function (data) {
            console.debug("settings load", data);
            var result;
            try {
                result = JSON.parse(JSON.parse(data));
            }
            catch {
                try {
                    result = JSON.parse(data);
                } catch (e) {
                    result = data
                }
            };
            deferred.resolve(result);
        },
        fail: function (error) {
            deferred.reject();
        }
    };

    $.ajax(storageRequestSettings);
    return deferred.promise();
}
