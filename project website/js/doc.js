const colors = {
    'colector': { // Coletor
        "color": "#0F1A3E",
        "dark": "#050915",
        "light": "#192b67"
    },
    'frontend': { // Frontend
        "color": "#B7458C",
        "dark": "#943871",
        "light": "#c05998"
    },
    'db': { // DB
        "color": "#C91FD8",
        "dark": "#a619b3",
        "light": "#d436e2"
    },
    'worker': { // Worker
        "color": "#34855A",
        "dark": "#2b6e4a",
        "light": "#40a56f"
    }
}

$(document).ready(function () {
    // M3
    // Gantt chart

    // Get log data from API
    $.ajax({
        type: "GET",
        url: "http://gsx2json.com/api?id=1bSYkXeiBynJszxFCmmmRslO4wSW1CU6rzLB0C15iErM&sheet=3",
        dataType: "json",
        success: function (data) {
            // data[rows] is object with keys: taskid, taskname, resource, start, end, durationmilliseconds, percentcomplete, dependencies
            drawChart(data['rows']);
            console.log("Got data!");
            console.log(data['rows']);
        }
    });

    // Draw chart
    google.charts.load('current', { 'packages': ['gantt'] });
    google.charts.setOnLoadCallback(drawChart);

    function daysToMilliseconds(days) {
        return days * 24 * 60 * 60 * 1000;
    }

    function drawChart(apiData) {

        var data = new google.visualization.DataTable();
        data.addColumn('string', 'Task ID');
        data.addColumn('string', 'Task Name');
        data.addColumn('string', 'Componente');
        data.addColumn('date', 'Start Date');
        data.addColumn('date', 'End Date');
        data.addColumn('number', 'Duration');
        data.addColumn('number', 'Percent Complete');
        data.addColumn('string', 'Dependencies');

        var rows = [];

        apiData.some((obj, ind) => {
            if( !obj['taskid'] ) {
                return null;
            }
            console.log(obj);
            rows.push([
                obj['taskid'],
                obj['tarefa'],
                obj['componente'],
                new Date(`${obj['start'].split("/")[2]}-${obj['start'].split("/")[1]}-${obj['start'].split("/")[0]}`),
                new Date(`${obj['end'].split("/")[2]}-${obj['end'].split("/")[1]}-${obj['end'].split("/")[0]}`),
                obj['durationmilliseconds'],
                obj['percentcomplete'],
                obj['dependencies']!=0 ? obj['dependencies'] : null
            ]);
        })

        data.addRows(rows);

        const labelsOrdered = apiData.map(obj => obj['componente']).filter((v, i, a) => a.indexOf(v) === i);
        const chartColors = labelsOrdered.map(label => {
            if (typeof(label)=="string") {
                const key = Object.keys(colors).filter(c => label.toLowerCase().indexOf(c.toLowerCase())>-1);
                if (key.length != 0) {
                    return colors[key];
                }
            }
        }).filter(c => c!=undefined);

        var options = {
            height: 45*rows.length,
            width: $("#accordionM3 .card-header").width(),
            gantt: {
                criticalPathEnabled: false,
                palette: chartColors
            }    
        };

        var chart = new google.visualization.Gantt(document.getElementById('schedule_chart'));

        chart.draw(data, options);
    }

});