/**
 * Using http://gsx2json.com/ to convert Google Spread to JSON
 * 
 * Spread at https://docs.google.com/spreadsheets/d/1bSYkXeiBynJszxFCmmmRslO4wSW1CU6rzLB0C15iErM/edit?usp=sharing
 */

function hourSpanToMinutes(day, h1, h2) {
    // Expecting day like DD/MM/YYYY and hours like HH:MM
    // Returns the number of hours between h1 and h2
    h1date = new Date(day.split("/")[2],day.split("/")[1]-1,day.split("/")[0],h1.split(":")[0], h1.split(":")[1]);
    h2date = new Date(day.split("/")[2],day.split("/")[1]-1,day.split("/")[0],h2.split(":")[0], h2.split(":")[1]);
    return  Math.abs(h1date - h2date) / 36e5;
}

$(document).ready(function () {

    function AppViewModel() {
        var self = this;

        self.logs = ko.observable(null);
        self.commitsDate = ko.observable(null);
        self.PRsDate = ko.observable(null);

        // Get log data from API
        $.ajax({
            type: "GET",
            url: "http://gsx2json.com/api?id=1bSYkXeiBynJszxFCmmmRslO4wSW1CU6rzLB0C15iErM&sheet=1",
            dataType: "json",
            success: function (data) {
                // Work count 
                activities = 0;
                hours = 0;

                // Convert data[rows] structure
                // From [{ week: , period: , activity: , beginning: , end:  }* ]
                // To { <weekName>: { period: , activities: { name:, beginning: , end:  }* }
                log = {}
                // Foreach row, add to log
                data['rows'].forEach(row => {
                    // Add week to log if not there yet
                    if(!(row['week'] in log)){
                        log[row['week']] = {
                            'period': row['period'],
                            'activities': []
                        }
                    }
                    // If valid
                    if (row['activity']!=0 && row['beggining']!=0 && row['end']!=0) {
                        // Update work count
                        activities += 1;
                        hours += hourSpanToMinutes(row['day'], row['beginning'], row['end']);
                        // Add activity to week activities array
                        log[row['week']]['activities'].push({
                            'name': row['activity'],
                            'beginning': row['beginning'].replace(":", "h"),
                            'end': row['end'].replace(":", "h"),
                            'day': row['day'].split("/")[0] + "/" + row['day'].split("/")[1],
                            'description': row['description']
                        });
                    }
                });
                // Make log an array with stucture
                // [ { week: <weekName>, period: <weekPeriod>, activities: [] }* ]
                logs = [];
                Object.entries(log).forEach(([k, v], i) => {
                    logs.push({
                        'week': k,
                        'period': v['period'],
                        'activities': v['activities']
                    })
                });
                // console.log(logs);
                self.logs(logs);

                // Update work count
                $("#animateMeetings").animateNumber(
                    { number: activities },
                    { easing: 'swing', duration: 1000}
                );
                $("#animateHours").animateNumber(
                    { number: Math.round(hours) },
                    { easing: 'swing', duration: 1000}
                );
            }
        });

        // Get commits data from API
        $.ajax({
            type: "GET",
            url: "http://gsx2json.com/api?id=1bSYkXeiBynJszxFCmmmRslO4wSW1CU6rzLB0C15iErM&sheet=2",
            dataType: "json",
            success: function (data) {
                self.commitsDate(data['rows'][data['rows'].length -1]['date']);
                $("#animateCommits").animateNumber(
                    { number: data['rows'][data['rows'].length -1]['commits'] },
                    { easing: 'swing', duration: 1000}
                );
            }
        });

        // Get commits data from API
        $.ajax({
            type: "GET",
            url: "http://gsx2json.com/api?id=1bSYkXeiBynJszxFCmmmRslO4wSW1CU6rzLB0C15iErM&sheet=3",
            dataType: "json",
            success: function (data) {
                self.PRsDate(data['rows'][data['rows'].length -1]['date']);
                $("#animatePRs").animateNumber(
                    { number: data['rows'][data['rows'].length -1]['pullrequests'] },
                    { easing: 'swing', duration: 1000}
                );
            }
        });
    }

    ko.applyBindings(new AppViewModel(), document.getElementById("ko"));

});

