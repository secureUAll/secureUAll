/**
 * Using http://gsx2json.com/ to convert Google Spread to JSON
 * 
 * Spread at https://docs.google.com/spreadsheets/d/1bSYkXeiBynJszxFCmmmRslO4wSW1CU6rzLB0C15iErM/edit?usp=sharing
 */

$(document).ready(function () {

    function AppViewModel() {
        var self = this;

        self.logs = ko.observable(null);

        $.ajax({
            type: "GET",
            url: "http://gsx2json.com/api?id=1bSYkXeiBynJszxFCmmmRslO4wSW1CU6rzLB0C15iErM&sheet=1",
            dataType: "json",
            success: function (data) {
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
                    // Add activity to week activities array
                    // If valid
                    if (row['activity']!=0 && row['beggining']!=0 && row['end']!=0) {
                        log[row['week']]['activities'].push({
                            'name': row['activity'],
                            'beginning': row['beginning'],
                            'end': row['end'],
                            'day': row['day'],
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
            }
        });
    }

    ko.applyBindings(new AppViewModel(), document.getElementById("ko"));

});

