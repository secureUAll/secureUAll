const colors = {
    'colector': { // Coletor
        "color": "#4D4D4D",
        "dark": "#333333",
        "light": "#4D4D4D"
    },
    'frontend': { // Frontend
        "color": "#92D400",
        "dark": "#7AB300",
        "light": "#92D400"
    },
    'db': { // DB
        "color": "#578000",
        "dark": "#344d00",
        "light": "#578000"
    },
    'workers': { // Worker
        "color": "#1a1a1a",
        "dark": "#000000",
        "light": "#1a1a1a"
    },
    'today': {
        "color": "#0F1A3E",
        "dark": "#050915",
        "light": "#192b67"
    }
}

// Draw chart
google.charts.load('current', { 'packages': ['gantt'] });
google.charts.setOnLoadCallback(drawChart);

function drawChart(apiData, elId, width) {

    var data = new google.visualization.DataTable();
    data.addColumn('string', 'Task ID');
    data.addColumn('string', 'Task Name');
    data.addColumn('string', 'Componente');
    data.addColumn('date', 'Start Date');
    data.addColumn('date', 'End Date');
    data.addColumn('number', 'Duration');
    data.addColumn('number', 'Percent Complete');
    data.addColumn('string', 'Dependencies');

    const today = new Date();
    const tomorrow = new Date(today.getTime() + (24 * 60 * 60 * 1000));
    var rows = [];

    apiData.some((obj, ind) => {
        if( !obj['taskid'] /* || obj['componente']!="Workers" */ ) {
            return null;
        }
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

    rows.push([
        'Today', 
        'Today', 
        'Today', 
        today,
        tomorrow, 
        86400000, 
        100, 
        null 
    ]);

    data.addRows(rows);

    const labelsOrdered = rows.map(r => r[2]).filter((v, i, a) => a.indexOf(v) === i);
    const chartColors = labelsOrdered.map(label => {
        if (typeof(label)=="string") {
            const key = Object.keys(colors).filter(c => label.toLowerCase().indexOf(c.toLowerCase())>-1);
            if (key.length != 0) {
                return colors[key];
            }
        }
    }).filter(c => c!=undefined);
    // chartColors.push(colors['today']);

    var options = {
        height: 45*rows.length + 20,
        width: width,
        gantt: {
            criticalPathEnabled: false,
            palette: chartColors
        }    
    };

    var chart = new google.visualization.Gantt(document.getElementById(elId));

    chart.draw(data, options);
}

$(document).ready(function () {
    // M3
    // Gantt chart
    let apiData = null;

    // Get log data from API
    $.ajax({
        type: "GET",
        url: "http://gsx2json.com/api?id=1bSYkXeiBynJszxFCmmmRslO4wSW1CU6rzLB0C15iErM&sheet=4",
        dataType: "json",
        success: function (data) {
            // data[rows] is object with keys: taskid, tarefa, componente, start, end, durationmilliseconds, percentcomplete, dependencies
            apiData = data['rows'];
        }
    });

    $('#collapseSchedule').on('shown.bs.collapse', function () {
        drawChart(apiData, 'schedule_chart', $("#accordionM3 .card-header").width());
    })

    $('#M3ModalSchedule').on('shown.bs.modal', function (e) {
        drawChart(apiData, 'schedule_chart_modal', $('#M3ModalSchedule .modal-lg').width()-50);
    });

});

/**
 * Commands to make gantt chart invisble for screen shots
 * $("rect").attr("fill", "rgba(0,0,0,0)")
 * $("line").attr("stroke", "rgba(0,0,0,0)")
 */