function sendRequest() { //Every time the user types

            var ui = $('#searchArea').val().trim();
            $("#searchResults").empty(); //Clear any results from sendFinalRequest()
            $("#crawlerResults").empty();
            console.log(ui);
            $.ajax({ //AJAX
                type: "POST",
                url: 'WebService2.asmx/searchQ',
                data: JSON.stringify({search: ui}), //Pass in user input
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: OnSuccess,
                failure: function (response) {
                    alert(response.d);
                }
            });

            function OnSuccess(response) {
                var answer = JSON.parse(response.d);
                var t = "<p>";
                for (i = 0; i < answer.length; i++) { //Convert into HTML code
                    t = t + "<a style='color: white;' href='https://en.wikipedia.org/wiki/" + answer[i] + "'>" + answer[i] + "</a><br> ";
                }
                t = t + " </p> ";
                $("#jsonDiv").html(t); //Insert this HTML code
            }
        }

function sendFinalRequest() { //Only when the submit button is pressed
            
    $("#searchResults").empty();
    $('#crawlerResults').empty();
    $('#jsonDiv').empty();
    var term = $('#searchArea').val().trim();
          
    if (term.length > 0 && term != null) {
                
        //Search for an nba player
        $.ajax({
            crossDomain: true,
            type: "GET",
            contentType: "application/json; charset=utf-8",
            url: "http://ec2-54-187-242-42.us-west-2.compute.amazonaws.com/index.php",
            data: { text1: term },
            dataType: 'jsonp',
            jsonp: 'callback',
            success: function (data) {
                console.log("success");
                      
                /* Data returned:
                data['General_Team']
                data['General_GP'] 
                data['General_Min']
                data['FG_Pct']
                data['FG_M']
                data['FG_A']
                data['3PT_Pct']
                data['3PT_M']
                data['3PT_A']
                data['FT_Pct']
                data['FT_M']
                data['FT_A']
                data['Rebounds_Tot']
                data['Misc_Ast']
                data['Misc_TO']
                data['Misc_Stl']
                data['Misc_Blk']
                data['Misc_PF']
                data['Misc_PPG']*/

                $("#searchResults").html("<br><table><tr><th>Name</th><th>Team</th><th>Games Played</th><th>Minutes Per Game</th><th>FG Made</th><th>FG Attempted</th><th>FG %</th><th>3PT Made</th><th>3PT Attempted</th><th>3PT %</th><th>FT Made</th><th>FT Attempted</th><th>FT %</th><th>Off. Rebounds</th><th>Def. Rebounds</th><th>Total Rebounds</th><th>Assists</th><th>Turnovers</th><th>Steals</th><th>Blocks</th><th>Personal Fouls</th><th>PPG</th></tr><tr><td>" + data[0][0] + "</td><td>" + data[0][1] +"</td><td>"+ data[0][2] + "</td><td>"+ data[0][3] + "</td><td>" + data[0][4] + "</td><td>" + data[0][5] + "</td><td>" + data[0][6] + "</td><td>" + data[0][7] + "</td><td>" + data[0][8] + "</td><td>" + data[0][9] + "</td><td>" + data[0][10] + "</td><td>" + data[0][11] + "</td><td>" + data[0][12] + "</td><td>"+ data[0][13] + "</td><td>" + data[0][14] + "</td><td>" + data[0][15] + "</td><td>" + data[0][16] + "</td><td>" + data[0][17] + "</td><td>" + data[0][18] + "</td><td>" + data[0][19] + "</td><td>" + data[0][20] + "</td><td>"+ data[0][21] + "</td><td></tr></table>");

            },
            error: function (data) {
                console.log(data);
            }
        });

        var data = JSON.stringify({ //Since input must be JSON
            search: term
        });

        $.ajax({
            type: "POST",
            contentType: "application/json; charset=utf-8",          
            url: 'WebService1.asmx/searchTable',
            data: data, //We pass in the JSON here
            dataType: "json",
            success: function (data) {
                var results = JSON.parse(data.d);
                $.each(results, function (i, item) {
            
                    //Add each of the results to the crawler results
                    var url = results[i]["url"];
                    var title = results[i]["pageTitle"];
                    var date = results[i]["date"];
                    var pageText = results[i]["pageText"];
                    $("#crawlerResults").append("<br><div><b>" + title + ", (" + date + "), <a href='" + url + "'>Link</a></b></div>");

                    //Extra credit (bold body snippets)
                    var terms = term.split(" "); //Bold each word, not only when they exactly match user query
                    for (var t = 0; t < terms.length; t++)
                    {
                        var allCase = [terms[t].toLowerCase(), terms[t].toUpperCase(), (terms[t].charAt(0).toUpperCase() + terms[t].slice(1).toLowerCase())]; //Ignore uppercase, lowercase, and capital first letter
                        for (var c = 0; c < allCase.length; c++) {
                            var boldSplit = pageText.split(allCase[c]); //Split the results based on query
                            var withBold = "";
                            for (var i = 0; i < boldSplit.length - 1; i++) //If the page text does not contain this term we will skip this loop
                            {
                                withBold += (boldSplit[i] + "<b>" + allCase[c] + "</b>");
                            }
                            withBold += boldSplit[boldSplit.length - 1];
                            pageText = withBold;
                        }
                    }
                    var boldUnify = ("<div>" + pageText + "</div>");
                    $("#crawlerResults").append(boldUnify);                            
                });
            },
            error: function (data) {
                console.log(data);
            }
        });
    }
}

function downWiki() { //Download the Wiki file from Blob storage

    $.ajax({
        type: "POST",
        contentType: "application/json; charset=utf-8",
        url: "WebService2.asmx/downloadWiki",
        success: function (data) {
            console.log("downloadWiki Success Result : ");
            console.log(data);
            alert("Downloaded!");
        },
        error: function (data) {
            console.log("downloadWiki Failure Result : ");
            console.log(data);
        }
    })
}

function buildTree() { //Build the Trie from the downloaded Wiki file (must call downWiki() first!)

    $.ajax({
        type: "POST",
        contentType: "application/json; charset=utf-8",
        url: "WebService2.asmx/buildTree",
        success: function (data) {
            console.log("downloadWiki Success Result : ");
            console.log(data);
            alert("Tree Built!");
        },
        error: function (data) {
            console.log("downloadWiki Failure Result : ");
            console.log(data);
        }
    })
}