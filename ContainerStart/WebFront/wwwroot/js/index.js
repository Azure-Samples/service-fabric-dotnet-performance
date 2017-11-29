// ---------------------------------------------------------------
// Global variables
// ---------------------------------------------------------------

// Static
var defaultRefreshRate = 5; // InSeconds
var minRefreshRate = 2; // InSeconds
var maxRefreshRate = 121; // InSeconds
var serviceUrl = location.protocol + '//' + location.hostname + (location.port ? ':' + location.port : '');
var serviceUrl = serviceUrl + (location.pathname ? location.pathname : '');

// Non-Static
var refreshInterval = setInterval(updatePageContent, defaultRefreshRate*1000);
var currentContainerCount = 0;

// Note: this is inefficient, but very clear, so we are keeping this for demo purposes
// If the refresh rate is a negative number, this means that we don't refresh
var currentRefreshRate = defaultRefreshRate;

// ---------------------------------------------------------------
// Initialize the page content
// ---------------------------------------------------------------

updatePageContent();

// ---------------------------------------------------------------
// Page functions - Showing count and refresh
// ---------------------------------------------------------------

// Initialize the slider and make sure that on drag of the slider we show the new values
$( "#slider-refresh-rate" ).slider({
      range: "min",
      value: defaultRefreshRate,
      min: minRefreshRate,
      max: maxRefreshRate,
      slide: function( event, ui ) {
        // Update the displayed refresh interval
        if (ui.value != maxRefreshRate)
        {
            $( "#refresh-rate" ).val( ui.value + " seconds" );
            currentRefreshRate = ui.value;
        }
        else
        {
            $( "#refresh-rate" ).val( "No refresh" );
            currentRefreshRate = -1;
        }
        updateRefreshRate();
      }
    });

// Set the initial refresh rate value of the refresh slider.
$( "#refresh-rate" ).val( $( "#slider-refresh-rate" ).slider( "value" ) + " seconds" );

// On set timed intervals, call REST api to update page content.
// If this method is called, then the interval will change. A new interval will not be added.
function updateRefreshRate()
{
    clearInterval(refreshInterval);

    // Only set the page to auto load data if we have a valid refresh interval
    // (basically, if the user has chosen to have refresh enabled)
    if (currentRefreshRate > 0) 
    {
        refreshInterval = setInterval(updatePageContent, currentRefreshRate*1000);
    }    
}

// Updates the page content by calling REST api
function updatePageContent()
{
    $.ajax({
        url: serviceUrl + "api/values/getContainerCount",
        dataType: "text",
        method: "GET"
    })
   .done(function (containerCountJSON) {
        var containerCount = JSON.parse(containerCountJSON);
        updateContainerCount(containerCount);
   });
}

function updateContainerCount(containerCount)
{
    // Set a global count, in case other methods want to know the current count
    currentContainerCount = containerCount;

    // Change the number of containers displayed
    $('.container-count').text(containerCount);

    // Update chart
    addOrUpdateChartData();
}

// ---------------------------------------------------------------
// Page functions - Historial data chart
// ---------------------------------------------------------------

var color = Chart.helpers.color;

var config = {
    type: 'line',
    data: {
        datasets: [
        {
            label: "Number of containers",
            backgroundColor: 'rgba(255, 99, 132, 0.2)',
            borderColor: 'rgba(255, 99, 12, 0.2)',
            fill: false,
            data: []
        }]
    },
    options: {
        responsive: true,
        title: {
            display: true,
            text: "Container Count Since Page Load"
        },
        scales: {
            xAxes: [{
                type: "time",
                display: true,
                scaleLabel: {
                    display: true,
                    labelString: 'Time'
                },
                ticks: {
                    minRotation: 60
                }
            }],
            yAxes: [{
                display: true,
                scaleLabel: {
                    display: true,
                    labelString: 'Number of Containers'
                }
            }]
        }
    }
};
window.onload = function() {
    var ctx = document.getElementById("time-chart").getContext("2d");
    window.myLine = new Chart(ctx, config);
};

function addData() 
{
    if (config && config.data.datasets.length > 0) {

        for (var index = 0; index < config.data.datasets.length; ++index) {
            config.data.datasets[index].data.push({
                x: moment().format(),
                y: currentContainerCount
            });
        }

        window.myLine.update();
    }
};

function removeData() 
{
    config.data.datasets.forEach(function(dataset, datasetIndex) {
        dataset.data.shift();
    });

    window.myLine.update();
};

function addOrUpdateChartData()
{
    if (config && config.data.datasets[0].data.length >= 120)
    {
        removeData();
    }

    addData();
}
