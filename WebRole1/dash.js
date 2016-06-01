function startCrawling() { //Tells the Worker Role to begin crawling

            $.ajax({ //AJAX
                type: "POST",
                url: 'WebService1.asmx/startCrawling',
                data: null, 
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    {
                        alert("success " + response.d);
                    }
                },
                failure: function (response) {
                    alert(response.d);
                }
            });

        }

function stopCrawling() { //Tells the Worker Role to stop crawling

    $.ajax({ //AJAX
        type: "POST",
        url: 'WebService1.asmx/stopCrawling',
        data: null,
        contentType: "application/json; charset=utf-8",
        success: function (response) {
            {
                alert("stopped " + response.d);
            }
        },
        failure: function () {
            alert(response.d);
        }
    });

}


function clearIndex() { //Tells the Worker Role to clear queues

    $.ajax({ //AJAX
        type: "POST",
        url: 'WebService1.asmx/clearIndex',
        data: null,
        contentType: "application/json; charset=utf-8",
        success: function (response) {
            {
                alert("cleared " + response.d);
            }
        },
        failure: function () {
            alert(response.d);
        }
    });

}

function clearTable() { //Tells the Worker Role to clear tables

    $.ajax({ //AJAX
        type: "POST",
        url: 'WebService1.asmx/clearTable',
        data: null,
        contentType: "application/json; charset=utf-8",
        success: function (response) {
            {
                alert("cleared " + response.d);
            }
        },
        failure: function () {
            alert(response.d);
        }
    });

}

function getPageTitle() { //Retrieve statistics to display on the dashboard

    var trieSize = 0;
    var lastStr = "No titles added";

    $.ajax({ //AJAX to retrieve size of Trie (number of titles added)
        type: "GET",
        url: 'WebService2.asmx/getTitleCount',
        dataType: 'text',
        success: function (response) {
            {
                console.log(response);
                trieSize = response;
            }
        },
        failure: function () {
            alert(response.d);
        }
    });

    $.ajax({ //AJAX to retrieve last title added to the Trie
        type: "GET",
        url: 'WebService2.asmx/getLastTitle',
        dataType: 'text',
        success: function (response) {
            {
                console.log(response);
                lastStr = response;
            }
        },
        failure: function () {
            alert(response.d);
        }
    });

    $.ajax({ //AJAX to retrieve all other information to display on dashboard
        type: "POST",
        url: 'WebService1.asmx/getPageTitle',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (response) {
            {
                var answer = JSON.parse(response.d);
                var tenUrl = answer[4].split(',');
                var t = "<h2><b>Worker Role Status:</b> " + answer[0] + "</h2><h2><b>RAM: </b>" + answer[1] + "</h2><h2><b>CPU:</b> " + answer[2] + "</h2><h2><b>Number of Titles in Trie: </b>" + trieSize + "</h2><h2><b>Last Title Added to Trie: </b>" + lastStr + "</h2><h2><b>URLs Crawled: </b>" + answer[3] + "</h2><h2><b>Last 10 URLs:</b><br><ol type='1'>";
                for (var i = tenUrl.length - 1; i >= 0; i--)
                {
                    t += "<li>" + tenUrl[i] + "</li>";
                }
                t += "</ol></h2><h2><b>Queue Size:</b> " + answer[5] + "</h2><h2><b>Table Size:</b> " + answer[6] + "</h2><h2><b>Errors and Urls:</b><br><ul>";
                var erList = answer[7].split('$');
                for (var i = 0; i < erList.length; i++) {
                    t += "<li>" + erList[i] + "</li>";
                }
                t += "</ul></h2>";

                $("#stats").html(t); //Insert this HTML code
            }
        },
        failure: function (response) {
            alert(response.d);
        }
    });

}